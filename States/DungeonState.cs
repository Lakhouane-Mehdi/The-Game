// ============================================================================
// DungeonState.cs — Multi-Room Dungeon with Boss Fight
// Author: Mehdi Lakhouane
// Description: A 5-room dungeon accessed from the overworld. Uses SharedPlayer.
//   Room 0: Entrance hall — breakable pots, bamboo enemies
//   Room 1: Pressure plate puzzle — step on plates to open doors
//   Room 2: Spirit gauntlet — ranged spirit enemies + bamboos
//   Room 3: Mini-boss arena — locked door, clear enemies to proceed
//   Room 4: Boss room — The Silence Guardian (multi-phase)
//   Player moves between rooms via door zones at screen edges.
// ============================================================================

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TheGame.Core;
using TheGame.Entities;
using TheGame.Entities.Environment;
using TheGame.Entities.Items;
using TheGame.Systems;
using TheGame.UI;
using TheGame.Utils;

namespace TheGame.States
{
    public class DungeonState : GameStateBase
    {
        // ── Shared player reference ──
        private Player _player;

        // ── Room system ──
        private int _currentRoom;
        private const int RoomCount = 5;

        // ── Per-room data ──
        private readonly List<Enemy>[] _roomEnemies = new List<Enemy>[RoomCount];
        private readonly List<SolidRect>[] _roomWalls = new List<SolidRect>[RoomCount];
        private readonly List<Breakable>[] _roomBreakables = new List<Breakable>[RoomCount];
        private readonly List<PressurePlate>[] _roomPlates = new List<PressurePlate>[RoomCount];
        private readonly List<LockedDoor>[] _roomDoors = new List<LockedDoor>[RoomCount];

        // ── Projectiles (shared across rooms) ──
        private readonly List<Projectile> _projectiles = new();
        private readonly List<EnemyProjectile> _enemyProjectiles = new();

        // ── Boss ──
        private DungeonBoss _boss;
        private bool _bossDefeated;
        private float _victoryTimer;
        private bool _victoryShown;

        // ── Dialogue ──
        private DialogueBox _dialogueBox;
        private bool _dialogueActive;

        // ── Textures ──
        private Texture2D _floorTile;
        private Texture2D _potSprite;

        // ── Room transition ──
        private float _transitionTimer;
        private bool _transitioning;
        private int _transitionTarget;
        private Vector2 _transitionSpawn;

        // ── Room cleared tracking ──
        private readonly bool[] _roomCleared = new bool[RoomCount];

        // ── Door walls that can be removed ──
        private readonly Dictionary<string, SolidRect> _namedWalls = new();

        // ── Minimap ──
        private int _highestRoomVisited;

        // ── Input ──
        private KeyboardState _prevKb = Keyboard.GetState();

        // ── Screen dimensions (shorthand) ──
        private const int SW = Game1.ScreenWidth;
        private const int SH = Game1.ScreenHeight;
        private const int Wall = 16;

        // ── Entrance flag — set when entering from overworld ──
        private bool _initialized;

        public DungeonState(Game1 game, ContentManager content)
            : base(game, content)
        {
            // Initialize room arrays
            for (int i = 0; i < RoomCount; i++)
            {
                _roomEnemies[i] = new List<Enemy>();
                _roomWalls[i] = new List<SolidRect>();
                _roomBreakables[i] = new List<Breakable>();
                _roomPlates[i] = new List<PressurePlate>();
                _roomDoors[i] = new List<LockedDoor>();
            }
        }

        /// <summary>
        /// Called when entering the dungeon from overworld.
        /// Sets up the shared player and builds all rooms.
        /// </summary>
        public void EnterDungeon(Player player)
        {
            _player = player;
            _player.OnSpawnProjectile = proj => _projectiles.Add(proj);
            _currentRoom = 0;
            _player.Position = new Vector2(SW / 2f - 32, SH - 80);
            _bossDefeated = false;
            _victoryTimer = 0f;
            _victoryShown = false;
            _highestRoomVisited = 0;

            // Load textures
            if (AssetLoader.Exists("Tiles/stone_floor.png"))
                _floorTile = AssetLoader.LoadTexture("Tiles/stone_floor.png");
            if (AssetLoader.Exists("Objects/pot.png"))
                _potSprite = AssetLoader.LoadTexture("Objects/pot.png");

            _dialogueBox = new DialogueBox();

            // Clear and rebuild all rooms
            for (int i = 0; i < RoomCount; i++)
            {
                _roomEnemies[i].Clear();
                _roomWalls[i].Clear();
                _roomBreakables[i].Clear();
                _roomPlates[i].Clear();
                _roomDoors[i].Clear();
                _roomCleared[i] = false;
            }
            _namedWalls.Clear();
            _projectiles.Clear();
            _enemyProjectiles.Clear();

            BuildRoom0_Entrance();
            BuildRoom1_PuzzleHall();
            BuildRoom2_SpiritGauntlet();
            BuildRoom3_Gauntlet();
            BuildRoom4_BossRoom();

            _initialized = true;
        }

        // ====================================================================
        //  ROOM BUILDING
        // ====================================================================

        private List<SolidRect> MakeBorderWalls(bool openTop, bool openBottom)
        {
            var walls = new List<SolidRect>();

            // Left wall
            walls.Add(new SolidRect(0, Wall, Wall, SH - Wall * 2));
            // Right wall
            walls.Add(new SolidRect(SW - Wall, Wall, Wall, SH - Wall * 2));

            // Top wall (with optional doorway)
            if (openTop)
            {
                walls.Add(new SolidRect(Wall, 0, SW / 2 - Wall - 30, Wall));
                walls.Add(new SolidRect(SW / 2 + 30, 0, SW / 2 - Wall - 30, Wall));
            }
            else
            {
                walls.Add(new SolidRect(0, 0, SW, Wall));
            }

            // Bottom wall (with optional doorway)
            if (openBottom)
            {
                walls.Add(new SolidRect(Wall, SH - Wall, SW / 2 - Wall - 30, Wall));
                walls.Add(new SolidRect(SW / 2 + 30, SH - Wall, SW / 2 - Wall - 30, Wall));
            }
            else
            {
                walls.Add(new SolidRect(0, SH - Wall, SW, Wall));
            }

            return walls;
        }

        private void BuildRoom0_Entrance()
        {
            // Open top (to room 1), open bottom (exit to overworld)
            _roomWalls[0] = MakeBorderWalls(openTop: true, openBottom: true);

            // Interior pillars
            _roomWalls[0].Add(new SolidRect(150, 120, 32, 32));
            _roomWalls[0].Add(new SolidRect(618, 120, 32, 32));
            _roomWalls[0].Add(new SolidRect(150, 320, 32, 32));
            _roomWalls[0].Add(new SolidRect(618, 320, 32, 32));

            // Enemies
            AddEnemy(0, new Bamboo(new Vector2(250, 150)));
            AddEnemy(0, new Bamboo(new Vector2(500, 200)));
            AddEnemy(0, new Bamboo(new Vector2(350, 350)));

            // Breakable pots
            if (_potSprite != null)
            {
                _roomBreakables[0].Add(new Breakable(new Vector2(100, 80), _potSprite, DropType.Heart));
                _roomBreakables[0].Add(new Breakable(new Vector2(670, 80), _potSprite, DropType.Ammo));
                _roomBreakables[0].Add(new Breakable(new Vector2(100, 370), _potSprite, DropType.Heart));
                _roomBreakables[0].Add(new Breakable(new Vector2(670, 370), _potSprite, DropType.Ammo));
            }
        }

        private void BuildRoom1_PuzzleHall()
        {
            // Open top (to room 2), open bottom (to room 0)
            _roomWalls[1] = MakeBorderWalls(openTop: true, openBottom: true);

            // Central corridor walls creating a maze
            _roomWalls[1].Add(new SolidRect(200, Wall, 16, 180));
            _roomWalls[1].Add(new SolidRect(584, Wall, 16, 180));
            _roomWalls[1].Add(new SolidRect(200, 280, 16, SH - 280 - Wall));
            _roomWalls[1].Add(new SolidRect(584, 280, 16, SH - 280 - Wall));

            // Inner walls creating puzzle chambers
            _roomWalls[1].Add(new SolidRect(320, 100, 160, 16));
            _roomWalls[1].Add(new SolidRect(320, 340, 160, 16));

            // Door wall (opened by pressure plate) - blocks path forward
            var doorWall = new SolidRect(370, 200, 60, 16);
            _roomWalls[1].Add(doorWall);
            _namedWalls["puzzle_door_1"] = doorWall;

            // Pressure plates
            var plate1 = new PressurePlate(new Vector2(120, 220), TriggerMode.Toggle);
            plate1.OnStateChanged = (active) =>
            {
                if (active && _namedWalls.ContainsKey("puzzle_door_1"))
                {
                    _roomWalls[1].Remove(_namedWalls["puzzle_door_1"]);
                    _namedWalls.Remove("puzzle_door_1");
                }
            };
            _roomPlates[1].Add(plate1);

            var plate2 = new PressurePlate(new Vector2(650, 220), TriggerMode.Toggle);
            _roomPlates[1].Add(plate2);

            // Pots with hints
            if (_potSprite != null)
            {
                _roomBreakables[1].Add(new Breakable(new Vector2(240, 50), _potSprite, DropType.Heart));
                _roomBreakables[1].Add(new Breakable(new Vector2(520, 50), _potSprite, DropType.Ammo));
            }
        }

        private void BuildRoom2_SpiritGauntlet()
        {
            // Open top (to room 3), open bottom (to room 1)
            _roomWalls[2] = MakeBorderWalls(openTop: true, openBottom: true);

            // Pillar obstacles for cover
            _roomWalls[2].Add(new SolidRect(200, 150, 48, 48));
            _roomWalls[2].Add(new SolidRect(552, 150, 48, 48));
            _roomWalls[2].Add(new SolidRect(200, 280, 48, 48));
            _roomWalls[2].Add(new SolidRect(552, 280, 48, 48));
            _roomWalls[2].Add(new SolidRect(376, 200, 48, 48)); // center pillar

            // Spirit enemies (ranged)
            var spirit1 = new Spirit(new Vector2(300, 100));
            spirit1.OnShootProjectile = (pos, dir) =>
                _enemyProjectiles.Add(new EnemyProjectile(pos, dir, 130f, 1));
            AddEnemy(2, spirit1);

            var spirit2 = new Spirit(new Vector2(500, 350));
            spirit2.OnShootProjectile = (pos, dir) =>
                _enemyProjectiles.Add(new EnemyProjectile(pos, dir, 130f, 1));
            AddEnemy(2, spirit2);

            // Bamboo backup
            AddEnemy(2, new Bamboo(new Vector2(400, 300)));

            if (_potSprite != null)
            {
                _roomBreakables[2].Add(new Breakable(new Vector2(100, 50), _potSprite, DropType.Heart));
                _roomBreakables[2].Add(new Breakable(new Vector2(660, 50), _potSprite, DropType.Heart));
                _roomBreakables[2].Add(new Breakable(new Vector2(100, 400), _potSprite, DropType.Ammo));
                _roomBreakables[2].Add(new Breakable(new Vector2(660, 400), _potSprite, DropType.Ammo));
            }
        }

        private void BuildRoom3_Gauntlet()
        {
            // Open top (to room 4 — boss), open bottom (to room 2)
            // Top door is LOCKED until all enemies are killed
            _roomWalls[3] = MakeBorderWalls(openTop: false, openBottom: true);

            // The locked top doorway — a wall that gets removed
            var bossGate = new SolidRect(SW / 2 - 30, 0, 60, Wall);
            // Already closed (full top wall), we'll replace top wall with doorway when cleared

            // Enemies — mixed gauntlet
            AddEnemy(3, new Bamboo(new Vector2(200, 120)));
            AddEnemy(3, new Bamboo(new Vector2(550, 120)));
            AddEnemy(3, new Bamboo(new Vector2(200, 350)));
            AddEnemy(3, new Bamboo(new Vector2(550, 350)));

            var spirit = new Spirit(new Vector2(380, 200));
            spirit.OnShootProjectile = (pos, dir) =>
                _enemyProjectiles.Add(new EnemyProjectile(pos, dir, 130f, 1));
            AddEnemy(3, spirit);

            if (_potSprite != null)
            {
                _roomBreakables[3].Add(new Breakable(new Vector2(50, 50), _potSprite, DropType.Heart));
                _roomBreakables[3].Add(new Breakable(new Vector2(710, 50), _potSprite, DropType.Heart));
                _roomBreakables[3].Add(new Breakable(new Vector2(380, 400), _potSprite, DropType.Heart));
            }
        }

        private void BuildRoom4_BossRoom()
        {
            // No exit top, open bottom (to room 3)
            _roomWalls[4] = MakeBorderWalls(openTop: false, openBottom: true);

            // Arena pillars (cover from projectiles)
            _roomWalls[4].Add(new SolidRect(180, 140, 32, 32));
            _roomWalls[4].Add(new SolidRect(588, 140, 32, 32));
            _roomWalls[4].Add(new SolidRect(180, 320, 32, 32));
            _roomWalls[4].Add(new SolidRect(588, 320, 32, 32));

            // Boss
            _boss = new DungeonBoss(new Vector2(SW / 2f - 40, 80));
            _boss.LoadContent();
            _boss.OnShootProjectile = (pos, dir) =>
                _enemyProjectiles.Add(new EnemyProjectile(pos, dir, 140f, 1));
            _roomEnemies[4].Add(_boss);

            // Healing pots at corners
            if (_potSprite != null)
            {
                _roomBreakables[4].Add(new Breakable(new Vector2(40, 40), _potSprite, DropType.Heart));
                _roomBreakables[4].Add(new Breakable(new Vector2(720, 40), _potSprite, DropType.Heart));
                _roomBreakables[4].Add(new Breakable(new Vector2(40, 410), _potSprite, DropType.Heart));
                _roomBreakables[4].Add(new Breakable(new Vector2(720, 410), _potSprite, DropType.Heart));
            }
        }

        private void AddEnemy(int room, Enemy enemy)
        {
            enemy.LoadContent();
            _roomEnemies[room].Add(enemy);
        }

        // ====================================================================
        //  UPDATE
        // ====================================================================

        public override void Update(GameTime gameTime)
        {
            if (!_initialized) return;

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            KeyboardState kb = Keyboard.GetState();

            // ── Dialogue ──
            if (_dialogueBox != null && _dialogueBox.IsActive)
            {
                _dialogueBox.Update(gameTime);
                if (_dialogueBox.JustClosed)
                    _dialogueActive = false;
                _prevKb = kb;
                return;
            }

            // ── Room transition fade ──
            if (_transitioning)
            {
                _transitionTimer += dt;
                if (_transitionTimer >= 0.3f)
                {
                    _currentRoom = _transitionTarget;
                    _player.Position = _transitionSpawn;
                    _transitioning = false;
                    _projectiles.Clear();
                    _enemyProjectiles.Clear();
                    if (_currentRoom > _highestRoomVisited)
                        _highestRoomVisited = _currentRoom;
                }
                _prevKb = kb;
                return;
            }

            // ── Victory sequence ──
            if (_bossDefeated)
            {
                _victoryTimer += dt;
                if (_victoryTimer > 3f && !_victoryShown)
                {
                    _victoryShown = true;
                    WorldStateManager.SetFlag("boss_defeated", true);
                    GameRef.QuestFlags.Add("boss_defeated");

                    _dialogueBox.Open(new List<DialogueBox.DialoguePage>
                    {
                        new() { Speaker = "???", Text = "THE SILENCE SHATTERS... THE RESONANCE RETURNS!" },
                        new() { Text = "YOU HAVE RESTORED THE GREAT TUNING FORK." },
                        new() { Text = "THE WORLD BEGINS TO HUM ONCE MORE..." },
                        new() { Text = "CONGRATULATIONS, TUNER. YOUR QUEST IS COMPLETE." }
                    });
                    _dialogueActive = true;
                }
                if (_victoryShown && !_dialogueActive)
                {
                    // Return to overworld
                    GameRef.ChangeState(GameState.Overworld);
                }
                _prevKb = kb;
                return;
            }

            // ── Inventory ──
            if (kb.IsKeyDown(Keys.I) && _prevKb.IsKeyUp(Keys.I))
            {
                GameRef.ChangeState(GameState.Inventory);
                _prevKb = kb;
                return;
            }

            // ── Player update ──
            _player.Update(gameTime);
            var walls = _roomWalls[_currentRoom];
            CollisionSystem.MoveAndResolve(_player, walls, dt);
            ClampToRoom();

            // ── Enemies ──
            var enemies = _roomEnemies[_currentRoom];
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                enemies[i].TargetPosition = _player.Position;
                enemies[i].Update(gameTime);
                CollisionSystem.MoveAndResolve(enemies[i], walls, dt);

                // Spirit shooting
                if (enemies[i] is Spirit spirit && spirit.WantsToShoot)
                {
                    spirit.WantsToShoot = false;
                    Vector2 spawnPos = spirit.Position + spirit.HitboxOffset +
                        new Vector2(spirit.HitboxWidth / 2f, spirit.HitboxHeight / 2f);
                    spirit.OnShootProjectile?.Invoke(spawnPos, spirit.ShootDirection);
                }

                if (enemies[i].CanRemove)
                {
                    // Award coins
                    _player.Inventory.Coins += 3;
                    enemies.RemoveAt(i);
                }
            }

            CollisionSystem.CheckAttackHits(_player, enemies);
            CollisionSystem.CheckEnemyContact(_player, enemies);

            // Player death check
            if (!_player.IsAlive)
            {
                GameRef.TriggerGameOver();
                return;
            }

            // ── Check if room 3 is cleared → open boss gate ──
            if (_currentRoom == 3 && !_roomCleared[3] && enemies.Count == 0)
            {
                _roomCleared[3] = true;
                // Replace top wall with doorway
                _roomWalls[3].Clear();
                _roomWalls[3] = MakeBorderWalls(openTop: true, openBottom: true);

                _dialogueBox.Open(new List<DialogueBox.DialoguePage>
                {
                    new() { Text = "THE WAY FORWARD IS OPEN. THE GUARDIAN AWAITS." }
                });
                _dialogueActive = true;
            }

            // ── Boss defeat check ──
            if (_currentRoom == 4 && _boss != null && _boss.IsDefeated && !_bossDefeated)
            {
                _bossDefeated = true;
                _victoryTimer = 0f;
                _enemyProjectiles.Clear();
            }

            // ── Breakables ──
            var breakables = _roomBreakables[_currentRoom];
            if (_player.CurrentStateName == "attack")
            {
                foreach (var b in breakables)
                    if (!b.IsDestroyed && _player.AttackHitbox.Intersects(b.BoundingBox))
                        b.Destroy();
            }

            for (int i = breakables.Count - 1; i >= 0; i--)
            {
                breakables[i].Update(gameTime);
                DropType drop = breakables[i].TryCollectDrop(_player.BoundingBox);
                if (drop == DropType.Heart) _player.Heal(2);
                if (drop == DropType.Ammo) _player.Inventory.AddItem("bow", 3);
                if (breakables[i].IsDestroyed && breakables[i].CanRemove)
                    breakables.RemoveAt(i);
            }

            // ── Pressure plates ──
            foreach (var plate in _roomPlates[_currentRoom])
                plate.Update(gameTime, _player.BoundingBox);

            // ── Player projectiles ──
            for (int i = _projectiles.Count - 1; i >= 0; i--)
            {
                _projectiles[i].Update(gameTime, walls);
                foreach (var enemy in enemies)
                    if (_projectiles[i].TryHit(enemy))
                        enemy.TakeDamage(_projectiles[i].Damage);
                if (!_projectiles[i].IsAlive)
                    _projectiles.RemoveAt(i);
            }

            // ── Enemy projectiles ──
            for (int i = _enemyProjectiles.Count - 1; i >= 0; i--)
            {
                _enemyProjectiles[i].Update(gameTime, walls);
                _enemyProjectiles[i].TryHitPlayer(_player);
                if (!_enemyProjectiles[i].IsAlive)
                    _enemyProjectiles.RemoveAt(i);
            }

            // ── Room transitions ──
            CheckRoomTransitions();

            _prevKb = kb;
        }

        private void CheckRoomTransitions()
        {
            Rectangle pb = _player.BoundingBox;

            // Go north (top of screen → next room)
            if (pb.Top <= 4 && _currentRoom < RoomCount - 1)
            {
                // Room 3 → 4 requires room cleared
                if (_currentRoom == 3 && !_roomCleared[3]) return;

                StartTransition(_currentRoom + 1, new Vector2(SW / 2f - 32, SH - 80));
            }

            // Go south (bottom of screen → previous room)
            if (pb.Bottom >= SH - 4)
            {
                if (_currentRoom == 0)
                {
                    // Exit dungeon back to overworld
                    GameRef.ChangeState(GameState.Overworld);
                    return;
                }
                StartTransition(_currentRoom - 1, new Vector2(SW / 2f - 32, 30));
            }
        }

        private void StartTransition(int targetRoom, Vector2 spawnPos)
        {
            _transitioning = true;
            _transitionTimer = 0f;
            _transitionTarget = targetRoom;
            _transitionSpawn = spawnPos;
        }

        private void ClampToRoom()
        {
            // Don't clamp at doorways
            float minX = Wall;
            float maxX = SW - Wall - _player.HitboxWidth;
            // Y is not clamped — player can walk through doorways at top/bottom

            Vector2 pos = _player.Position;
            float hx = _player.HitboxOffset.X;
            float hy = _player.HitboxOffset.Y;

            if (pos.X + hx < minX) pos.X = minX - hx;
            if (pos.X + hx > maxX) pos.X = maxX - hx;
            // Don't clamp Y at doors
            _player.Position = pos;
        }

        // ====================================================================
        //  DRAW
        // ====================================================================

        public override void Draw(SpriteBatch sb)
        {
            if (!_initialized) return;

            Texture2D px = Game1.PixelTexture;

            // ── Floor ──
            DrawFloor(sb, px);

            // ── Walls ──
            DrawWalls(sb, px);

            // ── Door indicators ──
            DrawDoors(sb, px);

            // ── Pressure plates ──
            foreach (var plate in _roomPlates[_currentRoom])
                plate.Draw(sb);

            // ── Breakables ──
            foreach (var b in _roomBreakables[_currentRoom])
                b.Draw(sb);

            // ── Enemies ──
            foreach (var enemy in _roomEnemies[_currentRoom])
                enemy.Draw(sb);

            // ── Projectiles ──
            foreach (var proj in _projectiles)
                proj.Draw(sb);
            foreach (var proj in _enemyProjectiles)
                proj.Draw(sb);

            // ── Player ──
            _player.Draw(sb);

            // ── Ambient overlay ──
            float darkness = _currentRoom switch
            {
                0 => 0.1f,
                1 => 0.15f,
                2 => 0.2f,
                3 => 0.25f,
                4 => 0.12f, // boss room is lit dramatically
                _ => 0.1f
            };
            sb.Draw(px, new Rectangle(0, 0, SW, SH), Color.Black * darkness);

            // ── HUD ──
            DrawHUD(sb, px);

            // ── Room name ──
            DrawRoomName(sb);

            // ── Dialogue ──
            if (_dialogueActive)
                _dialogueBox.Draw(sb);

            // ── Transition fade ──
            if (_transitioning)
            {
                float t = _transitionTimer / 0.3f;
                float alpha = t < 0.5f ? t * 2f : (1f - t) * 2f;
                sb.Draw(px, new Rectangle(0, 0, SW, SH), Color.Black * alpha);
            }

            // ── Victory flash ──
            if (_bossDefeated && _victoryTimer < 2f)
            {
                float flash = MathF.Max(0f, 1f - _victoryTimer / 2f);
                sb.Draw(px, new Rectangle(0, 0, SW, SH), Color.White * flash);
            }

            // ── Minimap ──
            DrawMinimap(sb, px);
        }

        private void DrawFloor(SpriteBatch sb, Texture2D px)
        {
            Color floorColor = _currentRoom switch
            {
                0 => new Color(35, 30, 40),
                1 => new Color(30, 35, 45),
                2 => new Color(25, 25, 40),
                3 => new Color(40, 25, 30),
                4 => new Color(20, 15, 25),
                _ => new Color(30, 25, 35)
            };

            if (_floorTile != null)
            {
                for (int x = 0; x < SW; x += _floorTile.Width)
                    for (int y = 0; y < SH; y += _floorTile.Height)
                        sb.Draw(_floorTile, new Vector2(x, y), floorColor);
            }
            else
            {
                sb.Draw(px, new Rectangle(0, 0, SW, SH), floorColor);
            }

            // Tile pattern overlay
            Color lineColor = Color.White * 0.03f;
            for (int x = 0; x < SW; x += 32)
                sb.Draw(px, new Rectangle(x, 0, 1, SH), lineColor);
            for (int y = 0; y < SH; y += 32)
                sb.Draw(px, new Rectangle(0, y, SW, 1), lineColor);
        }

        private void DrawWalls(SpriteBatch sb, Texture2D px)
        {
            Color wallColor = _currentRoom switch
            {
                4 => new Color(60, 30, 50), // boss room - darker
                _ => new Color(50, 40, 55)
            };
            Color wallHighlight = wallColor * 1.3f;

            foreach (var wall in _roomWalls[_currentRoom])
            {
                Rectangle r = wall.Bounds;
                sb.Draw(px, r, wallColor);
                // Top highlight
                sb.Draw(px, new Rectangle(r.X, r.Y, r.Width, 2), wallHighlight);
                // Left highlight
                sb.Draw(px, new Rectangle(r.X, r.Y, 2, r.Height), wallHighlight);
            }

            // Pillar decorations (draw circle on square pillars)
            foreach (var wall in _roomWalls[_currentRoom])
            {
                Rectangle r = wall.Bounds;
                if (r.Width >= 32 && r.Width <= 48 && r.Height >= 32 && r.Height <= 48)
                {
                    // It's a pillar — draw ring
                    int cx = r.X + r.Width / 2;
                    int cy = r.Y + r.Height / 2;
                    int radius = r.Width / 2 - 4;
                    sb.Draw(px, new Rectangle(cx - radius, cy - radius, radius * 2, radius * 2),
                        Color.Gray * 0.3f);
                    sb.Draw(px, new Rectangle(cx - 2, cy - 2, 4, 4), Color.Cyan * 0.4f);
                }
            }
        }

        private void DrawDoors(SpriteBatch sb, Texture2D px)
        {
            // North door indicator
            if (_currentRoom < RoomCount - 1)
            {
                bool canPass = _currentRoom != 3 || _roomCleared[3];
                Color doorColor = canPass ? Color.Cyan * 0.4f : Color.Red * 0.3f;
                sb.Draw(px, new Rectangle(SW / 2 - 28, 0, 56, Wall), doorColor);
                // Arrow
                if (canPass)
                {
                    sb.Draw(px, new Rectangle(SW / 2 - 2, 2, 4, 8), Color.Cyan * 0.7f);
                    sb.Draw(px, new Rectangle(SW / 2 - 6, 6, 12, 2), Color.Cyan * 0.7f);
                }
            }

            // South door indicator
            {
                Color doorColor = Color.Cyan * 0.4f;
                sb.Draw(px, new Rectangle(SW / 2 - 28, SH - Wall, 56, Wall), doorColor);
                sb.Draw(px, new Rectangle(SW / 2 - 2, SH - 10, 4, 8), Color.Cyan * 0.7f);
                sb.Draw(px, new Rectangle(SW / 2 - 6, SH - 8, 12, 2), Color.Cyan * 0.7f);
            }
        }

        private void DrawHUD(SpriteBatch sb, Texture2D px)
        {
            // Hearts
            int heartSize = 16;
            int heartsPerRow = _player.MaxHealth / 2;
            for (int i = 0; i < _player.MaxHealth / 2; i++)
            {
                int hx = 10 + i * (heartSize + 4);
                int hy = 10;
                int healthPoints = _player.CurrentHealth;
                int heartIndex = i * 2;

                Color c;
                if (healthPoints >= heartIndex + 2)
                    c = Color.Red;
                else if (healthPoints >= heartIndex + 1)
                    c = Color.DarkRed;
                else
                    c = Color.Gray * 0.5f;

                // Heart shape
                sb.Draw(px, new Rectangle(hx, hy, heartSize / 2, heartSize / 2), c);
                sb.Draw(px, new Rectangle(hx + heartSize / 2, hy, heartSize / 2, heartSize / 2), c);
                sb.Draw(px, new Rectangle(hx - 1, hy + heartSize / 4, heartSize + 2, heartSize / 2), c);
                sb.Draw(px, new Rectangle(hx + 2, hy + heartSize * 3 / 4 - 2, heartSize - 4, heartSize / 4), c);
            }

            // Coins
            PixelFont.DrawString(sb, $"{_player.Inventory.Coins} COINS", 10, 32, Color.Gold, 1);
        }

        private void DrawRoomName(SpriteBatch sb)
        {
            string name = _currentRoom switch
            {
                0 => "ENTRANCE HALL",
                1 => "PUZZLE CORRIDOR",
                2 => "SPIRIT GAUNTLET",
                3 => "THE GATE OF SILENCE",
                4 => "CHAMBER OF THE GUARDIAN",
                _ => "DUNGEON"
            };

            int textWidth = name.Length * 6;
            PixelFont.DrawString(sb, name, SW / 2 - textWidth / 2, SH - 30, Color.Gray * 0.6f, 1);
        }

        private void DrawMinimap(SpriteBatch sb, Texture2D px)
        {
            int mapX = SW - 60;
            int mapY = 10;
            int roomW = 12;
            int roomH = 10;
            int gap = 2;

            // Background
            sb.Draw(px, new Rectangle(mapX - 4, mapY - 4, roomW + 8, (roomH + gap) * RoomCount + 6),
                Color.Black * 0.6f);

            for (int i = 0; i < RoomCount; i++)
            {
                int ry = mapY + (RoomCount - 1 - i) * (roomH + gap);

                Color c;
                if (i == _currentRoom) c = Color.White;
                else if (i <= _highestRoomVisited) c = Color.Gray;
                else c = Color.DarkGray * 0.3f;

                if (i == 4 && !_bossDefeated) c = i == _currentRoom ? Color.Red : Color.DarkRed * 0.5f;

                sb.Draw(px, new Rectangle(mapX, ry, roomW, roomH), c);
            }
        }
    }
}
