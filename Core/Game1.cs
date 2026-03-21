// ============================================================================
// Game1.cs — Main Game Loop
// Author: Mehdi Lakhouane
// Description: Core game loop running at 60 FPS. Manages global game state
//              transitions between Menu, Overworld, and Dungeon screens.
// ============================================================================

using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TheGame.Utils;

namespace TheGame.Core
{
    /// <summary>
    /// The possible high-level states the game can be in.
    /// </summary>
    public enum GameState
    {
        Menu,
        Overworld,
        Dungeon,
        Paused
    }

    /// <summary>
    /// Main entry point. Inherits from MonoGame's Game class.
    /// Delegates Update/Draw calls to the currently active state handler.
    /// </summary>
    public class Game1 : Game
    {
        // ── Graphics ──
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        // ── Debug drawing ──
        public static Texture2D PixelTexture { get; private set; }

        // ── State Management ──
        public GameState CurrentState { get; private set; } = GameState.Menu;

        // ── State Handlers ──
        private States.MenuState _menuState;
        private States.OverworldState _overworldState;
        private States.DungeonState _dungeonState;

        // ── Screen Constants ──
        public const int ScreenWidth = 800;
        public const int ScreenHeight = 480;

        // ── Previous keyboard state (for edge detection) ──
        private KeyboardState _prevKeyboard;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            // Lock the frame rate to 60 FPS
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

            // Initialise the raw asset loader (bypasses MGCB pipeline)
            string contentPath = Path.Combine(
                System.AppDomain.CurrentDomain.BaseDirectory, "Content");
            AssetLoader.Initialize(GraphicsDevice, contentPath);

            // Create a 1x1 white pixel for debug drawing
            PixelTexture = AssetLoader.CreatePixel(Color.White);

            // Initialise each state handler — they load their own assets
            _menuState = new States.MenuState(this, Content);
            _overworldState = new States.OverworldState(this, Content);
            _dungeonState = new States.DungeonState(this, Content);
        }

        // ── Public API for state transitions (called by state handlers) ──

        /// <summary>
        /// Switches the active game state. Called from within state handlers
        /// when a transition condition is met (e.g. player enters a dungeon).
        /// </summary>
        public void ChangeState(GameState newState)
        {
            CurrentState = newState;
        }

        // ── Core Loop ──

        protected override void Update(GameTime gameTime)
        {
            KeyboardState kb = Keyboard.GetState();

            // Global exit shortcut
            if (kb.IsKeyDown(Keys.Escape))
                Exit();

            // Delegate update to the active state
            switch (CurrentState)
            {
                case GameState.Menu:
                    _menuState.Update(gameTime);
                    break;

                case GameState.Overworld:
                    _overworldState.Update(gameTime);
                    break;

                case GameState.Dungeon:
                    _dungeonState.Update(gameTime);
                    break;

                case GameState.Paused:
                    // Unpause on P (edge-triggered)
                    if (kb.IsKeyDown(Keys.P) && _prevKeyboard.IsKeyUp(Keys.P))
                        CurrentState = GameState.Overworld;
                    break;
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
                SamplerState.PointClamp,       // Pixel-perfect scaling
                null, null, null, null);

            switch (CurrentState)
            {
                case GameState.Menu:
                    _menuState.Draw(_spriteBatch);
                    break;

                case GameState.Overworld:
                    _overworldState.Draw(_spriteBatch);
                    break;

                case GameState.Dungeon:
                    _dungeonState.Draw(_spriteBatch);
                    break;

                case GameState.Paused:
                    _overworldState.Draw(_spriteBatch);
                    // Draw pause overlay
                    _spriteBatch.Draw(PixelTexture,
                        new Rectangle(0, 0, ScreenWidth, ScreenHeight),
                        Color.Black * 0.5f);
                    break;
            }

            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
