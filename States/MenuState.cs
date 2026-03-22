// ============================================================================
// MenuState.cs — Title / Main Menu
// Author: Mehdi Lakhouane
// Description: Displays the game title and waits for the player to press
//              Enter to begin. Transitions to the Overworld state.
// ============================================================================

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TheGame.Core;
using TheGame.Utils;

namespace TheGame.States
{
    public class MenuState : GameStateBase
    {
        private Texture2D _titleBg;
        private float _blinkTimer;
        private bool _showPrompt = true;
        private float _titleFloat;   // for floating animation
        private float _bgScroll;     // slow background scroll
        private KeyboardState _prevKb = Keyboard.GetState();

        public MenuState(Game1 game, ContentManager content)
            : base(game, content)
        {
            if (AssetLoader.Exists("Tiles/grass_floor.png"))
                _titleBg = AssetLoader.LoadTexture("Tiles/grass_floor.png");
        }

        public override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            KeyboardState kb = Keyboard.GetState();

            // Blink the prompt
            _blinkTimer += dt;
            if (_blinkTimer >= 0.5f)
            {
                _blinkTimer = 0f;
                _showPrompt = !_showPrompt;
            }

            // Floating title animation
            _titleFloat += dt * 2f;

            // Slow background scroll
            _bgScroll += dt * 8f;

            // Transition on Enter
            if (kb.IsKeyDown(Keys.Enter) && _prevKb.IsKeyUp(Keys.Enter))
                GameRef.ChangeState(GameState.Overworld);

            _prevKb = kb;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            Texture2D px = Game1.PixelTexture;

            // ── Dark background ──
            spriteBatch.Draw(px, new Rectangle(0, 0, Game1.ScreenWidth, Game1.ScreenHeight),
                new Color(15, 25, 15));

            // ── Tiled grass background with slow diagonal scroll ──
            if (_titleBg != null)
            {
                int tw = _titleBg.Width;
                int th = _titleBg.Height;
                int offX = -(int)(_bgScroll % tw);
                int offY = -(int)(_bgScroll * 0.5f % th);

                for (int x = offX - tw; x < Game1.ScreenWidth + tw; x += tw)
                    for (int y = offY - th; y < Game1.ScreenHeight + th; y += th)
                        spriteBatch.Draw(_titleBg, new Vector2(x, y), Color.White * 0.15f);
            }

            // ── Vignette overlay (darker edges) ──
            int vigH = 80;
            for (int i = 0; i < vigH; i++)
            {
                float alpha = (1f - (float)i / vigH) * 0.6f;
                spriteBatch.Draw(px, new Rectangle(0, i, Game1.ScreenWidth, 1), Color.Black * alpha);
                spriteBatch.Draw(px, new Rectangle(0, Game1.ScreenHeight - 1 - i, Game1.ScreenWidth, 1), Color.Black * alpha);
            }

            // ── Title box with border ──
            int titleY = 100 + (int)(MathF.Sin(_titleFloat) * 4f);
            int boxW = 360, boxH = 70;
            int boxX = Game1.ScreenWidth / 2 - boxW / 2;

            // Outer glow
            spriteBatch.Draw(px, new Rectangle(boxX - 4, titleY - 4, boxW + 8, boxH + 8),
                Color.Gold * 0.3f);
            // Border
            spriteBatch.Draw(px, new Rectangle(boxX - 2, titleY - 2, boxW + 4, boxH + 4),
                Color.Gold * 0.8f);
            // Background
            spriteBatch.Draw(px, new Rectangle(boxX, titleY, boxW, boxH),
                new Color(10, 20, 10) * 0.95f);

            // ── Title text: "THE GAME" ──
            PixelFont.DrawCenteredWithShadow(spriteBatch, "THE GAME", titleY + 12,
                Color.Gold, scale: 5);

            // ── Decorative line under title ──
            int lineY = titleY + boxH + 15;
            int lineW = 200;
            int lineX = Game1.ScreenWidth / 2 - lineW / 2;
            spriteBatch.Draw(px, new Rectangle(lineX, lineY, lineW, 2), Color.Gold * 0.5f);
            spriteBatch.Draw(px, new Rectangle(lineX + 30, lineY + 5, lineW - 60, 1), Color.Gold * 0.3f);

            // ── Subtitle ──
            PixelFont.DrawCenteredWithShadow(spriteBatch, "A ZELDA-LIKE ADVENTURE", lineY + 20,
                new Color(150, 200, 150), scale: 2);

            // ── Blinking "PRESS ENTER TO START" ──
            if (_showPrompt)
            {
                PixelFont.DrawCenteredWithShadow(spriteBatch, "PRESS ENTER TO START", 290,
                    Color.White, scale: 3);
            }

            // ── Controls hint ──
            int hintY = 370;
            Color hintColor = new Color(120, 140, 120);

            PixelFont.DrawCentered(spriteBatch, "CONTROLS", hintY, Color.Gray, scale: 2);

            hintY += 25;
            PixelFont.DrawCentered(spriteBatch, "WASD - MOVE", hintY, hintColor, scale: 2);
            hintY += 20;
            PixelFont.DrawCentered(spriteBatch, "SPACE - ATTACK", hintY, hintColor, scale: 2);
            hintY += 20;
            PixelFont.DrawCentered(spriteBatch, "X - USE ITEM  Q/E - CYCLE", hintY, hintColor, scale: 2);
        }
    }
}
