// ============================================================================
// EnemyProjectile.cs — Projectile fired BY enemies at the player
// Author: Mehdi Lakhouane
// Description: Simple projectile that moves in a straight line and damages
//              the player on contact. Used by Spirit enemies and the boss.
// ============================================================================

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheGame.Core;
using TheGame.Systems;

namespace TheGame.Entities.Items
{
    public class EnemyProjectile
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public int Damage { get; set; } = 1;
        public bool IsAlive { get; private set; } = true;
        public float LifeTime { get; set; } = 3f;

        private float _age;
        private float _trailTimer;
        private Color _color;

        public Rectangle BoundingBox => new Rectangle(
            (int)Position.X - 4, (int)Position.Y - 4, 8, 8);

        public EnemyProjectile(Vector2 position, Vector2 direction, float speed = 150f, int damage = 1)
        {
            Position = position;
            if (direction != Vector2.Zero)
                direction.Normalize();
            Velocity = direction * speed;
            Damage = damage;
            _color = new Color(180, 50, 220); // purple energy
        }

        public void Update(GameTime gameTime, List<SolidRect> walls)
        {
            if (!IsAlive) return;
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            Position += Velocity * dt;
            _age += dt;
            _trailTimer += dt;

            if (_age >= LifeTime)
            {
                IsAlive = false;
                return;
            }

            // Check wall collision
            if (walls != null)
            {
                foreach (var wall in walls)
                {
                    if (BoundingBox.Intersects(wall.Bounds))
                    {
                        IsAlive = false;
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Check if this projectile hits the player. Returns true if hit.
        /// </summary>
        public bool TryHitPlayer(Player player)
        {
            if (!IsAlive || !player.IsAlive || player.IsInvincible) return false;

            if (BoundingBox.Intersects(player.BoundingBox))
            {
                player.TakeDamage(Damage);
                IsAlive = false;
                return true;
            }
            return false;
        }

        public void Draw(SpriteBatch sb)
        {
            if (!IsAlive) return;

            Texture2D px = Game1.PixelTexture;

            // Glow
            float pulse = MathF.Sin(_age * 10f) * 0.2f + 0.8f;
            int glowSize = (int)(16 * pulse);
            sb.Draw(px, new Rectangle(
                (int)Position.X - glowSize / 2,
                (int)Position.Y - glowSize / 2,
                glowSize, glowSize), _color * 0.2f);

            // Core
            sb.Draw(px, new Rectangle(
                (int)Position.X - 3, (int)Position.Y - 3, 6, 6), _color);

            // Bright center
            sb.Draw(px, new Rectangle(
                (int)Position.X - 1, (int)Position.Y - 1, 2, 2), Color.White);
        }
    }
}
