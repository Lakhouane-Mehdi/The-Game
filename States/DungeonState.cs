// ============================================================================
// DungeonState.cs — Dungeon Gameplay
// Author: Mehdi Lakhouane
// Description: Interior dungeon state. Simpler than overworld — single room
//              with corridors, breakable pots, and tighter enemy encounters.
// ============================================================================

using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TheGame.Core;
using TheGame.Entities;
using TheGame.Entities.Environment;
using TheGame.Entities.Items;
using TheGame.Systems;
using TheGame.Utils;

namespace TheGame.States
{
    public class DungeonState : GameStateBase
    {
        private Player _player;
        private List<Enemy> _enemies = new();
        private List<SolidRect> _walls = new();
        private List<Breakable> _breakables = new();
        private List<Projectile> _projectiles = new();
        private Texture2D _floorTile;

        public DungeonState(Game1 game, ContentManager content)
            : base(game, content)
        {
            _player = new Player(new Vector2(
                Game1.ScreenWidth / 2f - 32,
                Game1.ScreenHeight - 80));
            _player.LoadContent();
            _player.OnSpawnProjectile = (proj) => _projectiles.Add(proj);

            // Copy inventory from overworld would happen via a shared ref
            // For now, give dungeon player the same items
            string contentPath = System.IO.Path.Combine(
                System.AppDomain.CurrentDomain.BaseDirectory, "Content");
            Inventory.LoadCatalog(contentPath);
            _player.Inventory.AddItem("sword");
            _player.Inventory.AddItem("boomerang");

            if (AssetLoader.Exists("Tiles/stone_floor.png"))
                _floorTile = AssetLoader.LoadTexture("Tiles/stone_floor.png");

            // Enemies
            var b1 = new Bamboo(new Vector2(300, 150));
            b1.LoadContent();
            _enemies.Add(b1);

            var b2 = new Bamboo(new Vector2(450, 250));
            b2.LoadContent();
            _enemies.Add(b2);

            // Breakable pots
            Texture2D potTex = null;
            if (AssetLoader.Exists("Objects/pot.png"))
                potTex = AssetLoader.LoadTexture("Objects/pot.png");
            if (potTex != null)
            {
                _breakables.Add(new Breakable(new Vector2(250, 300), potTex, DropType.Heart));
                _breakables.Add(new Breakable(new Vector2(500, 150), potTex, DropType.Ammo));
            }

            // Walls
            int w = 16;
            _walls = new List<SolidRect>
            {
                new SolidRect(0, 0, Game1.ScreenWidth, w),
                new SolidRect(0, Game1.ScreenHeight - w, Game1.ScreenWidth, w),
                new SolidRect(0, 0, w, Game1.ScreenHeight),
                new SolidRect(Game1.ScreenWidth - w, 0, w, Game1.ScreenHeight),
                new SolidRect(200, 80, w, 300),
                new SolidRect(584, 80, w, 300),
                new SolidRect(350, 180, 48, 48),
                new SolidRect(420, 300, 48, 48),
            };
        }

        public override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            _player.Update(gameTime);
            CollisionSystem.MoveAndResolve(_player, _walls, dt);
            CollisionSystem.ClampToScreen(_player, Game1.ScreenWidth, Game1.ScreenHeight);

            for (int i = _enemies.Count - 1; i >= 0; i--)
            {
                _enemies[i].TargetPosition = _player.Position;
                _enemies[i].Update(gameTime);
                CollisionSystem.MoveAndResolve(_enemies[i], _walls, dt);

                if (_enemies[i].CanRemove)
                {
                    _enemies.RemoveAt(i);
                    continue;
                }
            }

            CollisionSystem.CheckAttackHits(_player, _enemies);
            CollisionSystem.CheckEnemyContact(_player, _enemies);

            // Attack breakables
            if (_player.CurrentStateName == "attack")
            {
                foreach (var b in _breakables)
                    if (!b.IsDestroyed && _player.AttackHitbox.Intersects(b.BoundingBox))
                        b.Destroy();
            }

            // Update breakables and collect drops
            for (int i = _breakables.Count - 1; i >= 0; i--)
            {
                _breakables[i].Update(gameTime);
                DropType drop = _breakables[i].TryCollectDrop(_player.BoundingBox);
                if (drop == DropType.Heart) _player.Heal(2);
                if (_breakables[i].IsDestroyed && _breakables[i].CanRemove)
                    _breakables.RemoveAt(i);
            }

            // Update projectiles
            for (int i = _projectiles.Count - 1; i >= 0; i--)
            {
                _projectiles[i].Update(gameTime, _walls);
                foreach (var enemy in _enemies)
                    if (_projectiles[i].TryHit(enemy))
                        enemy.TakeDamage(_projectiles[i].Damage);
                if (!_projectiles[i].IsAlive) _projectiles.RemoveAt(i);
            }

            if (_player.BoundingBox.Bottom >= Game1.ScreenHeight - 20)
                GameRef.ChangeState(GameState.Overworld);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            Texture2D px = Game1.PixelTexture;

            if (_floorTile != null)
            {
                for (int x = 0; x < Game1.ScreenWidth; x += _floorTile.Width)
                    for (int y = 0; y < Game1.ScreenHeight; y += _floorTile.Height)
                        spriteBatch.Draw(_floorTile, new Vector2(x, y), Color.White * 0.7f);
            }
            else
                spriteBatch.Draw(px, new Rectangle(0, 0, Game1.ScreenWidth, Game1.ScreenHeight), new Color(30, 25, 35));

            Color wallColor = new Color(50, 35, 45);
            foreach (var wall in _walls)
                spriteBatch.Draw(px, wall.Bounds, wallColor);

            spriteBatch.Draw(px, new Rectangle(380, Game1.ScreenHeight - 16, 40, 16), Color.DarkGreen);

            foreach (var b in _breakables) b.Draw(spriteBatch);
            foreach (var enemy in _enemies) enemy.Draw(spriteBatch);
            foreach (var proj in _projectiles) proj.Draw(spriteBatch);

            _player.Draw(spriteBatch);

            spriteBatch.Draw(px, new Rectangle(0, 0, Game1.ScreenWidth, Game1.ScreenHeight), Color.Black * 0.15f);
        }
    }
}
