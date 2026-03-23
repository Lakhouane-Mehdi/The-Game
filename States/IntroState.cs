// ============================================================================
// IntroState.cs — Splash/Intro Screen
// Author: Mehdi Lakhouane
// Description: A cinematic pixel-art intro screen with animated effects.
//              Shows "MADE BY MEHDI LAKHOUANE" with particles, glowing text,
//              and a procedural pixel-art background before the title screen.
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
    public class IntroState : GameStateBase
    {
        private float _timer;
        private float _totalTime;
        private float _blinkTimer;
        private bool _showPrompt = true;
        private KeyboardState _prevKb;

        // Particles
        private struct Particle
        {
            public Vector2 Pos;
            public Vector2 Vel;
            public float Life, MaxLife;
            public Color Col;
            public int Size;
        }
        private readonly List<Particle> _particles = new();
        private readonly Random _rng = new();

        // Phase timing
        private const float FadeInDuration = 1.2f;
        private const float HoldDuration = 3.5f;
        private const float FadeOutDuration = 0.8f;
        private const float TotalDuration = FadeInDuration + HoldDuration + FadeOutDuration;

        // Floating runes/glyphs positions (procedural decoration)
        private readonly (float x, float y, float speed, float offset, int glyph)[] _runes;

        // Text reveal timer
        private float _revealTimer;
        private bool _canSkip;

        public IntroState(Game1 game, ContentManager content) : base(game, content)
        {
            _prevKb = Keyboard.GetState();

            // Generate floating rune positions
            _runes = new (float, float, float, float, int)[20];
            for (int i = 0; i < _runes.Length; i++)
            {
                _runes[i] = (
                    _rng.Next(20, Game1.ScreenWidth - 20),
                    _rng.Next(20, Game1.ScreenHeight - 20),
                    0.3f + (float)_rng.NextDouble() * 0.8f,
                    (float)_rng.NextDouble() * MathF.PI * 2,
                    _rng.Next(6)
                );
            }
        }

        public override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            KeyboardState kb = Keyboard.GetState();

            _timer += dt;
            _totalTime += dt;
            _revealTimer += dt;
            _blinkTimer += dt;
            if (_blinkTimer >= 0.5f) { _blinkTimer = 0f; _showPrompt = !_showPrompt; }

            // Allow skip after 1 second
            _canSkip = _timer > 1.0f;

            if (_canSkip && kb.IsKeyDown(Keys.Enter) && _prevKb.IsKeyUp(Keys.Enter))
            {
                GameRef.ChangeState(GameState.Menu);
                _prevKb = kb;
                return;
            }

            // Auto-advance after full duration
            if (_timer >= TotalDuration)
            {
                GameRef.ChangeState(GameState.Menu);
                return;
            }

            // Spawn particles
            if (_timer > 0.5f && _timer < FadeInDuration + HoldDuration)
            {
                for (int i = 0; i < 2; i++)
                {
                    float angle = (float)_rng.NextDouble() * MathF.PI * 2;
                    float speed = 15f + (float)_rng.NextDouble() * 40f;
                    int type = _rng.Next(3);
                    Color col = type switch
                    {
                        0 => new Color(255, 200 + _rng.Next(55), 80 + _rng.Next(80)),   // gold
                        1 => new Color(150 + _rng.Next(80), 200 + _rng.Next(55), 100),   // green
                        _ => new Color(200 + _rng.Next(55), 180 + _rng.Next(50), 255),   // purple
                    };

                    _particles.Add(new Particle
                    {
                        Pos = new Vector2(
                            Game1.ScreenWidth / 2f + (_rng.Next(200) - 100),
                            Game1.ScreenHeight / 2f + (_rng.Next(60) - 30)),
                        Vel = new Vector2(MathF.Cos(angle) * speed, MathF.Sin(angle) * speed - 20f),
                        Life = 0,
                        MaxLife = 1.5f + (float)_rng.NextDouble() * 1.5f,
                        Col = col,
                        Size = 2 + _rng.Next(3)
                    });
                }
            }

            // Update particles
            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                var p = _particles[i];
                p.Life += dt;
                p.Pos += p.Vel * dt;
                p.Vel.Y += 8f * dt; // slight gravity
                p.Vel *= 0.99f;
                _particles[i] = p;
                if (p.Life >= p.MaxLife)
                    _particles.RemoveAt(i);
            }

            _prevKb = kb;
        }

        public override void Draw(SpriteBatch sb)
        {
            Texture2D px = Game1.PixelTexture;
            int sw = Game1.ScreenWidth;
            int sh = Game1.ScreenHeight;

            // Calculate global alpha for fade in/out
            float alpha;
            if (_timer < FadeInDuration)
                alpha = _timer / FadeInDuration;
            else if (_timer > FadeInDuration + HoldDuration)
                alpha = 1f - ((_timer - FadeInDuration - HoldDuration) / FadeOutDuration);
            else
                alpha = 1f;
            alpha = MathHelper.Clamp(alpha, 0f, 1f);

            // ── Deep dark background ──
            sb.Draw(px, new Rectangle(0, 0, sw, sh), new Color(5, 8, 15));

            // ── Animated starfield / floating pixel dust ──
            for (int i = 0; i < _runes.Length; i++)
            {
                var (rx, ry, spd, off, glyph) = _runes[i];
                float bobX = rx + MathF.Sin(_totalTime * spd + off) * 12f;
                float bobY = ry + MathF.Cos(_totalTime * spd * 0.7f + off) * 8f;
                float runeAlpha = (0.15f + MathF.Sin(_totalTime * spd * 2f + off) * 0.1f) * alpha;

                // Draw small pixel "rune" shapes
                Color runeCol = Color.Gold * runeAlpha;
                switch (glyph % 6)
                {
                    case 0: // diamond
                        sb.Draw(px, new Rectangle((int)bobX, (int)bobY - 2, 3, 1), runeCol);
                        sb.Draw(px, new Rectangle((int)bobX - 1, (int)bobY - 1, 5, 1), runeCol);
                        sb.Draw(px, new Rectangle((int)bobX - 2, (int)bobY, 7, 1), runeCol);
                        sb.Draw(px, new Rectangle((int)bobX - 1, (int)bobY + 1, 5, 1), runeCol);
                        sb.Draw(px, new Rectangle((int)bobX, (int)bobY + 2, 3, 1), runeCol);
                        break;
                    case 1: // cross
                        sb.Draw(px, new Rectangle((int)bobX, (int)bobY - 2, 1, 5), runeCol);
                        sb.Draw(px, new Rectangle((int)bobX - 2, (int)bobY, 5, 1), runeCol);
                        break;
                    case 2: // star dot
                        sb.Draw(px, new Rectangle((int)bobX - 1, (int)bobY - 1, 3, 3), runeCol);
                        sb.Draw(px, new Rectangle((int)bobX, (int)bobY - 2, 1, 1), runeCol);
                        sb.Draw(px, new Rectangle((int)bobX, (int)bobY + 2, 1, 1), runeCol);
                        sb.Draw(px, new Rectangle((int)bobX - 2, (int)bobY, 1, 1), runeCol);
                        sb.Draw(px, new Rectangle((int)bobX + 2, (int)bobY, 1, 1), runeCol);
                        break;
                    default: // simple dot with glow
                        sb.Draw(px, new Rectangle((int)bobX - 1, (int)bobY - 1, 3, 3), runeCol);
                        sb.Draw(px, new Rectangle((int)bobX, (int)bobY, 1, 1), Color.White * runeAlpha * 2f);
                        break;
                }
            }

            // ── Horizontal decorative lines ──
            float lineAlpha = alpha * 0.3f;
            int centerY = sh / 2;
            sb.Draw(px, new Rectangle(sw / 2 - 180, centerY - 50, 360, 1), Color.Gold * lineAlpha);
            sb.Draw(px, new Rectangle(sw / 2 - 140, centerY - 48, 280, 1), Color.Gold * lineAlpha * 0.5f);
            sb.Draw(px, new Rectangle(sw / 2 - 180, centerY + 50, 360, 1), Color.Gold * lineAlpha);
            sb.Draw(px, new Rectangle(sw / 2 - 140, centerY + 52, 280, 1), Color.Gold * lineAlpha * 0.5f);

            // ── Corner decorations (pixel art brackets) ──
            float cornerAlpha = alpha * 0.4f;
            DrawCorner(sb, px, sw / 2 - 200, centerY - 60, false, false, cornerAlpha);
            DrawCorner(sb, px, sw / 2 + 200, centerY - 60, true, false, cornerAlpha);
            DrawCorner(sb, px, sw / 2 - 200, centerY + 60, false, true, cornerAlpha);
            DrawCorner(sb, px, sw / 2 + 200, centerY + 60, true, true, cornerAlpha);

            // ── Particles (behind text) ──
            foreach (var p in _particles)
            {
                float pAlpha = (1f - p.Life / p.MaxLife) * alpha;
                sb.Draw(px, new Rectangle((int)p.Pos.X, (int)p.Pos.Y, p.Size, p.Size),
                    p.Col * pAlpha);
                // Glow around particle
                sb.Draw(px, new Rectangle((int)p.Pos.X - 1, (int)p.Pos.Y - 1, p.Size + 2, p.Size + 2),
                    p.Col * pAlpha * 0.3f);
            }

            // ── "MADE BY" text — appears first, smaller ──
            float madeByAlpha = MathHelper.Clamp((_revealTimer - 0.3f) * 2f, 0f, 1f) * alpha;
            if (madeByAlpha > 0)
            {
                PixelFont.DrawCentered(sb, "A GAME BY", centerY - 30,
                    new Color(160, 170, 180) * madeByAlpha, 2);
            }

            // ── "MEHDI LAKHOUANE" — big reveal with glow pulse ──
            float nameAlpha = MathHelper.Clamp((_revealTimer - 1.0f) * 1.5f, 0f, 1f) * alpha;
            if (nameAlpha > 0)
            {
                // Glow pulse behind the name
                float pulse = 0.6f + MathF.Sin(_totalTime * 3f) * 0.4f;
                int nameW = PixelFont.MeasureWidth("MEHDI LAKHOUANE", 4);
                int nameX = sw / 2 - nameW / 2;

                // Background glow rectangle
                sb.Draw(px, new Rectangle(nameX - 20, centerY - 12, nameW + 40, 30),
                    Color.Gold * nameAlpha * pulse * 0.08f);

                // Shadow layers (gives depth)
                PixelFont.DrawCentered(sb, "MEHDI LAKHOUANE", centerY + 2,
                    new Color(80, 60, 20) * nameAlpha, 4);
                PixelFont.DrawCentered(sb, "MEHDI LAKHOUANE", centerY + 1,
                    new Color(120, 90, 30) * nameAlpha, 4);

                // Main text with color cycling
                float hue = (_totalTime * 0.5f) % 1f;
                Color nameCol = HueShiftGold(hue, nameAlpha);
                PixelFont.DrawCentered(sb, "MEHDI LAKHOUANE", centerY, nameCol, 4);

                // Bright pixel highlights on letters (shimmer effect)
                if (nameAlpha > 0.8f)
                {
                    int shimmerPos = (int)((_totalTime * 80f) % (nameW + 40)) - 20;
                    for (int i = 0; i < 3; i++)
                    {
                        int sx = nameX + shimmerPos + i * 4 - 4;
                        int sy = centerY - 2 + _rng.Next(4);
                        if (sx > nameX - 10 && sx < nameX + nameW + 10)
                            sb.Draw(px, new Rectangle(sx, sy, 2, 2), Color.White * nameAlpha * 0.7f);
                    }
                }
            }

            // ── Subtitle fade in ──
            float subAlpha = MathHelper.Clamp((_revealTimer - 2.0f) * 1.5f, 0f, 1f) * alpha;
            if (subAlpha > 0)
            {
                PixelFont.DrawCentered(sb, "PRESENTS", centerY + 30,
                    new Color(120, 130, 140) * subAlpha, 2);
            }

            // ── "Press ENTER" prompt ──
            if (_canSkip && _showPrompt)
            {
                PixelFont.DrawCentered(sb, "PRESS ENTER", sh - 40,
                    Color.Gray * alpha * 0.5f, 1);
            }

            // ── Vignette ──
            int vigSize = 100;
            for (int i = 0; i < vigSize; i++)
            {
                float va = (1f - (float)i / vigSize) * 0.7f;
                sb.Draw(px, new Rectangle(0, i, sw, 1), Color.Black * va);
                sb.Draw(px, new Rectangle(0, sh - 1 - i, sw, 1), Color.Black * va);
                sb.Draw(px, new Rectangle(i, 0, 1, sh), Color.Black * va * 0.5f);
                sb.Draw(px, new Rectangle(sw - 1 - i, 0, 1, sh), Color.Black * va * 0.5f);
            }
        }

        private void DrawCorner(SpriteBatch sb, Texture2D px, int x, int y,
            bool flipX, bool flipY, float alpha)
        {
            Color c = Color.Gold * alpha;
            int dx = flipX ? -1 : 1;
            int dy = flipY ? -1 : 1;

            sb.Draw(px, new Rectangle(x, y, 12 * dx, 1), c);
            sb.Draw(px, new Rectangle(x, y, 1, 12 * dy), c);
            sb.Draw(px, new Rectangle(x + 2 * dx, y + 2 * dy, 8 * dx, 1), c * 0.5f);
            sb.Draw(px, new Rectangle(x + 2 * dx, y + 2 * dy, 1, 8 * dy), c * 0.5f);
        }

        private static Color HueShiftGold(float t, float alpha)
        {
            // Shifts between warm gold tones
            float r = 220 + MathF.Sin(t * MathF.PI * 2f) * 35f;
            float g = 180 + MathF.Sin(t * MathF.PI * 2f + 1f) * 30f;
            float b = 60 + MathF.Sin(t * MathF.PI * 2f + 2f) * 40f;
            return new Color((int)r, (int)g, (int)b) * alpha;
        }
    }
}
