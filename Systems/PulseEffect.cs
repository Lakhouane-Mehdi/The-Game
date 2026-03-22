// ============================================================================
// PulseEffect.cs — Echo Pulse Reveal System
// Author: Mehdi Lakhouane
// Description: When the boomerang (Echo) hits a Sound-Crystal tile (ID 55),
//              a pulse radiates outward, revealing hidden paths and enemies
//              for a limited duration. The pulse is a visual ring that expands.
// ============================================================================

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheGame.Core;

namespace TheGame.Systems
{
    public class PulseEffect
    {
        public Vector2 Origin { get; }
        public float MaxRadius { get; }
        public float Duration { get; }
        public float RevealDuration { get; }
        public bool IsActive { get; private set; }
        public bool IsRevealing { get; private set; }

        private float _timer;
        private float _revealTimer;
        private float _currentRadius;

        // ── Ring rendering ──
        private const int RingSegments = 48;
        private const float RingThickness = 3f;
        private const float ExpandSpeed = 200f; // pixels per second

        /// <summary>
        /// Creates a new pulse effect at the given origin.
        /// </summary>
        /// <param name="origin">World position where the pulse starts.</param>
        /// <param name="maxRadius">Maximum radius the ring expands to.</param>
        /// <param name="revealDuration">How long hidden objects stay revealed (seconds).</param>
        public PulseEffect(Vector2 origin, float maxRadius = 120f, float revealDuration = 3f)
        {
            Origin = origin;
            MaxRadius = maxRadius;
            Duration = maxRadius / ExpandSpeed;
            RevealDuration = revealDuration;
            IsActive = true;
            IsRevealing = true;
            _timer = 0f;
            _revealTimer = 0f;
            _currentRadius = 0f;
        }

        public void Update(GameTime gameTime)
        {
            if (!IsActive) return;

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Expand the ring
            if (_timer < Duration)
            {
                _timer += dt;
                _currentRadius = MathF.Min(_timer * ExpandSpeed, MaxRadius);
            }

            // Track reveal duration
            _revealTimer += dt;
            if (_revealTimer >= RevealDuration)
            {
                IsRevealing = false;
            }
            if (_revealTimer >= RevealDuration + 0.5f)
            {
                IsActive = false;
            }
        }

        /// <summary>
        /// Checks if a world position is within the revealed area.
        /// </summary>
        public bool IsPositionRevealed(Vector2 position)
        {
            if (!IsRevealing) return false;
            float dist = Vector2.Distance(Origin, position);
            return dist <= _currentRadius;
        }

        /// <summary>
        /// Checks if a rectangle overlaps the revealed area.
        /// </summary>
        public bool IsRectRevealed(Rectangle rect)
        {
            if (!IsRevealing) return false;
            // Check if center of rect is within pulse radius
            Vector2 center = new Vector2(rect.Center.X, rect.Center.Y);
            return Vector2.Distance(Origin, center) <= _currentRadius;
        }

        public void Draw(SpriteBatch sb)
        {
            if (!IsActive) return;

            Texture2D px = Game1.PixelTexture;

            // Fade out as reveal timer expires
            float alpha = IsRevealing
                ? 0.6f
                : MathF.Max(0f, 1f - (_revealTimer - RevealDuration) / 0.5f) * 0.3f;

            // Draw expanding ring using small rectangles
            if (_timer < Duration)
            {
                float ringAlpha = (1f - _timer / Duration) * 0.8f;
                DrawRing(sb, px, _currentRadius, Color.Cyan * ringAlpha, RingThickness);
            }

            // Draw subtle revealed area fill
            if (IsRevealing)
            {
                float fillAlpha = 0.05f + 0.03f * MathF.Sin(_revealTimer * 4f);
                DrawFilledCircle(sb, px, _currentRadius, Color.Cyan * fillAlpha);
            }

            // Inner glow at origin
            if (_timer < Duration * 2f)
            {
                float glowSize = 8f * (1f - MathF.Min(_timer / (Duration * 2f), 1f));
                sb.Draw(px, new Rectangle(
                    (int)(Origin.X - glowSize), (int)(Origin.Y - glowSize),
                    (int)(glowSize * 2), (int)(glowSize * 2)),
                    Color.White * alpha);
            }
        }

        private void DrawRing(SpriteBatch sb, Texture2D px, float radius, Color color, float thickness)
        {
            for (int i = 0; i < RingSegments; i++)
            {
                float angle = (float)i / RingSegments * MathF.PI * 2f;
                float x = Origin.X + MathF.Cos(angle) * radius;
                float y = Origin.Y + MathF.Sin(angle) * radius;
                sb.Draw(px, new Rectangle((int)x, (int)y, (int)thickness, (int)thickness), color);
            }
        }

        private void DrawFilledCircle(SpriteBatch sb, Texture2D px, float radius, Color color)
        {
            // Approximate filled circle with horizontal lines
            int r = (int)radius;
            for (int dy = -r; dy <= r; dy += 4) // step by 4 for performance
            {
                float halfW = MathF.Sqrt(radius * radius - dy * dy);
                sb.Draw(px, new Rectangle(
                    (int)(Origin.X - halfW), (int)(Origin.Y + dy),
                    (int)(halfW * 2), 4), color);
            }
        }
    }
}
