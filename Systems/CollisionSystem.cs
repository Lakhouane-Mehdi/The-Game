// ============================================================================
// CollisionSystem.cs — AABB Collision Handling
// Author: Mehdi Lakhouane
// Description: Static helper for Axis-Aligned Bounding Box collision.
//              Handles entity-vs-wall, entity-vs-entity, and attack-vs-enemy
//              overlap testing with minimum-penetration resolution.
// ============================================================================

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TheGame.Entities;

namespace TheGame.Systems
{
    /// <summary>
    /// Represents a solid, axis-aligned rectangle in the world
    /// (walls, rocks, water tiles, etc.).
    /// </summary>
    public struct SolidRect
    {
        public Rectangle Bounds;

        public SolidRect(int x, int y, int width, int height)
        {
            Bounds = new Rectangle(x, y, width, height);
        }

        public SolidRect(Rectangle rect)
        {
            Bounds = rect;
        }
    }

    /// <summary>
    /// Pure-static collision utility. No allocations per frame.
    /// </summary>
    public static class CollisionSystem
    {
        // ──────────────────────────────────────────────
        //  1. AABB Overlap Test
        // ──────────────────────────────────────────────

        /// <summary>
        /// Returns true if two rectangles overlap.
        /// </summary>
        public static bool Overlaps(Rectangle a, Rectangle b)
        {
            return a.Intersects(b);
        }

        // ──────────────────────────────────────────────
        //  2. Entity vs. Solid Walls (Slide Resolution)
        // ──────────────────────────────────────────────

        /// <summary>
        /// Moves an entity by its Velocity * dt, then resolves any
        /// overlap with solid rectangles using minimum-penetration push-out.
        /// This allows the player to "slide" along walls instead of sticking.
        /// </summary>
        /// <param name="entity">The entity to move and resolve.</param>
        /// <param name="solids">List of solid wall/obstacle rectangles.</param>
        /// <param name="dt">Delta time for this frame.</param>
        public static void MoveAndResolve(Entity entity, List<SolidRect> solids, float dt)
        {
            // ── Move on X axis first ──
            entity.Position.X += entity.Velocity.X * dt;
            ResolveAxis(entity, solids, resolveX: true);

            // ── Then move on Y axis ──
            entity.Position.Y += entity.Velocity.Y * dt;
            ResolveAxis(entity, solids, resolveX: false);
        }

        /// <summary>
        /// Checks the entity's bounding box against all solids and pushes
        /// it out along the specified axis.
        /// </summary>
        private static void ResolveAxis(Entity entity, List<SolidRect> solids, bool resolveX)
        {
            Rectangle entityBox = entity.BoundingBox;

            for (int i = 0; i < solids.Count; i++)
            {
                Rectangle wall = solids[i].Bounds;
                if (!entityBox.Intersects(wall)) continue;

                // Calculate overlap on each axis
                Rectangle overlap = Rectangle.Intersect(entityBox, wall);

                if (resolveX)
                {
                    // Push out on X axis
                    if (entityBox.Center.X < wall.Center.X)
                        entity.Position.X -= overlap.Width;  // push left
                    else
                        entity.Position.X += overlap.Width;  // push right
                }
                else
                {
                    // Push out on Y axis
                    if (entityBox.Center.Y < wall.Center.Y)
                        entity.Position.Y -= overlap.Height; // push up
                    else
                        entity.Position.Y += overlap.Height; // push down
                }

                // Refresh the bounding box after adjustment
                entityBox = entity.BoundingBox;
            }
        }

        // ──────────────────────────────────────────────
        //  3. Player Attack vs. Enemies
        // ──────────────────────────────────────────────

        /// <summary>
        /// Tests the player's attack hitbox against all enemies.
        /// Applies damage and knockback to any enemy that overlaps.
        /// </summary>
        public static void CheckAttackHits(Player player, List<Enemy> enemies)
        {
            if (player.CurrentStateName != "attack") return;
            if (player.AttackHitbox == Rectangle.Empty) return;

            for (int i = 0; i < enemies.Count; i++)
            {
                Enemy enemy = enemies[i];
                if (!enemy.IsAlive || enemy.IsInvincible) continue;

                if (player.AttackHitbox.Intersects(enemy.BoundingBox))
                {
                    // Set target so HurtState knows knockback direction
                    enemy.TargetPosition = player.Position;
                    enemy.TakeDamage(player.AttackDamage);
                }
            }
        }

        // ──────────────────────────────────────────────
        //  4. Enemy Contact Damage vs. Player
        // ──────────────────────────────────────────────

        /// <summary>
        /// Checks if any alive enemy is overlapping the player.
        /// Applies contact damage to the player.
        /// </summary>
        public static void CheckEnemyContact(Player player, List<Enemy> enemies)
        {
            if (!player.IsAlive || player.IsInvincible) return;

            for (int i = 0; i < enemies.Count; i++)
            {
                Enemy enemy = enemies[i];
                if (!enemy.IsAlive) continue;

                if (player.BoundingBox.Intersects(enemy.BoundingBox))
                {
                    player.TakeDamage(enemy.ContactDamage);
                    break;  // only take damage from one enemy per frame
                }
            }
        }

        // ──────────────────────────────────────────────
        //  5. Screen Boundary Clamping
        // ──────────────────────────────────────────────

        /// <summary>
        /// Prevents an entity from leaving the visible screen area.
        /// </summary>
        public static void ClampToScreen(Entity entity, int screenWidth, int screenHeight)
        {
            if (entity.Position.X + entity.HitboxOffset.X < 0)
                entity.Position.X = -entity.HitboxOffset.X;

            if (entity.Position.Y + entity.HitboxOffset.Y < 0)
                entity.Position.Y = -entity.HitboxOffset.Y;

            if (entity.Position.X + entity.HitboxOffset.X + entity.HitboxWidth > screenWidth)
                entity.Position.X = screenWidth - entity.HitboxOffset.X - entity.HitboxWidth;

            if (entity.Position.Y + entity.HitboxOffset.Y + entity.HitboxHeight > screenHeight)
                entity.Position.Y = screenHeight - entity.HitboxOffset.Y - entity.HitboxHeight;
        }
    }
}
