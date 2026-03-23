// ============================================================================
// Game1.cs — Main Game Loop
// Author: Mehdi Lakhouane
// Description: Core game loop running at 60 FPS. Manages global game state
//              transitions between Menu, Overworld, Interior, and Dungeon.
//              Owns the shared Player instance for state persistence.
// ============================================================================

using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TheGame.Entities;
using TheGame.Systems;
using TheGame.Utils;

namespace TheGame.Core
{
    public enum GameState
    {
        Menu,
        Overworld,
        Interior,
        Dungeon,
        Paused,
        Inventory,
        GameOver,
        Shop,
        Blacksmith
    }

    public class Game1 : Game
    {
        // ── Singleton ──
        public static Game1 Instance { get; private set; }

        // ── Graphics ──
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        // ── Debug drawing ──
        public static Texture2D PixelTexture { get; private set; }

        // ── State Management ──
        public GameState CurrentState { get; private set; } = GameState.Menu;
        private GameState _stateBeforePause;

        // ── State Handlers ──
        private States.MenuState _menuState;
        private States.OverworldState _overworldState;
        private States.DungeonState _dungeonState;
        private States.PauseMenuState _pauseMenuState;
        private States.InteriorState _interiorState;
        private States.InventoryState _inventoryState;
        private States.GameOverState _gameOverState;
        private States.ShopState _shopState;
        private States.BlacksmithState _blacksmithState;

        // ── Shared Player (persists across states) ──
        public Player SharedPlayer { get; private set; }

        // ── Global Quest Flags (shared across all states) ──
        public HashSet<string> QuestFlags { get; } = new();

        // ── Global Unlocked Doors ──
        public HashSet<string> UnlockedDoors { get; } = new();

        // ── Fade Transition ──
        private readonly FadeTransition _fade = new();

        // ── Screen Constants ──
        public const int ScreenWidth = 800;
        public const int ScreenHeight = 480;

        // ── Previous keyboard state ──
        private KeyboardState _prevKeyboard;

        public Game1()
        {
            Instance = this;
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            IsFixedTimeStep = true;
            TargetElapsedTime = System.TimeSpan.FromSeconds(1.0 / 60.0);
        }

        protected override void Initialize()
        {
            _graphics.PreferredBackBufferWidth = ScreenWidth;
            _graphics.PreferredBackBufferHeight = ScreenHeight;
            _graphics.ApplyChanges();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            string contentPath = Path.Combine(
                System.AppDomain.CurrentDomain.BaseDirectory, "Content");
            AssetLoader.Initialize(GraphicsDevice, contentPath);

            PixelTexture = AssetLoader.CreatePixel(Color.White);

            // ── Create shared player ──
            SharedPlayer = new Player(new Vector2(
                5 * ScreenWidth + ScreenWidth / 2f - 32,
                3 * ScreenHeight + ScreenHeight / 2f - 32));
            SharedPlayer.LoadContent();

            // Load item catalog and give starter items
            Entities.Items.Inventory.LoadCatalog(contentPath);
            SharedPlayer.Inventory.AddItem("sword");
            SharedPlayer.Inventory.AddItem("boomerang");

            // ── Initialise state handlers ──
            _menuState = new States.MenuState(this, Content);
            _overworldState = new States.OverworldState(this, Content);
            _dungeonState = new States.DungeonState(this, Content);
            _pauseMenuState = new States.PauseMenuState(this, Content);
            _interiorState = new States.InteriorState(this, Content);
            _inventoryState = new States.InventoryState(this, Content);
            _gameOverState = new States.GameOverState(this, Content);
            _shopState = new States.ShopState(this, Content);
            _blacksmithState = new States.BlacksmithState(this, Content);
        }

        // ── Public API for state transitions ──

        public void ChangeState(GameState newState)
        {
            if (newState == GameState.Paused || newState == GameState.Inventory)
            {
                _stateBeforePause = CurrentState;
                if (newState == GameState.Paused) _pauseMenuState.Reset();
            }
            CurrentState = newState;
        }

        /// <summary>
        /// Enters an interior with a fade transition.
        /// </summary>
        public void EnterInterior(string interiorId, Vector2 spawnInside)
        {
            _fade.Start(() =>
            {
                _interiorState.LoadInterior(interiorId, SharedPlayer, spawnInside);
                CurrentState = GameState.Interior;
            });
        }

        /// <summary>
        /// Returns to the overworld with a fade transition.
        /// </summary>
        public void EnterOverworld(Vector2 spawnPos, int roomX, int roomY)
        {
            _fade.Start(() =>
            {
                SharedPlayer.Position = spawnPos;
                _overworldState.ReturnFromInterior(SharedPlayer, roomX, roomY);
                CurrentState = GameState.Overworld;
            });
        }

        /// <summary>
        /// Returns to overworld from pause, resuming the correct state.
        /// </summary>
        public void ResumeFromPause()
        {
            CurrentState = _stateBeforePause;
        }

        /// <summary>
        /// Enters the dungeon with a fade transition.
        /// </summary>
        public void EnterDungeon()
        {
            _fade.Start(() =>
            {
                _dungeonState.EnterDungeon(SharedPlayer);
                CurrentState = GameState.Dungeon;
            });
        }

        /// <summary>
        /// Triggers the Game Over screen.
        /// </summary>
        public void TriggerGameOver()
        {
            _gameOverState.Reset();
            CurrentState = GameState.GameOver;
        }

        /// <summary>Opens the shop UI overlay.</summary>
        public void OpenShop()
        {
            _stateBeforePause = CurrentState;
            _shopState.Open(SharedPlayer);
            CurrentState = GameState.Shop;
        }

        /// <summary>Opens the blacksmith UI overlay.</summary>
        public void OpenBlacksmith()
        {
            _stateBeforePause = CurrentState;
            _blacksmithState.Open(SharedPlayer);
            CurrentState = GameState.Blacksmith;
        }

        public void QuitGame() => Exit();

        // ── Core Loop ──

        protected override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            KeyboardState kb = Keyboard.GetState();

            // Update fade transition
            _fade.Update(dt);

            // Escape → pause menu (from gameplay states)
            if (kb.IsKeyDown(Keys.Escape) && _prevKeyboard.IsKeyUp(Keys.Escape))
            {
                if (CurrentState == GameState.Menu)
                    Exit();
                else if (CurrentState == GameState.Overworld ||
                         CurrentState == GameState.Dungeon ||
                         CurrentState == GameState.Interior)
                    ChangeState(GameState.Paused);
            }

            // Don't update gameplay during fade
            if (!_fade.IsActive)
            {
                switch (CurrentState)
                {
                    case GameState.Menu:
                        _menuState.Update(gameTime);
                        break;
                    case GameState.Overworld:
                        _overworldState.Update(gameTime);
                        break;
                    case GameState.Interior:
                        _interiorState.Update(gameTime);
                        break;
                    case GameState.Dungeon:
                        _dungeonState.Update(gameTime);
                        break;
                    case GameState.Paused:
                        _pauseMenuState.Update(gameTime);
                        break;
                    case GameState.Inventory:
                        _inventoryState.Update(gameTime);
                        break;
                    case GameState.GameOver:
                        _gameOverState.Update(gameTime);
                        break;
                    case GameState.Shop:
                        _shopState.Update(gameTime);
                        break;
                    case GameState.Blacksmith:
                        _blacksmithState.Update(gameTime);
                        break;
                }
            }

            _prevKeyboard = kb;
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(20, 20, 20));

            _spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                null, null, null, null);

            switch (CurrentState)
            {
                case GameState.Menu:
                    _menuState.Draw(_spriteBatch);
                    break;
                case GameState.Overworld:
                    _overworldState.Draw(_spriteBatch);
                    break;
                case GameState.Interior:
                    _interiorState.Draw(_spriteBatch);
                    break;
                case GameState.Dungeon:
                    _dungeonState.Draw(_spriteBatch);
                    break;
                case GameState.Paused:
                    // Draw the state we paused from, then overlay
                    switch (_stateBeforePause)
                    {
                        case GameState.Overworld:
                            _overworldState.Draw(_spriteBatch);
                            break;
                        case GameState.Interior:
                            _interiorState.Draw(_spriteBatch);
                            break;
                        case GameState.Dungeon:
                            _dungeonState.Draw(_spriteBatch);
                            break;
                    }
                    _pauseMenuState.Draw(_spriteBatch);
                    break;
                case GameState.Inventory:
                    // Draw the state we came from (frozen) then overlay inventory
                    switch (_stateBeforePause)
                    {
                        case GameState.Overworld:
                            _overworldState.Draw(_spriteBatch);
                            break;
                        case GameState.Interior:
                            _interiorState.Draw(_spriteBatch);
                            break;
                        case GameState.Dungeon:
                            _dungeonState.Draw(_spriteBatch);
                            break;
                    }
                    _inventoryState.Draw(_spriteBatch);
                    break;
                case GameState.GameOver:
                    _gameOverState.Draw(_spriteBatch);
                    break;
                case GameState.Shop:
                    switch (_stateBeforePause)
                    {
                        case GameState.Interior: _interiorState.Draw(_spriteBatch); break;
                        case GameState.Overworld: _overworldState.Draw(_spriteBatch); break;
                    }
                    _shopState.Draw(_spriteBatch);
                    break;
                case GameState.Blacksmith:
                    switch (_stateBeforePause)
                    {
                        case GameState.Interior: _interiorState.Draw(_spriteBatch); break;
                        case GameState.Overworld: _overworldState.Draw(_spriteBatch); break;
                    }
                    _blacksmithState.Draw(_spriteBatch);
                    break;
            }

            // Draw fade overlay on top of everything
            _fade.Draw(_spriteBatch);

            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
