// ============================================================================
// Projectile.cs — Projectile & Boomerang Logic
// Author: Mehdi Lakhouane
// Description: A world-space projectile spawned by item use. Supports two
//              modes: standard (flies until max range or wall hit) and
//              boomerang (flies 150px out, then returns to the player's
//              CURRENT position, ignoring walls on the return trip).
//
// Boomerang math:
//   Phase 1 (outbound): Travel in Facing direction at Speed.
//                        Stop after Range pixels traveled.
//   Phase 2 (return):   Lerp toward player.Position each frame.
//                        Ignore wall collisions. Destroy on reaching player.
// ============================================================================

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheGame.Systems;

namespace TheGame.Entities.Items
{
    public enum ProjectilePhase
    {
        Outbound,
        Returning
    }

    public class Projectile
    {
        // ── Transform ──
        public Vector2 Position;
        public Vector2 Velocity;
        public float Rotation;

        // ── Properties ──
        public Texture2D Sprite { get; set; }
        public int Damage { get; set; }
        public float Speed { get; set; }
        public float MaxRange { get; set; }
        public bool IsBoomerang { get; set; }
        public bool IsAlive { get; set; } = true;

        // ── Tracking ──
        private Vector2 _origin;
        private float _distanceTraveled;

        // ── Boomerang phase ──
        public ProjectilePhase Phase { get; private set; } = ProjectilePhase.Outbound;
        private Func<Vector2> _getPlayerPos; // closure to get live player position
        private float _returnSpeed;

        // ── Hitbox ──
        public Rectangle BoundingBox => new Rectangle(
            (int)Position.X + 4, (int)Position.Y + 4, 24, 24);

        // ── Already-hit tracking (prevent multi-hit on same enemy) ──
        private readonly HashSet<Entity> _hitEntities = new();

        // ── Spin animation (boomerang) ──
        private float _spinAngle;

        /// <summary>
        /// Creates a new projectile.
        /// </summary>
        /// <param name="startPos">World position to spawn at.</param>
        /// <param name="direction">Normalised direction vector.</param>
        /// <param name="speed">Pixels per second.</param>
        /// <param name="maxRange">Max distance before dying (or before boomerang return).</param>
        /// <param name="damage">Damage dealt on hit.</param>
        /// <param name="isBoomerang">If true, returns to the player after maxRange.</param>
        /// <param name="getPlayerPos">Function that returns the player's current position (for boomerang return).</param>
        public Projectile(Vector2 startPos, Vector2 direction, float speed,
            float maxRange, int damage, bool isBoomerang, Func<Vector2> getPlayerPos = null)
        {
            Position = startPos;
            _origin = startPos;
            Speed = speed;
            MaxRange = maxRange;
            Damage = damage;
            IsBoomerang = isBoomerang;
            _getPlayerPos = getPlayerPos;
            _returnSpeed = speed * 1.2f; // return slightly faster

            if (direction != Vector2.Zero)
                direction.Normalize();
            Velocity = direction * speed;
            Rotation = MathF.Atan2(direction.Y, direction.X);
        }

        // ── Update ──

        public void Update(GameTime gameTime, List<SolidRect> walls)
        {
            if (!IsAlive) return;
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            switch (Phase)
            {
                case ProjectilePhase.Outbound:
                    UpdateOutbound(dt, walls);
                    break;

                case ProjectilePhase.Returning:
                    UpdateReturn(dt);
                    break;
            }

            // Spin for boomerang visual
            if (IsBoomerang)
                _spinAngle += dt * 15f;
        }

        /// <summary>
        /// Outbound: fly in the initial direction. Die on wall hit (if not boomerang)
        /// or switch to return phase at max range (if boomerang).
        /// </summary>
        private void UpdateOutbound(float dt, List<SolidRect> walls)
        {
            Position += Velocity * dt;
            _distanceTraveled += Speed * dt;

            // Check wall collision
            foreach (var wall in walls)
            {
                if (BoundingBox.Intersects(wall.Bounds))
                {
                    if (IsBoomerang)
                    {
                        // Boomerang bounces off walls → start return
                        Phase = ProjectilePhase.Returning;
                        _hitEntities.Clear(); // can hit again on return
                        return;
                    }
                    else
                    {
                        // Normal projectile dies on wall hit
                        IsAlive = false;
                        return;
                    }
                }
            }

            // Max range reached
            if (_distanceTraveled >= MaxRange)
            {
                if (IsBoomerang)
                {
                    Phase = ProjectilePhase.Returning;
                    _hitEntities.Clear();
                }
                else
                {
                    IsAlive = false;
                }
            }
        }

        /// <summary>
        /// Return phase: home in on the player's current position.
        /// Ignores wall collisions. Dies when it reaches the player.
        /// </summary>
        private void UpdateReturn(float dt)
        {
            if (_getPlayerPos == null)
            {
                IsAlive = false;
                return;
            }

            Vector2 playerPos = _getPlayerPos();
            Vector2 toPlayer = playerPos - Position;
            float dist = toPlayer.Length();

            if (dist < 16f)
            {
                // Reached the player — destroy
                IsAlive = false;
                return;
            }

            // Steer toward player
            toPlayer.Normalize();
            Velocity = toPlayer * _returnSpeed;
            Position += Velocity * dt;
            Rotation = MathF.Atan2(toPlayer.Y, toPlayer.X);
        }

        // ── Collision with entities ──

        /// <summary>
        /// Checks if this projectile hits an entity. Tracks already-hit
        /// entities to prevent multi-hit (except on boomerang return).
        /// </summary>
        public bool TryHit(Entity target)
        {
            if (!IsAlive || !target.IsAlive) return false;
            if (_hitEntities.Contains(target)) return false;

            if (BoundingBox.Intersects(target.BoundingBox))
            {
                _hitEntities.Add(target);

                // Normal projectile dies on hit; boomerang keeps going
                if (!IsBoomerang)
                    IsAlive = false;

                return true;
            }

            return false;
        }

        // ── Drawing ──

        public void Draw(SpriteBatch sb)
        {
            if (!IsAlive || Sprite == null) return;

            if (IsBoomerang)
            {
                // Spin effect
                Vector2 origin = new Vector2(Sprite.Width / 2f, Sprite.Height / 2f);
                sb.Draw(Sprite, Position + origin, null, Color.White,
                    _spinAngle, origin, 1f, SpriteEffects.None, 0f);
            }
            else
            {
                sb.Draw(Sprite, Position, null, Color.White,
                    Rotation, Vector2.Zero, 1f, SpriteEffects.None, 0f);
            }
        }
    }
}
