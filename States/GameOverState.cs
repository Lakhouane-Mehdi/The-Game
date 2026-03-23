// ============================================================================
// GameOverState.cs — Death / Game Over Screen
// Author: Mehdi Lakhouane
// Description: Shown when the player dies. Options to retry (respawn at
//              last checkpoint) or return to title screen.
// ============================================================================

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TheGame.Core;
using TheGame.Systems;
using TheGame.Utils;

namespace TheGame.States
{
    public class GameOverState : GameStateBase
    {
        private int _selectedIndex;
        private float _fadeIn;
        private float _timer;
        private float _cursorBlink;
        private KeyboardState _prevKb = Keyboard.GetState();
        private readonly string[] _options = { "RETRY", "TITLE SCREEN" };

        public GameOverState(Game1 game, ContentManager content)
            : base(game, content)
        {
        }

        public void Reset()
        {
            _selectedIndex = 0;
            _fadeIn = 0f;
            _timer = 0f;
            _prevKb = Keyboard.GetState();
        }

        public override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            KeyboardState kb = Keyboard.GetState();

            _timer += dt;
            _fadeIn = MathF.Min(1f, _timer / 1.5f); // fade in over 1.5s
            _cursorBlink += dt * 3f;

            // Don't accept input until fade-in complete
            if (_fadeIn < 1f)
            {
                _prevKb = kb;
                return;
            }

            // Navigate
            if ((kb.IsKeyDown(Keys.W) && _prevKb.IsKeyUp(Keys.W)) ||
                (kb.IsKeyDown(Keys.Up) && _prevKb.IsKeyUp(Keys.Up)))
            {
                _selectedIndex--;
                if (_selectedIndex < 0) _selectedIndex = _options.Length - 1;
            }
            if ((kb.IsKeyDown(Keys.S) && _prevKb.IsKeyUp(Keys.S)) ||
                (kb.IsKeyDown(Keys.Down) && _prevKb.IsKeyUp(Keys.Down)))
            {
                _selectedIndex++;
                if (_selectedIndex >= _options.Length) _selectedIndex = 0;
            }

            // Confirm
            if (kb.IsKeyDown(Keys.Enter) && _prevKb.IsKeyUp(Keys.Enter))
            {
                switch (_selectedIndex)
                {
                    case 0: // Retry
                        // Respawn player with full health at starting position
                        var player = GameRef.SharedPlayer;
                        player.SetHealth(player.MaxHealth);
                        player.Position = new Microsoft.Xna.Framework.Vector2(
                            5 * Game1.ScreenWidth + Game1.ScreenWidth / 2f - 32,
                            3 * Game1.ScreenHeight + Game1.ScreenHeight / 2f - 32);
                        GameRef.ChangeState(GameState.Overworld);
                        break;
                    case 1: // Title Screen
                        GameRef.ChangeState(GameState.Menu);
                        break;
                }
            }

            _prevKb = kb;
        }

        public override void Draw(SpriteBatch sb)
        {
            Texture2D px = Game1.PixelTexture;

            // Dark red background
            sb.Draw(px, new Rectangle(0, 0, Game1.ScreenWidth, Game1.ScreenHeight),
                new Color(20, 5, 5) * _fadeIn);

            // Blood-red vignette
            for (int i = 0; i < 60; i++)
            {
                float alpha = (1f - (float)i / 60) * 0.4f * _fadeIn;
                sb.Draw(px, new Rectangle(0, i, Game1.ScreenWidth, 1), Color.DarkRed * alpha);
                sb.Draw(px, new Rectangle(0, Game1.ScreenHeight - 1 - i, Game1.ScreenWidth, 1), Color.DarkRed * alpha);
            }

            // "YOU DIED" text with pulse
            float pulse = 1f + MathF.Sin(_timer * 2f) * 0.05f;
            int titleY = 140;
            PixelFont.DrawCenteredWithShadow(sb, "YOU DIED", titleY, Color.DarkRed * _fadeIn, (int)(6 * pulse));

            // Decorative lines
            int lineW = 180;
            int lineX = Game1.ScreenWidth / 2 - lineW / 2;
            sb.Draw(px, new Rectangle(lineX, titleY + 50, lineW, 2), Color.DarkRed * 0.5f * _fadeIn);

            // Subtitle
            PixelFont.DrawCentered(sb, "THE STILLNESS CLAIMS ANOTHER...",
                titleY + 70, new Color(150, 80, 80) * _fadeIn, 2);

            // Menu options (only after fade-in)
            if (_fadeIn >= 1f)
            {
                int menuY = 300;
                for (int i = 0; i < _options.Length; i++)
                {
                    bool selected = (i == _selectedIndex);
                    Color c = selected ? Color.White : new Color(150, 100, 100);
                    int scale = selected ? 3 : 2;
                    int oy = menuY + i * 40;

                    PixelFont.DrawCenteredWithShadow(sb, _options[i], oy, c, scale);

                    if (selected)
                    {
                        float blink = 0.5f + 0.5f * MathF.Sin(_cursorBlink);
                        int tw = PixelFont.MeasureWidth(_options[i], scale);
                        PixelFont.DrawString(sb, "-", Game1.ScreenWidth / 2 - tw / 2 - 20, oy,
                            Color.Red * blink, scale);
                    }
                }
            }
        }
    }
}
