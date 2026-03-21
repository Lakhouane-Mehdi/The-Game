// ============================================================================
// RoomManager.cs — Room-Based Entity Manager
// Author: Mehdi Lakhouane
// Description: Tracks which room each entity belongs to. Only entities in the
//              camera's current room are updated (AI ticked) and drawn.
//              Enemies in off-screen rooms are "frozen" — they don't move,
//              don't take damage, and don't render.
// ============================================================================

using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheGame.Entities;
using TheGame.Entities.Environment;

namespace TheGame.Systems
{
    /// <summary>
    /// Wrapper that pairs an entity with the room it belongs to.
    /// </summary>
    public class RoomEntity<T>
    {
        public T Entity;
        public int RoomX;
        public int RoomY;
        public bool IsActive;   // set by RoomManager based on current room

        public RoomEntity(T entity, int roomX, int roomY)
        {
            Entity = entity;
            RoomX = roomX;
            RoomY = roomY;
            IsActive = false;
        }
    }

    /// <summary>
    /// Manages all room-tagged entities. Updates active flags when the
    /// camera changes rooms.
    /// </summary>
    public class RoomManager
    {
        private readonly Camera _camera;

        public List<RoomEntity<Enemy>> Enemies { get; } = new();
        public List<RoomEntity<Breakable>> Breakables { get; } = new();
        public List<RoomEntity<PressurePlate>> Triggers { get; } = new();

        public RoomManager(Camera camera)
        {
            _camera = camera;
        }

        // ── Registration ──

        public void AddEnemy(Enemy enemy, int roomX, int roomY)
        {
            Enemies.Add(new RoomEntity<Enemy>(enemy, roomX, roomY));
        }

        public void AddBreakable(Breakable b, int roomX, int roomY)
        {
            Breakables.Add(new RoomEntity<Breakable>(b, roomX, roomY));
        }

        public void AddTrigger(PressurePlate t, int roomX, int roomY)
        {
            Triggers.Add(new RoomEntity<PressurePlate>(t, roomX, roomY));
        }

        // ── Activation ──

        /// <summary>
        /// Call when the camera changes rooms (or at startup).
        /// Activates entities in the current room, deactivates all others.
        /// </summary>
        public void RefreshActiveRoom()
        {
            int rx = _camera.RoomX;
            int ry = _camera.RoomY;

            foreach (var re in Enemies)
                re.IsActive = (re.RoomX == rx && re.RoomY == ry);

            foreach (var re in Breakables)
                re.IsActive = (re.RoomX == rx && re.RoomY == ry);

            foreach (var re in Triggers)
                re.IsActive = (re.RoomX == rx && re.RoomY == ry);
        }

        // ── Update (only active entities) ──

        public void UpdateEnemies(GameTime gameTime, Vector2 playerPos)
        {
            for (int i = Enemies.Count - 1; i >= 0; i--)
            {
                var re = Enemies[i];
                if (!re.IsActive) continue;

                re.Entity.TargetPosition = playerPos;
                re.Entity.Update(gameTime);

                if (re.Entity.CanRemove)
                {
                    Enemies.RemoveAt(i);
                    continue;
                }
            }
        }

        public void UpdateBreakables(GameTime gameTime)
        {
            for (int i = Breakables.Count - 1; i >= 0; i--)
            {
                var re = Breakables[i];
                if (!re.IsActive) continue;

                re.Entity.Update(gameTime);

                if (re.Entity.IsDestroyed && re.Entity.CanRemove)
                {
                    Breakables.RemoveAt(i);
                    continue;
                }
            }
        }

        public void UpdateTriggers(GameTime gameTime, Rectangle playerBB)
        {
            foreach (var re in Triggers)
            {
                if (!re.IsActive) continue;
                re.Entity.Update(gameTime, playerBB);
            }
        }

        // ── Draw (only active entities) ──

        public void DrawEnemies(SpriteBatch sb)
        {
            foreach (var re in Enemies)
            {
                if (!re.IsActive) continue;
                re.Entity.Draw(sb);
            }
        }

        public void DrawBreakables(SpriteBatch sb)
        {
            foreach (var re in Breakables)
            {
                if (!re.IsActive) continue;
                re.Entity.Draw(sb);
            }
        }

        public void DrawTriggers(SpriteBatch sb)
        {
            foreach (var re in Triggers)
            {
                if (!re.IsActive) continue;
                re.Entity.Draw(sb);
            }
        }

        // ── Helpers ──

        /// <summary>
        /// Returns only the active enemies (for collision checks).
        /// </summary>
        public List<Enemy> GetActiveEnemies()
        {
            var result = new List<Enemy>();
            foreach (var re in Enemies)
                if (re.IsActive && re.Entity.IsAlive)
                    result.Add(re.Entity);
            return result;
        }

        /// <summary>
        /// Returns only the active breakables (for attack collision).
        /// </summary>
        public List<Breakable> GetActiveBreakables()
        {
            var result = new List<Breakable>();
            foreach (var re in Breakables)
                if (re.IsActive && !re.Entity.IsDestroyed)
                    result.Add(re.Entity);
            return result;
        }
    }
}
