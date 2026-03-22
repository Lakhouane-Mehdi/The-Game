// ============================================================================
// PauseMenuState.cs — In-Game Command Menu
// Author: Mehdi Lakhouane
// Description: Pause/command menu overlay with navigable options.
//              Activated by pressing Escape during gameplay.
//              Options: Resume, Controls, Return to Title, Quit.
// ============================================================================

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TheGame.Core;
using TheGame.Systems;
using TheGame.Utils;

namespace TheGame.States
{
    public class PauseMenuState : GameStateBase
    {
        private readonly string[] _options = { "RESUME", "SAVE GAME", "CONTROLS", "TITLE SCREEN", "QUIT" };
        private int _selectedIndex;
        private KeyboardState _prevKb = Keyboard.GetState();
        private bool _showControls;
        private float _cursorBlink;
        private string _saveMessage;
        private float _saveMessageTimer;

        public PauseMenuState(Game1 game, ContentManager content)
            : base(game, content)
        {
        }

        /// <summary>
        /// Called when entering the pause menu to reset state.
        /// </summary>
        public void Reset()
        {
            _selectedIndex = 0;
            _showControls = false;
        }

        public override void Update(GameTime gameTime)
        {
            KeyboardState kb = Keyboard.GetState();
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _cursorBlink += dt * 3f;
            if (_saveMessageTimer > 0f) _saveMessageTimer -= dt;

            if (_showControls)
            {
                // Any key exits the controls sub-screen
                if ((kb.IsKeyDown(Keys.Escape) && _prevKb.IsKeyUp(Keys.Escape)) ||
                    (kb.IsKeyDown(Keys.Enter) && _prevKb.IsKeyUp(Keys.Enter)) ||
                    (kb.IsKeyDown(Keys.Back) && _prevKb.IsKeyUp(Keys.Back)))
                {
                    _showControls = false;
                }
                _prevKb = kb;
                return;
            }

            // Navigate up
            if ((kb.IsKeyDown(Keys.W) && _prevKb.IsKeyUp(Keys.W)) ||
                (kb.IsKeyDown(Keys.Up) && _prevKb.IsKeyUp(Keys.Up)))
            {
                _selectedIndex--;
                if (_selectedIndex < 0) _selectedIndex = _options.Length - 1;
            }

            // Navigate down
            if ((kb.IsKeyDown(Keys.S) && _prevKb.IsKeyUp(Keys.S)) ||
                (kb.IsKeyDown(Keys.Down) && _prevKb.IsKeyUp(Keys.Down)))
            {
                _selectedIndex++;
                if (_selectedIndex >= _options.Length) _selectedIndex = 0;
            }

            // Confirm selection
            if (kb.IsKeyDown(Keys.Enter) && _prevKb.IsKeyUp(Keys.Enter))
            {
                switch (_selectedIndex)
                {
                    case 0: // Resume
                        GameRef.ResumeFromPause();
                        break;
                    case 1: // Save Game
                        bool saved = SaveSystem.Save(GameRef, GameRef.SharedPlayer, 0, 0);
                        _saveMessage = saved ? "GAME SAVED!" : "SAVE FAILED!";
                        _saveMessageTimer = 2f;
                        break;
                    case 2: // Controls
                        _showControls = true;
                        break;
                    case 3: // Title Screen
                        GameRef.ChangeState(GameState.Menu);
                        break;
                    case 4: // Quit
                        GameRef.QuitGame();
                        break;
                }
            }

            // Escape also resumes
            if (kb.IsKeyDown(Keys.Escape) && _prevKb.IsKeyUp(Keys.Escape))
                GameRef.ResumeFromPause();

            _prevKb = kb;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            // The overworld is drawn underneath by Game1 before this.
            // We draw a dark overlay and menu on top.
            Texture2D px = Game1.PixelTexture;

            // ── Dim overlay ──
            spriteBatch.Draw(px, new Rectangle(0, 0, Game1.ScreenWidth, Game1.ScreenHeight),
                Color.Black * 0.7f);

            if (_showControls)
            {
                DrawControlsScreen(spriteBatch, px);
                return;
            }

            // ── Menu box ──
            int boxW = 300, boxH = 250;
            int boxX = Game1.ScreenWidth / 2 - boxW / 2;
            int boxY = Game1.ScreenHeight / 2 - boxH / 2;

            // Border
            spriteBatch.Draw(px, new Rectangle(boxX - 2, boxY - 2, boxW + 4, boxH + 4),
                Color.Gold * 0.6f);
            // Background
            spriteBatch.Draw(px, new Rectangle(boxX, boxY, boxW, boxH),
                new Color(10, 15, 10) * 0.95f);

            // ── Title ──
            PixelFont.DrawCenteredWithShadow(spriteBatch, "PAUSED", boxY + 20, Color.Gold, scale: 4);

            // ── Separator ──
            spriteBatch.Draw(px, new Rectangle(boxX + 20, boxY + 60, boxW - 40, 1), Color.Gold * 0.4f);

            // ── Menu options ──
            int optionY = boxY + 80;
            int optionSpacing = 35;

            for (int i = 0; i < _options.Length; i++)
            {
                bool selected = (i == _selectedIndex);
                Color color = selected ? Color.White : new Color(100, 110, 100);
                int scale = selected ? 3 : 2;
                int y = optionY + i * optionSpacing;

                PixelFont.DrawCentered(spriteBatch, _options[i], y, color, scale);

                // Draw selection arrow
                if (selected)
                {
                    int textW = PixelFont.MeasureWidth(_options[i], scale);
                    int arrowX = Game1.ScreenWidth / 2 - textW / 2 - 20;

                    // Blinking arrow
                    float alpha = 0.5f + 0.5f * System.MathF.Sin(_cursorBlink);
                    PixelFont.DrawString(spriteBatch, "-", arrowX, y, Color.Gold * alpha, scale);
                }
            }

            // ── Save message ──
            if (_saveMessageTimer > 0f && _saveMessage != null)
            {
                Color msgColor = _saveMessage.Contains("SAVED") ? Color.Lime : Color.Red;
                PixelFont.DrawCentered(spriteBatch, _saveMessage,
                    boxY + boxH - 50, msgColor * System.MathF.Min(1f, _saveMessageTimer), scale: 2);
            }

            // ── Bottom hint ──
            PixelFont.DrawCentered(spriteBatch, "W/S - SELECT  ENTER - CONFIRM",
                boxY + boxH - 25, new Color(80, 90, 80), scale: 1);
        }

        private void DrawControlsScreen(SpriteBatch spriteBatch, Texture2D px)
        {
            // ── Controls panel ──
            int boxW = 450, boxH = 340;
            int boxX = Game1.ScreenWidth / 2 - boxW / 2;
            int boxY = Game1.ScreenHeight / 2 - boxH / 2;

            spriteBatch.Draw(px, new Rectangle(boxX - 2, boxY - 2, boxW + 4, boxH + 4),
                Color.Gold * 0.6f);
            spriteBatch.Draw(px, new Rectangle(boxX, boxY, boxW, boxH),
                new Color(10, 15, 10) * 0.95f);

            PixelFont.DrawCenteredWithShadow(spriteBatch, "CONTROLS", boxY + 15, Color.Gold, scale: 4);

            spriteBatch.Draw(px, new Rectangle(boxX + 20, boxY + 55, boxW - 40, 1), Color.Gold * 0.4f);

            int y = boxY + 70;
            int spacing = 28;
            Color labelColor = Color.Gold;
            Color valueColor = new Color(180, 200, 180);

            DrawControlRow(spriteBatch, "MOVEMENT", "W A S D", y, labelColor, valueColor); y += spacing;
            DrawControlRow(spriteBatch, "ATTACK", "SPACE", y, labelColor, valueColor); y += spacing;
            DrawControlRow(spriteBatch, "USE ITEM", "X", y, labelColor, valueColor); y += spacing;
            DrawControlRow(spriteBatch, "CYCLE ITEMS", "Q / E", y, labelColor, valueColor); y += spacing;
            DrawControlRow(spriteBatch, "PAUSE MENU", "ESCAPE", y, labelColor, valueColor); y += spacing;
            DrawControlRow(spriteBatch, "DEBUG VIEW", "F3", y, labelColor, valueColor); y += spacing;

            y += 10;
            spriteBatch.Draw(px, new Rectangle(boxX + 40, y, boxW - 80, 1), Color.Gold * 0.3f);
            y += 15;

            DrawControlRow(spriteBatch, "NAVIGATE MENU", "W / S", y, labelColor, valueColor); y += spacing;
            DrawControlRow(spriteBatch, "CONFIRM", "ENTER", y, labelColor, valueColor); y += spacing;

            // ── Back hint ──
            PixelFont.DrawCentered(spriteBatch, "PRESS ENTER OR ESC TO GO BACK",
                boxY + boxH - 25, new Color(80, 90, 80), scale: 1);
        }

        private void DrawControlRow(SpriteBatch sb, string label, string value,
            int y, Color labelColor, Color valueColor)
        {
            int centerX = Game1.ScreenWidth / 2;
            // Label on left, value on right
            int labelW = PixelFont.MeasureWidth(label, 2);
            int valueW = PixelFont.MeasureWidth(value, 2);

            PixelFont.DrawString(sb, label, centerX - 120, y, labelColor, 2);
            PixelFont.DrawString(sb, value, centerX + 120 - valueW, y, valueColor, 2);
        }
    }
}
