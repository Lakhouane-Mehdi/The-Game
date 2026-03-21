// ============================================================================
// OverworldState.cs — Overworld Gameplay (Full Featured)
// Author: Mehdi Lakhouane
// Description: Main outdoor gameplay. Integrates:
//   - Zelda-style screen-scrolling camera
//   - Room-based enemy activation/deactivation
//   - Inventory & item use (Boomerang, Bow, Bombs)
//   - Projectile management
//   - Breakable objects with item drops
//   - Pressure plate triggers that open doors
//   - HUD: hearts + item slot
// ============================================================================

using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TheGame.Core;
using TheGame.Entities;
using TheGame.Entities.Environment;
using TheGame.Entities.Items;
using TheGame.Systems;
using TheGame.Utils;

namespace TheGame.States
{
    public class OverworldState : GameStateBase
    {
        // ── Camera ──
        private Camera _camera;

        // ── Entities ──
        private Player _player;
        private RoomManager _rooms;

        // ── Projectiles (live in world space) ──
        private readonly List<Projectile> _projectiles = new();

        // ── World walls (per room — keyed by "rx,ry") ──
        private readonly Dictionary<string, List<SolidRect>> _roomWalls = new();

        // ── Toggleable walls (opened by pressure plates) ──
        private readonly List<SolidRect> _doorWalls = new();
        private readonly Dictionary<string, SolidRect> _namedDoors = new();

        // ── Tiles ──
        private Texture2D _floorTile;
        private Texture2D _dungeonFloorTile;

        // ── UI / HUD ──
        private Texture2D _heartFull;
        private Texture2D _heartHalf;
        private Texture2D _heartEmpty;

        // ── Object sprites ──
        private Texture2D _potSprite;
        private Texture2D _crateSprite;
        private Texture2D _grassSprite;

        // ── Audio ──
        private SoundEffect _swordSfx;
        private SoundEffect _hitSfx;

        // ── Input edge detection ──
        private KeyboardState _prevKb;

        // ── World size: 3x2 rooms ──
        private const int WorldRoomsX = 3;
        private const int WorldRoomsY = 2;

        public OverworldState(Game1 game, ContentManager content)
            : base(game, content)
        {
            // ── Camera ──
            _camera = new Camera(Game1.ScreenWidth, Game1.ScreenHeight, WorldRoomsX, WorldRoomsY);
            _camera.SnapToRoom(1, 0); // start in the center-top room

            // ── Room Manager ──
            _rooms = new RoomManager(_camera);

            // ── Player ──
            _player = new Player(new Vector2(
                1 * Game1.ScreenWidth + Game1.ScreenWidth / 2f - 32,
                0 * Game1.ScreenHeight + Game1.ScreenHeight / 2f - 32));
            _player.LoadContent();

            // Wire projectile spawning
            _player.OnSpawnProjectile = (proj) => _projectiles.Add(proj);

            // ── Load item catalog and give starter items ──
            string contentPath = System.IO.Path.Combine(
                System.AppDomain.CurrentDomain.BaseDirectory, "Content");
            Inventory.LoadCatalog(contentPath);
            _player.Inventory.AddItem("sword");
            _player.Inventory.AddItem("boomerang");

            // ── Load assets ──
            LoadAssets();

            // ── Build world ──
            BuildRoomWalls();
            SpawnEnemies();
            SpawnBreakables();
            SpawnTriggers();

            // ── Activate initial room ──
            _rooms.RefreshActiveRoom();
        }

        // ──────────────────────────────────────────────
        //  Asset Loading
        // ──────────────────────────────────────────────

        private void LoadAssets()
        {
            if (AssetLoader.Exists("Tiles/ground.png"))
                _floorTile = AssetLoader.LoadTexture("Tiles/ground.png");
            if (AssetLoader.Exists("Tiles/Floor.png"))
                _dungeonFloorTile = AssetLoader.LoadTexture("Tiles/Floor.png");

            if (AssetLoader.Exists("UI/heart.png"))
                _heartFull = AssetLoader.LoadTexture("UI/heart.png");
            if (AssetLoader.Exists("UI/heart_2.png"))
                _heartHalf = AssetLoader.LoadTexture("UI/heart_2.png");
            if (AssetLoader.Exists("UI/heart_3.png"))
                _heartEmpty = AssetLoader.LoadTexture("UI/heart_3.png");

            if (AssetLoader.Exists("Objects/pot.png"))
                _potSprite = AssetLoader.LoadTexture("Objects/pot.png");
            if (AssetLoader.Exists("Objects/crate.png"))
                _crateSprite = AssetLoader.LoadTexture("Objects/crate.png");
            if (AssetLoader.Exists("Objects/grass_1.png"))
                _grassSprite = AssetLoader.LoadTexture("Objects/grass_1.png");

            if (AssetLoader.Exists("Audio/SFX/sword.wav"))
                _swordSfx = AssetLoader.LoadSound("Audio/SFX/sword.wav");
            if (AssetLoader.Exists("Audio/SFX/hit.wav"))
                _hitSfx = AssetLoader.LoadSound("Audio/SFX/hit.wav");
        }

        // ──────────────────────────────────────────────
        //  World Building — Walls per Room
        // ──────────────────────────────────────────────

        private void BuildRoomWalls()
        {
            int w = Game1.ScreenWidth;
            int h = Game1.ScreenHeight;
            int t = 16; // wall thickness

            // For each room, generate border walls with openings for exits
            for (int ry = 0; ry < WorldRoomsY; ry++)
            {
                for (int rx = 0; rx < WorldRoomsX; rx++)
                {
                    int ox = rx * w; // room origin X
                    int oy = ry * h; // room origin Y
                    var walls = new List<SolidRect>();

                    // ── Top wall (opening in center if room above exists) ──
                    if (ry > 0)
                    {
                        walls.Add(new SolidRect(ox, oy, w / 2 - 30, t));
                        walls.Add(new SolidRect(ox + w / 2 + 30, oy, w / 2 - 30, t));
                    }
                    else
                        walls.Add(new SolidRect(ox, oy, w, t));

                    // ── Bottom wall ──
                    if (ry < WorldRoomsY - 1)
                    {
                        walls.Add(new SolidRect(ox, oy + h - t, w / 2 - 30, t));
                        walls.Add(new SolidRect(ox + w / 2 + 30, oy + h - t, w / 2 - 30, t));
                    }
                    else
                        walls.Add(new SolidRect(ox, oy + h - t, w, t));

                    // ── Left wall ──
                    if (rx > 0)
                    {
                        walls.Add(new SolidRect(ox, oy, t, h / 2 - 30));
                        walls.Add(new SolidRect(ox, oy + h / 2 + 30, t, h / 2 - 30));
                    }
                    else
                        walls.Add(new SolidRect(ox, oy, t, h));

                    // ── Right wall ──
                    if (rx < WorldRoomsX - 1)
                    {
                        walls.Add(new SolidRect(ox + w - t, oy, t, h / 2 - 30));
                        walls.Add(new SolidRect(ox + w - t, oy + h / 2 + 30, t, h / 2 - 30));
                    }
                    else
                        walls.Add(new SolidRect(ox + w - t, oy, t, h));

                    _roomWalls[$"{rx},{ry}"] = walls;
                }
            }

            // ── Add obstacles in specific rooms ──

            // Room (1,0) — starting room: a few rocks
            AddWall(1, 0, new SolidRect(1 * Game1.ScreenWidth + 350, 200, 48, 48));
            AddWall(1, 0, new SolidRect(1 * Game1.ScreenWidth + 150, 350, 48, 48));

            // Room (0,0) — west room: maze-like corridors
            AddWall(0, 0, new SolidRect(200, 100, 16, 280));
            AddWall(0, 0, new SolidRect(500, 100, 16, 280));

            // Room (2,0) — east room: obstacle course
            AddWall(2, 0, new SolidRect(2 * Game1.ScreenWidth + 200, 150, 48, 48));
            AddWall(2, 0, new SolidRect(2 * Game1.ScreenWidth + 400, 250, 48, 48));
            AddWall(2, 0, new SolidRect(2 * Game1.ScreenWidth + 600, 150, 48, 48));

            // Room (1,1) — south room: has a door opened by a pressure plate
            var doorWall = new SolidRect(1 * Game1.ScreenWidth + 370, Game1.ScreenHeight + 200, 60, 16);
            _namedDoors["south_door"] = doorWall;
            AddWall(1, 1, doorWall);
        }

        private void AddWall(int rx, int ry, SolidRect wall)
        {
            string key = $"{rx},{ry}";
            if (_roomWalls.ContainsKey(key))
                _roomWalls[key].Add(wall);
        }

        // ──────────────────────────────────────────────
        //  Spawning Entities
        // ──────────────────────────────────────────────

        private void SpawnEnemies()
        {
            int w = Game1.ScreenWidth;
            int h = Game1.ScreenHeight;

            // Room (1,0) — starting room: 2 bamboos
            var b1 = new Bamboo(new Vector2(w + 200, 150));
            b1.LoadContent();
            _rooms.AddEnemy(b1, 1, 0);

            var b2 = new Bamboo(new Vector2(w + 600, 350));
            b2.LoadContent();
            _rooms.AddEnemy(b2, 1, 0);

            // Room (0,0) — west room: raccoon boss
            var raccoon = new Raccoon(new Vector2(350, 240));
            raccoon.LoadContent();
            _rooms.AddEnemy(raccoon, 0, 0);

            // Room (2,0) — east room: 3 bamboos
            for (int i = 0; i < 3; i++)
            {
                var b = new Bamboo(new Vector2(2 * w + 150 + i * 200, 200 + i * 50));
                b.LoadContent();
                _rooms.AddEnemy(b, 2, 0);
            }

            // Room (1,1) — south room: bamboo guards
            var g1 = new Bamboo(new Vector2(w + 250, h + 150));
            g1.LoadContent();
            _rooms.AddEnemy(g1, 1, 1);

            var g2 = new Bamboo(new Vector2(w + 550, h + 300));
            g2.LoadContent();
            _rooms.AddEnemy(g2, 1, 1);
        }

        private void SpawnBreakables()
        {
            int w = Game1.ScreenWidth;
            int h = Game1.ScreenHeight;

            // Room (1,0) — pots that drop hearts
            Texture2D potTex = _potSprite ?? _grassSprite;
            if (potTex != null)
            {
                _rooms.AddBreakable(
                    new Breakable(new Vector2(w + 100, 100), potTex, DropType.Heart), 1, 0);
                _rooms.AddBreakable(
                    new Breakable(new Vector2(w + 680, 100), potTex, DropType.Heart), 1, 0);
            }

            // Room (0,0) — crates that drop ammo
            Texture2D crateTex = _crateSprite ?? potTex;
            if (crateTex != null)
            {
                _rooms.AddBreakable(
                    new Breakable(new Vector2(100, 350), crateTex, DropType.Ammo), 0, 0);
                _rooms.AddBreakable(
                    new Breakable(new Vector2(650, 200), crateTex, DropType.Ammo), 0, 0);
            }

            // Room (2,0) — grass bushes (just for fun, no drops)
            if (_grassSprite != null)
            {
                _rooms.AddBreakable(
                    new Breakable(new Vector2(2 * w + 300, 350), _grassSprite), 2, 0);
                _rooms.AddBreakable(
                    new Breakable(new Vector2(2 * w + 500, 350), _grassSprite), 2, 0);
            }

            // Room (1,1) — pots guarding the pressure plate room
            if (potTex != null)
            {
                _rooms.AddBreakable(
                    new Breakable(new Vector2(w + 300, h + 100), potTex, DropType.Heart), 1, 1);
                _rooms.AddBreakable(
                    new Breakable(new Vector2(w + 500, h + 100), potTex, DropType.None), 1, 1);
            }
        }

        private void SpawnTriggers()
        {
            int w = Game1.ScreenWidth;
            int h = Game1.ScreenHeight;

            // Room (1,1) — pressure plate that opens the south door
            var plate = new PressurePlate(
                new Vector2(w + 400, h + 350), TriggerMode.Toggle);

            plate.OnStateChanged = (activated) =>
            {
                if (_namedDoors.TryGetValue("south_door", out SolidRect door))
                {
                    string key = "1,1";
                    if (activated)
                        _roomWalls[key].Remove(door);  // open door
                    else
                    {
                        if (!_roomWalls[key].Contains(door))
                            _roomWalls[key].Add(door);  // close door
                    }
                }
            };

            _rooms.AddTrigger(plate, 1, 1);
        }

        // ──────────────────────────────────────────────
        //  Get walls for the current room
        // ──────────────────────────────────────────────

        private List<SolidRect> GetCurrentWalls()
        {
            string key = $"{_camera.RoomX},{_camera.RoomY}";
            return _roomWalls.ContainsKey(key) ? _roomWalls[key] : new List<SolidRect>();
        }

        // ──────────────────────────────────────────────
        //  UPDATE
        // ──────────────────────────────────────────────

        public override void Update(GameTime gameTime)
        {
            KeyboardState kb = Keyboard.GetState();
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // ── Pause ──
            if (kb.IsKeyDown(Keys.P) && _prevKb.IsKeyUp(Keys.P))
            {
                GameRef.ChangeState(GameState.Paused);
                _prevKb = kb;
                return;
            }

            // ── Camera transition (gameplay frozen during slide) ──
            if (_camera.IsTransitioning)
            {
                _camera.Update(gameTime);
                _prevKb = kb;
                return;
            }

            // ── Check room transition ──
            if (_camera.CheckTransition(_player.Position, _player.BoundingBox, out Vector2 newPos))
            {
                _player.Position = newPos;
                _rooms.RefreshActiveRoom();
                _camera.Update(gameTime);
                _prevKb = kb;
                return;
            }

            // ── Track state for SFX ──
            string prevState = _player.CurrentStateName;

            // ── 1. Update player ──
            _player.Update(gameTime);

            if (_player.CurrentStateName == "attack" && prevState != "attack")
                _swordSfx?.Play(0.5f, 0f, 0f);

            // ── 2. Resolve player vs. walls ──
            var walls = GetCurrentWalls();
            CollisionSystem.MoveAndResolve(_player, walls, 0);

            // ── 3. Update enemies (only in current room) ──
            _rooms.UpdateEnemies(gameTime, _player.Position);

            // Resolve enemies vs walls
            var activeEnemies = _rooms.GetActiveEnemies();
            foreach (var enemy in activeEnemies)
                CollisionSystem.MoveAndResolve(enemy, walls, 0);

            // ── 4. Combat: attack vs enemies ──
            int prevHealth = _player.CurrentHealth;
            CollisionSystem.CheckAttackHits(_player, activeEnemies);
            CollisionSystem.CheckEnemyContact(_player, activeEnemies);

            if (_player.CurrentHealth < prevHealth)
                _hitSfx?.Play(0.6f, 0f, 0f);

            // ── 5. Attack vs breakables ──
            if (_player.CurrentStateName == "attack")
            {
                foreach (var b in _rooms.GetActiveBreakables())
                {
                    if (!b.IsDestroyed && _player.AttackHitbox.Intersects(b.BoundingBox))
                        b.Destroy();
                }
            }

            // ── 6. Update breakables & collect drops ──
            _rooms.UpdateBreakables(gameTime);
            foreach (var re in _rooms.Breakables)
            {
                if (!re.IsActive) continue;
                DropType drop = re.Entity.TryCollectDrop(_player.BoundingBox);
                if (drop == DropType.Heart)
                    _player.Heal(2);
                else if (drop == DropType.Ammo)
                {
                    var item = _player.Inventory.EquippedItem;
                    if (item != null)
                        _player.Inventory.AddAmmo(item.Id, 3);
                }
            }

            // ── 7. Update triggers ──
            _rooms.UpdateTriggers(gameTime, _player.BoundingBox);

            // ── 8. Update projectiles ──
            for (int i = _projectiles.Count - 1; i >= 0; i--)
            {
                var proj = _projectiles[i];
                proj.Update(gameTime, walls);

                // Check projectile vs enemies
                foreach (var enemy in activeEnemies)
                {
                    if (proj.TryHit(enemy))
                    {
                        enemy.TargetPosition = _player.Position;
                        enemy.TakeDamage(proj.Damage);
                    }
                }

                // Check projectile vs breakables
                foreach (var b in _rooms.GetActiveBreakables())
                {
                    if (!b.IsDestroyed && proj.BoundingBox.Intersects(b.BoundingBox))
                    {
                        b.Destroy();
                        if (!proj.IsBoomerang) proj.IsAlive = false;
                    }
                }

                if (!proj.IsAlive)
                    _projectiles.RemoveAt(i);
            }

            // ── 9. Dungeon entrance (room 2,0 right side) ──
            // Can add specific dungeon doors per room here

            _prevKb = kb;
        }

        // ──────────────────────────────────────────────
        //  DRAW
        // ──────────────────────────────────────────────

        public override void Draw(SpriteBatch spriteBatch)
        {
            // Note: SpriteBatch.Begin is called in Game1 — we need to end it
            // and restart with the camera transform for world-space drawing.
            spriteBatch.End();

            // ── World-space drawing (affected by camera) ──
            spriteBatch.Begin(
                SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.PointClamp, null, null, null,
                _camera.TransformMatrix);

            DrawWorld(spriteBatch);

            spriteBatch.End();

            // ── Screen-space drawing (HUD — not affected by camera) ──
            spriteBatch.Begin(
                SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.PointClamp, null, null, null, null);

            DrawHUD(spriteBatch);
        }

        private void DrawWorld(SpriteBatch sb)
        {
            Texture2D px = Game1.PixelTexture;
            int w = Game1.ScreenWidth;
            int h = Game1.ScreenHeight;

            // ── Draw floor tiles for all visible rooms ──
            // (Draw current room + neighbors for smooth transitions)
            for (int ry = _camera.RoomY - 1; ry <= _camera.RoomY + 1; ry++)
            {
                for (int rx = _camera.RoomX - 1; rx <= _camera.RoomX + 1; rx++)
                {
                    if (rx < 0 || rx >= WorldRoomsX || ry < 0 || ry >= WorldRoomsY) continue;

                    int ox = rx * w;
                    int oy = ry * h;

                    // Choose floor tile per room
                    Texture2D floor = (ry == 1) ? (_dungeonFloorTile ?? _floorTile) : _floorTile;

                    if (floor != null)
                    {
                        for (int x = ox; x < ox + w; x += floor.Width)
                            for (int y = oy; y < oy + h; y += floor.Height)
                                sb.Draw(floor, new Vector2(x, y), Color.White);
                    }
                    else
                    {
                        Color bg = (ry == 1) ? new Color(30, 25, 35) : new Color(34, 80, 34);
                        sb.Draw(px, new Rectangle(ox, oy, w, h), bg);
                    }

                    // ── Draw walls for this room ──
                    string key = $"{rx},{ry}";
                    if (_roomWalls.ContainsKey(key))
                    {
                        Color wallColor = (ry == 1) ? new Color(50, 35, 45) : new Color(60, 40, 30);
                        foreach (var wall in _roomWalls[key])
                            sb.Draw(px, wall.Bounds, wallColor);
                    }
                }
            }

            // ── Draw triggers ──
            _rooms.DrawTriggers(sb);

            // ── Draw breakables ──
            _rooms.DrawBreakables(sb);

            // ── Draw enemies ──
            _rooms.DrawEnemies(sb);

            // ── Draw projectiles ──
            foreach (var proj in _projectiles)
                proj.Draw(sb);

            // ── Draw player ──
            _player.Draw(sb);

#if DEBUG
            // ── Debug hitboxes ──
            _player.DrawHitbox(sb, Color.Lime);
            foreach (var e in _rooms.GetActiveEnemies())
                e.DrawHitbox(sb, Color.Red);

            if (_player.CurrentStateName == "attack" && _player.AttackHitbox != Rectangle.Empty)
                sb.Draw(px, _player.AttackHitbox, Color.Yellow * 0.4f);
#endif
        }

        // ──────────────────────────────────────────────
        //  HUD (screen-space)
        // ──────────────────────────────────────────────

        private void DrawHUD(SpriteBatch sb)
        {
            Texture2D px = Game1.PixelTexture;

            // ── Hearts ──
            int heartsTotal = _player.MaxHealth / 2;
            int hpLeft = _player.CurrentHealth;
            int heartSize = 28;
            int padding = 4;
            int startX = 20;
            int startY = 20;

            for (int i = 0; i < heartsTotal; i++)
            {
                Texture2D heartTex;
                if (hpLeft >= 2)      { heartTex = _heartFull;  hpLeft -= 2; }
                else if (hpLeft == 1) { heartTex = _heartHalf;  hpLeft = 0; }
                else                  { heartTex = _heartEmpty; }

                Vector2 pos = new Vector2(startX + i * (heartSize + padding), startY);

                if (heartTex != null)
                    sb.Draw(heartTex, new Rectangle((int)pos.X, (int)pos.Y, heartSize, heartSize), Color.White);
                else
                    sb.Draw(px, new Rectangle((int)pos.X, (int)pos.Y, heartSize, heartSize),
                        hpLeft > 0 ? Color.Red : Color.DarkRed);
            }

            // ── Item slot ──
            _player.Inventory.DrawHUD(sb, px);

            // ── Room indicator (small) ──
            int indicatorX = Game1.ScreenWidth / 2 - WorldRoomsX * 8;
            int indicatorY = 8;
            for (int ry = 0; ry < WorldRoomsY; ry++)
            {
                for (int rx = 0; rx < WorldRoomsX; rx++)
                {
                    Color c = (rx == _camera.RoomX && ry == _camera.RoomY)
                        ? Color.White : Color.Gray * 0.4f;
                    sb.Draw(px, new Rectangle(
                        indicatorX + rx * 16, indicatorY + ry * 12, 14, 10), c);
                }
            }
        }
    }
}
