// ============================================================================
// CreditsState.cs — Full Credits Screen
// Author: Mehdi Lakhouane
// Description: Cinematic scrolling credits with pixel-art effects.
// ============================================================================

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TheGame.Core;
using TheGame.Utils;

namespace TheGame.States
{
    public class CreditsState : GameStateBase
    {
        private float _timer;
        private float _scrollY;
        private KeyboardState _prevKb;
        private readonly Random _rng = new();

        // Floating pixel stars
        private struct Star
        {
            public float X, Y, Speed, Brightness;
            public int Size;
        }
        private readonly Star[] _stars;

        // Credit entries
        private static readonly (string title, string[] names, Color titleColor)[] Credits =
        {
            ("GAME DESIGN AND PROGRAMMING", new[] { "MEHDI LAKHOUANE" }, Color.Gold),
            ("THE FADING RESONANCE", new[] { "A ZELDA-LIKE RPG ADVENTURE" }, new Color(180, 220, 180)),
            ("ART AND SPRITES", new[] {
                "CUTE FANTASY FREE PACK",
                "SPROUT LANDS ASSET PACK",
                "FREE PIXEL ART TILESETS"
            }, new Color(150, 200, 255)),
            ("PLAYER CHARACTER", new[] {
                "CUTE FANTASY - PLAYER BASE, HAIR, CLOTHES",
                "LAYERED SPRITE ANIMATION SYSTEM"
            }, new Color(150, 200, 255)),
            ("MONSTERS", new[] {
                "BAMBOO HOLLOW - FOREST ENEMY",
                "RACCOON GUARDIAN - MINI BOSS",
                "SPIRIT - RANGED DUNGEON ENEMY",
                "SQUID (THE SILENCE GUARDIAN) - FINAL BOSS"
            }, new Color(150, 200, 255)),
            ("ENVIRONMENT ART", new[] {
                "TREES, STUMPS AND BUSHES - SPROUT LANDS",
                "MUSHROOMS, FLOWERS, STONES - SPROUT LANDS",
                "WOODEN HOUSES AND ROOFS - SPROUT LANDS",
                "FENCES, PATHS AND BRIDGES - SPROUT LANDS",
                "WOODS TILESET - FREE PIXEL 16"
            }, new Color(150, 200, 255)),
            ("INTERIOR DESIGN", new[] {
                "BASIC FURNITURE - SPROUT LANDS",
                "CHEST AND DOOR ANIMATIONS",
                "LIGHT AND SHADOW EFFECTS"
            }, new Color(150, 200, 255)),
            ("ENGINE", new[] { "MONOGAME FRAMEWORK", ".NET 8 / C#" }, new Color(200, 180, 255)),
            ("STORY", new[] {
                "THE WORLD FALLS SILENT...",
                "THE GREAT TUNING FORK HAS BEEN CORRUPTED",
                "ONLY THE LAST TUNER CAN RESTORE",
                "THE RESONANCE AND BRING BACK",
                "SOUND TO A DYING WORLD"
            }, new Color(220, 200, 150)),
            ("SPECIAL THANKS", new[] {
                "ALL OPEN-SOURCE PIXEL ART CREATORS",
                "THE MONOGAME COMMUNITY",
                "AND YOU, THE PLAYER"
            }, new Color(255, 220, 150)),
        };

        public CreditsState(Game1 game, ContentManager content) : base(game, content)
        {
            _prevKb = Keyboard.GetState();

            // Generate background stars
            _stars = new Star[60];
            for (int i = 0; i < _stars.Length; i++)
            {
                _stars[i] = new Star
                {
                    X = _rng.Next(Game1.ScreenWidth),
                    Y = _rng.Next(Game1.ScreenHeight),
                    Speed = 0.2f + (float)_rng.NextDouble() * 0.6f,
                    Brightness = 0.2f + (float)_rng.NextDouble() * 0.5f,
                    Size = _rng.Next(3) == 0 ? 2 : 1
                };
            }
        }

        public void Open()
        {
            _timer = 0;
            _scrollY = 0;
            _prevKb = Keyboard.GetState();
        }

        public override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            KeyboardState kb = Keyboard.GetState();

            _timer += dt;

            // Scroll speed
            float scrollSpeed = 30f;
            if (kb.IsKeyDown(Keys.Down) || kb.IsKeyDown(Keys.S))
                scrollSpeed = 90f; // fast scroll
            _scrollY += scrollSpeed * dt;

            // Back to menu
            if ((kb.IsKeyDown(Keys.Escape) && _prevKb.IsKeyUp(Keys.Escape)) ||
                (kb.IsKeyDown(Keys.Enter) && _prevKb.IsKeyUp(Keys.Enter) && _timer > 0.5f))
            {
                GameRef.ChangeState(GameState.Menu);
                _prevKb = kb;
                return;
            }

            // Auto-return after all credits scroll past
            if (_scrollY > GetTotalCreditsHeight() + 100)
            {
                GameRef.ChangeState(GameState.Menu);
                return;
            }

            _prevKb = kb;
        }

        private int GetTotalCreditsHeight()
        {
            int h = 120; // initial padding
            foreach (var (title, names, _) in Credits)
            {
                h += 30; // title
                h += names.Length * 18; // names
                h += 40; // spacing
            }
            h += 80; // "THANK YOU FOR PLAYING"
            return h;
        }

        public override void Draw(SpriteBatch sb)
        {
            Texture2D px = Game1.PixelTexture;
            int sw = Game1.ScreenWidth;
            int sh = Game1.ScreenHeight;

            // ── Background ──
            sb.Draw(px, new Rectangle(0, 0, sw, sh), new Color(5, 8, 18));

            // ── Animated stars ──
            for (int i = 0; i < _stars.Length; i++)
            {
                var s = _stars[i];
                float twinkle = s.Brightness + MathF.Sin(_timer * s.Speed * 5f + i) * 0.15f;
                float starY = (s.Y + _timer * s.Speed * 10f) % sh;
                Color starCol = Color.White * MathHelper.Clamp(twinkle, 0.05f, 0.6f);
                sb.Draw(px, new Rectangle((int)s.X, (int)starY, s.Size, s.Size), starCol);
            }

            // ── Scrolling credits ──
            float y = sh - _scrollY + 80; // start below screen, scroll up

            // Header: decorative line
            DrawHLine(sb, px, sw / 2 - 120, (int)y, 240, Color.Gold * 0.4f);
            y += 10;

            foreach (var (title, names, titleColor) in Credits)
            {
                // Section divider dots
                float divAlpha = GetVisibility(y, sh);
                if (divAlpha > 0)
                {
                    for (int dx = -30; dx <= 30; dx += 10)
                        sb.Draw(px, new Rectangle(sw / 2 + dx, (int)y - 8, 2, 2),
                            Color.Gold * 0.2f * divAlpha);
                }

                // Title
                float titleAlpha = GetVisibility(y, sh);
                if (titleAlpha > 0)
                {
                    // Glow bar behind title
                    int tw = PixelFont.MeasureWidth(title, 2);
                    sb.Draw(px, new Rectangle(sw / 2 - tw / 2 - 10, (int)y - 2, tw + 20, 18),
                        titleColor * 0.06f * titleAlpha);
                    PixelFont.DrawCentered(sb, title, (int)y, titleColor * titleAlpha, 2);
                }
                y += 30;

                // Names
                foreach (string name in names)
                {
                    float nameAlpha = GetVisibility(y, sh);
                    if (nameAlpha > 0)
                        PixelFont.DrawCentered(sb, name, (int)y,
                            new Color(200, 210, 220) * nameAlpha, 1);
                    y += 18;
                }

                y += 40;
            }

            // ── Final "Thank you" ──
            float tyAlpha = GetVisibility(y, sh);
            if (tyAlpha > 0)
            {
                DrawHLine(sb, px, sw / 2 - 100, (int)y - 10, 200, Color.Gold * 0.3f * tyAlpha);

                float pulse = 0.7f + MathF.Sin(_timer * 2f) * 0.3f;
                PixelFont.DrawCentered(sb, "THANK YOU FOR PLAYING", (int)y + 5,
                    Color.Gold * tyAlpha * pulse, 3);

                DrawHLine(sb, px, sw / 2 - 100, (int)y + 35, 200, Color.Gold * 0.3f * tyAlpha);
            }

            // ── Top/bottom fade gradient ──
            for (int i = 0; i < 60; i++)
            {
                float fa = (1f - i / 60f) * 0.95f;
                sb.Draw(px, new Rectangle(0, i, sw, 1), new Color(5, 8, 18) * fa);
                sb.Draw(px, new Rectangle(0, sh - 1 - i, sw, 1), new Color(5, 8, 18) * fa);
            }

            // ── Controls ──
            PixelFont.DrawCentered(sb, "ESC / ENTER - BACK    DOWN - FAST SCROLL",
                sh - 16, Color.Gray * 0.4f, 1);
        }

        private static float GetVisibility(float y, int screenHeight)
        {
            // Fade in/out at screen edges
            if (y < 60) return MathHelper.Clamp(y / 60f, 0, 1);
            if (y > screenHeight - 60) return MathHelper.Clamp((screenHeight - y) / 60f, 0, 1);
            if (y < 0 || y > screenHeight) return 0;
            return 1f;
        }

        private static void DrawHLine(SpriteBatch sb, Texture2D px, int x, int y, int w, Color c)
        {
            sb.Draw(px, new Rectangle(x, y, w, 1), c);
            sb.Draw(px, new Rectangle(x + 20, y + 2, w - 40, 1), c * 0.5f);
        }
    }
}
