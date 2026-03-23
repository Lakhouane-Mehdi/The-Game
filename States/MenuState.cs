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
using TheGame.Systems;
using TheGame.Utils;

namespace TheGame.States
{
    public class MenuState : GameStateBase
    {
        private Texture2D _titleBg;
        private float _blinkTimer;
        private bool _showPrompt = true;
        private float _titleFloat;
        private float _bgScroll;
        private KeyboardState _prevKb = Keyboard.GetState();
        private int _menuIndex;
        private bool _hasSave;

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

            _blinkTimer += dt;
            if (_blinkTimer >= 0.5f)
            {
                _blinkTimer = 0f;
                _showPrompt = !_showPrompt;
            }

            _titleFloat += dt * 2f;
            _bgScroll += dt * 8f;
            _hasSave = SaveSystem.HasSave();

            // Navigate menu
            int maxIndex = _hasSave ? 2 : 1;
            if ((kb.IsKeyDown(Keys.W) && _prevKb.IsKeyUp(Keys.W)) ||
                (kb.IsKeyDown(Keys.Up) && _prevKb.IsKeyUp(Keys.Up)))
            {
                _menuIndex--;
                if (_menuIndex < 0) _menuIndex = maxIndex;
            }
            if ((kb.IsKeyDown(Keys.S) && _prevKb.IsKeyUp(Keys.S)) ||
                (kb.IsKeyDown(Keys.Down) && _prevKb.IsKeyUp(Keys.Down)))
            {
                _menuIndex++;
                if (_menuIndex > maxIndex) _menuIndex = 0;
            }

            // Confirm
            if (kb.IsKeyDown(Keys.Enter) && _prevKb.IsKeyUp(Keys.Enter))
            {
                if (_menuIndex == 0)
                {
                    GameRef.ChangeState(GameState.Overworld);
                }
                else if (_menuIndex == 1 && _hasSave)
                {
                    var data = SaveSystem.Load();
                    if (data != null)
                    {
                        SaveSystem.ApplySave(GameRef, GameRef.SharedPlayer, data);
                        GameRef.ChangeState(GameState.Overworld);
                    }
                }
                else if ((_hasSave && _menuIndex == 2) || (!_hasSave && _menuIndex == 1))
                {
                    GameRef.ChangeState(GameState.Credits);
                }
            }

            _prevKb = kb;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            Texture2D px = Game1.PixelTexture;
            int sw = Game1.ScreenWidth;
            int sh = Game1.ScreenHeight;

            // ── Dark background ──
            spriteBatch.Draw(px, new Rectangle(0, 0, sw, sh), new Color(15, 25, 15));

            // ── Tiled grass background with slow diagonal scroll ──
            if (_titleBg != null)
            {
                int tw = _titleBg.Width;
                int th = _titleBg.Height;
                int offX = -(int)(_bgScroll % tw);
                int offY = -(int)(_bgScroll * 0.5f % th);

                for (int x = offX - tw; x < sw + tw; x += tw)
                    for (int y = offY - th; y < sh + th; y += th)
                        spriteBatch.Draw(_titleBg, new Vector2(x, y), Color.White * 0.15f);
            }

            // ── Vignette overlay ──
            int vigH = 80;
            for (int i = 0; i < vigH; i++)
            {
                float alpha = (1f - (float)i / vigH) * 0.6f;
                spriteBatch.Draw(px, new Rectangle(0, i, sw, 1), Color.Black * alpha);
                spriteBatch.Draw(px, new Rectangle(0, sh - 1 - i, sw, 1), Color.Black * alpha);
            }

            // ── Title box ──
            int titleY = 80 + (int)(MathF.Sin(_titleFloat) * 4f);
            int boxW = 420, boxH = 80;
            int boxX = sw / 2 - boxW / 2;

            // Outer glow
            spriteBatch.Draw(px, new Rectangle(boxX - 6, titleY - 6, boxW + 12, boxH + 12),
                Color.Gold * 0.15f);
            spriteBatch.Draw(px, new Rectangle(boxX - 3, titleY - 3, boxW + 6, boxH + 6),
                Color.Gold * 0.3f);
            // Border
            spriteBatch.Draw(px, new Rectangle(boxX - 2, titleY - 2, boxW + 4, boxH + 4),
                Color.Gold * 0.8f);
            // Background
            spriteBatch.Draw(px, new Rectangle(boxX, titleY, boxW, boxH),
                new Color(10, 20, 10) * 0.95f);

            // ── Corner ornaments on box ──
            DrawBoxCorner(spriteBatch, px, boxX, titleY, false, false);
            DrawBoxCorner(spriteBatch, px, boxX + boxW, titleY, true, false);
            DrawBoxCorner(spriteBatch, px, boxX, titleY + boxH, false, true);
            DrawBoxCorner(spriteBatch, px, boxX + boxW, titleY + boxH, true, true);

            // ── Title text ──
            PixelFont.DrawCenteredWithShadow(spriteBatch, "THE FADING RESONANCE", titleY + 10,
                Color.Gold, scale: 3);
            PixelFont.DrawCentered(spriteBatch, "THE GAME", titleY + 40,
                new Color(200, 180, 120), scale: 2);

            // ── Decorative divider ──
            int lineY = titleY + boxH + 12;
            int lineW = 240;
            int lineX = sw / 2 - lineW / 2;
            // Diamond center
            spriteBatch.Draw(px, new Rectangle(sw / 2 - 3, lineY - 1, 6, 3), Color.Gold * 0.6f);
            spriteBatch.Draw(px, new Rectangle(sw / 2 - 2, lineY - 2, 4, 5), Color.Gold * 0.3f);
            // Lines
            spriteBatch.Draw(px, new Rectangle(lineX, lineY, lineW, 1), Color.Gold * 0.4f);
            spriteBatch.Draw(px, new Rectangle(lineX + 40, lineY + 3, lineW - 80, 1), Color.Gold * 0.2f);

            // ── Subtitle ──
            PixelFont.DrawCenteredWithShadow(spriteBatch, "A ZELDA-LIKE ADVENTURE", lineY + 16,
                new Color(150, 200, 150), scale: 2);

            // ── Made by ──
            PixelFont.DrawCentered(spriteBatch, "MADE BY MEHDI LAKHOUANE", lineY + 36,
                new Color(120, 150, 120), scale: 1);

            // ── Menu options ──
            int menuY = 275;
            string[] options = _hasSave
                ? new[] { "NEW GAME", "CONTINUE", "CREDITS" }
                : new[] { "NEW GAME", "CREDITS" };
            for (int i = 0; i < options.Length; i++)
            {
                bool selected = (i == _menuIndex);
                Color c = selected ? Color.White : new Color(120, 140, 120);
                int scale = selected ? 3 : 2;
                int oy = menuY + i * 35;

                if (selected)
                {
                    // Selection highlight bar
                    int tw = PixelFont.MeasureWidth(options[i], scale);
                    spriteBatch.Draw(px, new Rectangle(sw / 2 - tw / 2 - 14, oy - 2, tw + 28, scale * 8 + 4),
                        Color.Gold * 0.08f);
                }

                PixelFont.DrawCenteredWithShadow(spriteBatch, options[i], oy, c, scale);
                if (selected && _showPrompt)
                {
                    int tw = PixelFont.MeasureWidth(options[i], scale);
                    // Animated arrows
                    float bounce = MathF.Sin(_titleFloat * 3f) * 2f;
                    PixelFont.DrawString(spriteBatch, ">",
                        (int)(sw / 2 - tw / 2 - 22 - bounce), oy, Color.Gold, scale);
                    PixelFont.DrawString(spriteBatch, "<",
                        (int)(sw / 2 + tw / 2 + 10 + bounce), oy, Color.Gold, scale);
                }
            }

            // ── Controls hint ──
            int hintY = sh - 50;
            Color hintColor = new Color(80, 100, 80);

            // Hint box
            int hintBoxW = 340;
            spriteBatch.Draw(px, new Rectangle(sw / 2 - hintBoxW / 2, hintY - 4, hintBoxW, 36),
                Color.Black * 0.3f);
            spriteBatch.Draw(px, new Rectangle(sw / 2 - hintBoxW / 2, hintY - 4, hintBoxW, 1),
                Color.Gold * 0.15f);

            PixelFont.DrawCentered(spriteBatch, "WASD - MOVE  SPACE - ATTACK  E - INTERACT", hintY, hintColor, scale: 1);
            hintY += 14;
            PixelFont.DrawCentered(spriteBatch, "X - USE ITEM  Q/E - CYCLE  I - INVENTORY", hintY, hintColor, scale: 1);
        }

        private static void DrawBoxCorner(SpriteBatch sb, Texture2D px, int x, int y,
            bool flipX, bool flipY)
        {
            int dx = flipX ? -1 : 1;
            int dy = flipY ? -1 : 1;
            Color c = Color.Gold * 0.6f;
            sb.Draw(px, new Rectangle(x, y, 8 * dx, 1), c);
            sb.Draw(px, new Rectangle(x, y, 1, 8 * dy), c);
            sb.Draw(px, new Rectangle(x + 2 * dx, y + 2 * dy, 4 * dx, 1), c * 0.4f);
            sb.Draw(px, new Rectangle(x + 2 * dx, y + 2 * dy, 1, 4 * dy), c * 0.4f);
        }
    }
}
