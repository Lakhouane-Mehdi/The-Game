// ============================================================================
// Breakable.cs — Breakable World Object (Bush, Pot, Crate)
// Author: Mehdi Lakhouane
// Description: A destructible object in the world. When hit by the player's
//              sword or a projectile, it plays a destruction effect and
//              optionally drops a pickup (heart, ammo, key, etc.).
//
// Uses the Ninja Adventure pot, crate, and grass sprites.
// ============================================================================

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheGame.Core;

namespace TheGame.Entities.Environment
{
    /// <summary>
    /// What a breakable drops when destroyed.
    /// </summary>
    public enum DropType
    {
        None,
        Heart,       // restores 2 HP
        Ammo,        // restores 3 arrows / bombs
        Key          // opens a locked door
    }

    public class Breakable
    {
        // ── Transform ──
        public Vector2 Position;
        public Texture2D Sprite { get; set; }

        // ── Collision ──
        public int Width { get; set; } = 32;
        public int Height { get; set; } = 32;
        public Rectangle BoundingBox => new Rectangle(
            (int)Position.X, (int)Position.Y, Width, Height);

        // ── State ──
        public bool IsDestroyed { get; private set; }
        public bool CanRemove { get; private set; }

        // ── Destruction animation ──
        private float _destroyTimer;
        private const float DestroyFadeTime = 0.3f;

        // ── Drop ──
        public DropType Drop { get; set; } = DropType.None;
        public bool HasDropped { get; private set; }

        // ── Drop pickup state ──
        public Vector2 DropPosition { get; private set; }
        public bool DropActive { get; private set; }
        private float _dropLifeTimer;
        private const float DropLifeTime = 5f; // seconds before drop disappears

        // ── Static RNG for drop variation ──
        private static readonly Random _rng = new();

        public Breakable(Vector2 position, Texture2D sprite, DropType drop = DropType.None)
        {
            Position = position;
            Sprite = sprite;
            Drop = drop;
        }

        /// <summary>
        /// Call when the player's attack hitbox or a projectile hits this object.
        /// </summary>
        public void Destroy()
        {
            if (IsDestroyed) return;
            IsDestroyed = true;
            _destroyTimer = 0f;

            // Spawn drop slightly offset
            if (Drop != DropType.None)
            {
                float offsetX = (_rng.NextSingle() - 0.5f) * 16f;
                float offsetY = (_rng.NextSingle() - 0.5f) * 16f;
                DropPosition = Position + new Vector2(Width / 2f + offsetX, Height / 2f + offsetY);
                DropActive = true;
                HasDropped = true;
            }
        }

        public void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (IsDestroyed)
            {
                _destroyTimer += dt;
                if (_destroyTimer >= DestroyFadeTime)
                    CanRemove = !DropActive; // keep alive while drop is on ground
            }

            // Drop lifetime
            if (DropActive)
            {
                _dropLifeTimer += dt;
                if (_dropLifeTimer >= DropLifeTime)
                {
                    DropActive = false;
                    CanRemove = true;
                }
            }
        }

        /// <summary>
        /// Try to collect the drop. Returns the drop type if collected.
        /// </summary>
        public DropType TryCollectDrop(Rectangle playerBB)
        {
            if (!DropActive) return DropType.None;

            Rectangle dropBB = new Rectangle(
                (int)DropPosition.X - 8, (int)DropPosition.Y - 8, 16, 16);

            if (playerBB.Intersects(dropBB))
            {
                DropActive = false;
                CanRemove = true;
                return Drop;
            }

            return DropType.None;
        }

        public void Draw(SpriteBatch sb)
        {
            Texture2D px = Game1.PixelTexture;

            // Draw the breakable (with fade on destroy)
            if (!IsDestroyed && Sprite != null)
            {
                sb.Draw(Sprite, Position, Color.White);
            }
            else if (IsDestroyed && _destroyTimer < DestroyFadeTime && Sprite != null)
            {
                // Shatter effect: shrink + fade
                float t = _destroyTimer / DestroyFadeTime;
                float scale = 1f - t * 0.5f;
                float alpha = 1f - t;
                Vector2 origin = new Vector2(Sprite.Width / 2f, Sprite.Height / 2f);
                sb.Draw(Sprite, Position + origin, null, Color.White * alpha,
                    t * 2f, origin, scale, SpriteEffects.None, 0f);
            }

            // Draw the drop
            if (DropActive)
            {
                // Pulsing pickup
                float pulse = 1f + MathF.Sin(_dropLifeTimer * 6f) * 0.15f;
                int size = (int)(12 * pulse);
                Color dropColor = Drop switch
                {
                    DropType.Heart => Color.Red,
                    DropType.Ammo => Color.Cyan,
                    DropType.Key => Color.Gold,
                    _ => Color.White
                };
                sb.Draw(px, new Rectangle(
                    (int)DropPosition.X - size / 2,
                    (int)DropPosition.Y - size / 2,
                    size, size), dropColor);
            }
        }
    }
}
