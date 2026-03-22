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

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
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
using TheGame.UI;
using TheGame.Utils;

namespace TheGame.States
{
    public class OverworldState : GameStateBase
    {
        // ── Camera ──
        private Camera _camera;

        // ── Entities ──
        private Player _player; // alias for GameRef.SharedPlayer
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
        private Texture2D _wallTile;

        // ── TileMap System ──
        private Texture2D _gameTileset;
        private Texture2D _villageTileset;
        private readonly Dictionary<string, TileMap> _roomTileMaps = new();
        private const int TileSize = 16;
        private const int TileScale = 2; // 16px tiles drawn at 32px

        // ── UI / HUD ──
        private Texture2D _heartFull;
        private Texture2D _heartHalf;
        private Texture2D _heartEmpty;

        // ── Object sprites ──
        private Texture2D _potSprite;
        private Texture2D _crateSprite;
        private Texture2D _grassSprite;

        // ── Decoration sprites (Sprout Lands) ──
        private Texture2D _treesSheet;   // Trees, stumps and bushes
        private Texture2D _rocksSheet;   // Mushrooms, Flowers, Stones
        private Texture2D _roofSheet;    // wooden_house_roof_tilset
        private Texture2D _wallSheet;    // wooden_house_walls_tilset
        private Texture2D _fenceSheet;   // fences

        // ── Room decorations (pre-generated, keyed by "rx,ry") ──
        private struct Decoration
        {
            public Rectangle Source;     // source rect in spritesheet
            public Vector2 Position;     // world position
            public Texture2D Sheet;      // which spritesheet
            public float Scale;
        }
        private readonly Dictionary<string, List<Decoration>> _roomDecorations = new();

        // ── Collectible pickups ──
        private struct Collectible
        {
            public Vector2 Position;
            public string ItemId;       // matches items.json id
            public Rectangle Source;    // sprite source rect
            public Texture2D Sheet;     // spritesheet
            public bool Collected;
            public int RoomX, RoomY;
        }
        private readonly List<Collectible> _collectibles = new();
        private string _pickupMessage;
        private float _pickupMessageTimer;

        // ── Audio ──
        private SoundEffect _swordSfx;
        private SoundEffect _hitSfx;
        private SoundEffect _deathSfx;

        // ── Input edge detection ──
        private KeyboardState _prevKb = Keyboard.GetState();

        // ── Debug options ──
        private bool _showDebug = false;

        // ── Damage flash ──
        private float _damageFlashTimer;
        private const float DamageFlashDuration = 0.25f;

        // ── Dialogue System ──
        private readonly DialogueBox _dialogueBox = new();
        private readonly List<RoomEntity<Interactable>> _interactables = new();
        private Interactable _activeInteractable;

        // ── Pulse Effects ──
        private readonly List<PulseEffect> _pulseEffects = new();

        // ── Warp Tiles (doors to interiors) ──
        private readonly List<WarpTile> _warpTiles = new();
        private float _warpCooldown;

        // ── Buildings (drawn as decorations in the overworld) ──
        private struct Building
        {
            public int X, Y, Width, Height; // world-space position and size
            public int DoorX, DoorY, DoorW, DoorH; // door opening position
            public string Label; // name shown above building
        }
        private readonly List<Building> _buildings = new();

        // ── Low Health Flash ──
        private float _lowHealthTimer;

        // ── Room floor colors (fallback for rooms without tile textures) ──
        private static Color GetRoomColor(int rx, int ry)
        {
            // 3 biome bands: rows 0-1 = Peaks, rows 2-3 = Woods, row 4 = Outskirts
            if (ry <= 1) return new Color(100 + rx * 3, 95 + rx * 2, 80 + rx * 2);     // grey/rocky
            if (ry <= 3) return new Color(70 + rx * 4, 130 + rx * 3, 50 + rx * 2);     // green/forest
            return new Color(55 + rx * 3, 65 + rx * 2, 45 + rx * 2);                    // sandy/dark
        }

        // ── World size: 7x5 rooms ──
        private const int WorldRoomsX = 7;
        private const int WorldRoomsY = 5;

        public OverworldState(Game1 game, ContentManager content)
            : base(game, content)
        {
            // ── Camera ──
            _camera = new Camera(Game1.ScreenWidth, Game1.ScreenHeight, WorldRoomsX, WorldRoomsY);
            _camera.SnapToRoom(3, 2); // start in the center room

            // ── Room Manager ──
            _rooms = new RoomManager(_camera);

            // ── Player (shared instance owned by Game1) ──
            _player = GameRef.SharedPlayer;

            // Wire projectile spawning
            _player.OnSpawnProjectile = (proj) => _projectiles.Add(proj);

            // ── Load assets ──
            LoadAssets();

            // ── Build world ──
            BuildRoomTileMaps();
            BuildRoomWalls();
            GenerateDecorations();
            SpawnEnemies();
            SpawnBreakables();
            SpawnTriggers();
            SpawnInteractables();
            SpawnWarpTiles();

            // ── Activate initial room ──
            _rooms.RefreshActiveRoom();
            RefreshActiveInteractables();
        }

        /// <summary>
        /// Called when returning from an interior. Restores camera and room state.
        /// </summary>
        public void ReturnFromInterior(Player player, int roomX, int roomY)
        {
            _player = player;
            _player.OnSpawnProjectile = (proj) => _projectiles.Add(proj);
            _camera.SnapToRoom(roomX, roomY);
            _rooms.RefreshActiveRoom();
            RefreshActiveInteractables();
            _warpCooldown = 0.5f; // prevent instant re-entry
        }

        // ──────────────────────────────────────────────
        //  Asset Loading
        // ──────────────────────────────────────────────

        private void LoadAssets()
        {
            // Use extracted single-tile images (64x64) as fallbacks
            if (AssetLoader.Exists("Tiles/grass_floor.png"))
                _floorTile = AssetLoader.LoadTexture("Tiles/grass_floor.png");
            if (AssetLoader.Exists("Tiles/stone_floor.png"))
                _dungeonFloorTile = AssetLoader.LoadTexture("Tiles/stone_floor.png");
            if (AssetLoader.Exists("Tiles/wall_tile.png"))
                _wallTile = AssetLoader.LoadTexture("Tiles/wall_tile.png");

            // Load the combined game tileset (extracted from Ninja Adventure assets)
            if (AssetLoader.Exists("Tiles/game_tileset.png"))
                _gameTileset = AssetLoader.LoadTexture("Tiles/game_tileset.png");

            // Village tileset for buildings (houses, fences, trees)
            if (AssetLoader.Exists("Tiles/tileset_village_abandoned.png"))
                _villageTileset = AssetLoader.LoadTexture("Tiles/tileset_village_abandoned.png");

            // Use extracted individual heart sprites (16x16 each)
            if (AssetLoader.Exists("UI/heart_full.png"))
                _heartFull = AssetLoader.LoadTexture("UI/heart_full.png");
            if (AssetLoader.Exists("UI/heart_half.png"))
                _heartHalf = AssetLoader.LoadTexture("UI/heart_half.png");
            if (AssetLoader.Exists("UI/heart_empty.png"))
                _heartEmpty = AssetLoader.LoadTexture("UI/heart_empty.png");

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
            if (AssetLoader.Exists("Audio/SFX/death.wav"))
                _deathSfx = AssetLoader.LoadSound("Audio/SFX/death.wav");

            // ── Sprout Lands decoration sprites ──
            if (AssetLoader.Exists("Objects/Nature/Trees, stumps and bushes.png"))
                _treesSheet = AssetLoader.LoadTexture("Objects/Nature/Trees, stumps and bushes.png");
            if (AssetLoader.Exists("Objects/Nature/Mushrooms, Flowers, Stones.png"))
                _rocksSheet = AssetLoader.LoadTexture("Objects/Nature/Mushrooms, Flowers, Stones.png");
            if (AssetLoader.Exists("Tiles/Buildings/wooden_house_roof_tilset.png"))
                _roofSheet = AssetLoader.LoadTexture("Tiles/Buildings/wooden_house_roof_tilset.png");
            if (AssetLoader.Exists("Tiles/Buildings/wooden_house_walls_tilset.png"))
                _wallSheet = AssetLoader.LoadTexture("Tiles/Buildings/wooden_house_walls_tilset.png");
            if (AssetLoader.Exists("Tiles/Buildings/fences.png"))
                _fenceSheet = AssetLoader.LoadTexture("Tiles/Buildings/fences.png");
        }

        // ──────────────────────────────────────────────
        //  TileMap Construction — per Room
        // ──────────────────────────────────────────────

        private void BuildRoomTileMaps()
        {
            if (_gameTileset == null) return;
            // Each room is 800x480 pixels. At scale 2, tiles are 32px.
            // That gives us 25 cols x 15 rows per room.
            int cols = Game1.ScreenWidth / (TileSize * TileScale);   // 25
            int rows = Game1.ScreenHeight / (TileSize * TileScale);  // 15
            System.Random rng = new System.Random(42); // deterministic

            for (int ry = 0; ry < WorldRoomsY; ry++)
            {
                for (int rx = 0; rx < WorldRoomsX; rx++)
                {
                    var map = new TileMap(TileSize, TileSize);
                    map.SetTileset(_gameTileset);
                    map.RenderScale = TileScale;

                    int[,] data = new int[rows, cols];

                    // Biome bands: rows 0-1 = Peaks, rows 2-3 = Woods, row 4 = Outskirts
                    int biome = ry <= 1 ? 0 : ry <= 3 ? 1 : 2;

                    for (int r = 0; r < rows; r++)
                    {
                        for (int c = 0; c < cols; c++)
                        {
                            bool isEdge = (r == 0 || r == rows - 1 || c == 0 || c == cols - 1);

                            if (biome == 0)
                            {
                                // Resonant Peaks — stone/grey floor
                                data[r, c] = 112 + rng.Next(8);
                                if (isEdge)
                                    data[r, c] = 96 + rng.Next(8);
                            }
                            else if (biome == 2)
                            {
                                // Silent Outskirts — sand/earth floor
                                data[r, c] = 16 + rng.Next(8);
                                if (isEdge)
                                    data[r, c] = 16 + rng.Next(4);
                            }
                            else
                            {
                                // Echoing Woods — grass floor (use same grass for edges)
                                data[r, c] = rng.Next(8);
                                if (rng.Next(5) == 0)
                                    data[r, c] = 8 + rng.Next(4);
                                if (isEdge)
                                    data[r, c] = rng.Next(4); // seamless grass, no grid borders
                            }

                            // Clear doorway openings (center of each edge)
                            int doorHalf = 2; // 2 tiles each side of center
                            bool inHCenter = (c >= cols / 2 - doorHalf && c <= cols / 2 + doorHalf);
                            bool inVCenter = (r >= rows / 2 - doorHalf && r <= rows / 2 + doorHalf);

                            // Floor tile for doorways based on biome
                            int floorTile = biome == 0 ? 112 + rng.Next(4) :
                                            biome == 2 ? 16 + rng.Next(4) :
                                            rng.Next(4);

                            if (r == 0 && inHCenter && ry > 0)
                                data[r, c] = floorTile;
                            if (r == rows - 1 && inHCenter && ry < WorldRoomsY - 1)
                                data[r, c] = floorTile;
                            if (c == 0 && inVCenter && rx > 0)
                                data[r, c] = floorTile;
                            if (c == cols - 1 && inVCenter && rx < WorldRoomsX - 1)
                                data[r, c] = floorTile;
                        }
                    }

                    map.LoadFromArray(data);
                    _roomTileMaps[$"{rx},{ry}"] = map;
                }
            }
        }

        // ──────────────────────────────────────────────
        //  Decoration Generation — Trees, Rocks, Flowers
        // ──────────────────────────────────────────────

        private void GenerateDecorations()
        {
            // Trees sheet: 192x112 — contains trees (~32x32), stumps, bushes, fruits
            // Rocks sheet: 192x80 — mushrooms, flowers, stones of various sizes
            var rng = new System.Random(123); // deterministic
            int w = Game1.ScreenWidth;
            int h = Game1.ScreenHeight;

            // Pre-define source rects for common decorations
            // Trees (from trees sheet) — large trees are ~32x32 at position (0,0), (32,0), etc.
            var treeSources = new Rectangle[]
            {
                new Rectangle(0, 0, 48, 48),      // round tree 1
                new Rectangle(48, 0, 48, 48),     // round tree 2
                new Rectangle(96, 0, 48, 48),     // pine tree
                new Rectangle(144, 0, 48, 48),    // big tree
            };
            // Small bushes/stumps (~16x16)
            var bushSources = new Rectangle[]
            {
                new Rectangle(0, 64, 16, 16),      // small bush
                new Rectangle(16, 64, 16, 16),     // stump
                new Rectangle(32, 64, 16, 16),     // berry bush
                new Rectangle(64, 64, 16, 16),     // mushroom
            };
            // Rocks (from rocks sheet) — stones are in bottom rows
            var rockSources = new Rectangle[]
            {
                new Rectangle(0, 48, 32, 32),      // large rock
                new Rectangle(32, 48, 32, 32),      // rock cluster
                new Rectangle(64, 48, 16, 16),      // small rock
                new Rectangle(80, 48, 16, 16),      // pebble
            };
            // Flowers (from rocks sheet)
            var flowerSources = new Rectangle[]
            {
                new Rectangle(0, 16, 16, 16),      // flower 1
                new Rectangle(16, 16, 16, 16),     // flower 2
                new Rectangle(32, 16, 16, 16),     // flower 3
                new Rectangle(48, 16, 16, 16),     // flower 4
            };

            for (int ry = 0; ry < WorldRoomsY; ry++)
            {
                for (int rx = 0; rx < WorldRoomsX; rx++)
                {
                    int ox = rx * w;
                    int oy = ry * h;
                    string key = $"{rx},{ry}";
                    var decs = new List<Decoration>();
                    int biome = ry <= 1 ? 0 : ry <= 3 ? 1 : 2;

                    // Number of decorations per room varies by biome
                    int treeCount = biome == 1 ? rng.Next(4, 8) : rng.Next(1, 4);
                    int rockCount = biome == 0 ? rng.Next(3, 7) : rng.Next(1, 3);
                    int flowerCount = biome == 1 ? rng.Next(3, 8) : rng.Next(0, 3);
                    int bushCount = rng.Next(2, 5);

                    // Skip starting room center area for decorations
                    bool isStartRoom = (rx == 3 && ry == 2);

                    // Trees
                    if (_treesSheet != null)
                    {
                        for (int i = 0; i < treeCount; i++)
                        {
                            float px = ox + 40 + rng.Next(w - 80);
                            float py = oy + 40 + rng.Next(h - 80);
                            if (isStartRoom && px > ox + 200 && px < ox + 600 && py > oy + 100 && py < oy + 400)
                                continue;
                            decs.Add(new Decoration
                            {
                                Source = treeSources[rng.Next(treeSources.Length)],
                                Position = new Vector2(px, py),
                                Sheet = _treesSheet,
                                Scale = 2f
                            });
                            // Tree trunk solid hitbox based on 48x48 source * 2f scale
                            AddWall(rx, ry, new SolidRect((int)px + 32, (int)py + 64, 32, 24));
                        }

                        // Bushes
                        for (int i = 0; i < bushCount; i++)
                        {
                            float bx2 = ox + 30 + rng.Next(w - 60);
                            float by2 = oy + 30 + rng.Next(h - 60);
                            // Skip bush if it overlaps the building area in start room
                            if (isStartRoom && bx2 > ox + 60 && bx2 < ox + 300 && by2 > oy + 20 && by2 < oy + 220)
                                continue;
                            decs.Add(new Decoration
                            {
                                Source = bushSources[rng.Next(bushSources.Length)],
                                Position = new Vector2(bx2, by2),
                                Sheet = _treesSheet,
                                Scale = 2f
                            });
                        }
                    }

                    // Rocks
                    if (_rocksSheet != null)
                    {
                        for (int i = 0; i < rockCount; i++)
                        {
                            decs.Add(new Decoration
                            {
                                Source = rockSources[rng.Next(rockSources.Length)],
                                Position = new Vector2(ox + 30 + rng.Next(w - 60), oy + 30 + rng.Next(h - 60)),
                                Sheet = _rocksSheet,
                                Scale = 2f
                            });
                        }

                        // Flowers (mostly in woods)
                        for (int i = 0; i < flowerCount; i++)
                        {
                            decs.Add(new Decoration
                            {
                                Source = flowerSources[rng.Next(flowerSources.Length)],
                                Position = new Vector2(ox + 20 + rng.Next(w - 40), oy + 20 + rng.Next(h - 40)),
                                Sheet = _rocksSheet,
                                Scale = 2f
                            });
                        }
                    }

                    _roomDecorations[key] = decs;
                }
            }

            // ── Hand-placed decorations for starting room (3,2) ──
            int sw = Game1.ScreenWidth;
            int sh = Game1.ScreenHeight;
            int sox = 3 * sw;
            int soy = 2 * sh;
            string startKey = "3,2";
            if (!_roomDecorations.ContainsKey(startKey))
                _roomDecorations[startKey] = new List<Decoration>();
            var startDecs = _roomDecorations[startKey];

            // Dirt path from house door heading south-east (using rock/earth tiles)
            if (_rocksSheet != null)
            {
                // Scatter small rocks along a path from the door area to the south
                for (int i = 0; i < 12; i++)
                {
                    startDecs.Add(new Decoration
                    {
                        Source = new Rectangle(64, 48, 16, 16), // small rock
                        Position = new Vector2(sox + 180 + i * 18 + (i % 2) * 6, soy + 210 + i * 20),
                        Sheet = _rocksSheet,
                        Scale = 1.5f
                    });
                }

                // Extra flowers around the house garden area
                for (int i = 0; i < 8; i++)
                {
                    startDecs.Add(new Decoration
                    {
                        Source = flowerSources[i % flowerSources.Length],
                        Position = new Vector2(sox + 60 + i * 30, soy + 210 + (i % 3) * 12),
                        Sheet = _rocksSheet,
                        Scale = 2f
                    });
                }

                // Flowers scattered in southern empty area
                for (int i = 0; i < 10; i++)
                {
                    startDecs.Add(new Decoration
                    {
                        Source = flowerSources[i % flowerSources.Length],
                        Position = new Vector2(sox + 100 + i * 60, soy + 320 + (i % 4) * 30),
                        Sheet = _rocksSheet,
                        Scale = 2f
                    });
                }

                // Rocks in corners for visual interest
                startDecs.Add(new Decoration
                {
                    Source = new Rectangle(0, 48, 32, 32), Position = new Vector2(sox + 50, soy + 380),
                    Sheet = _rocksSheet, Scale = 2f
                });
                startDecs.Add(new Decoration
                {
                    Source = new Rectangle(32, 48, 32, 32), Position = new Vector2(sox + 680, soy + 400),
                    Sheet = _rocksSheet, Scale = 2f
                });
            }

            // Extra bushes around the house perimeter
            if (_treesSheet != null)
            {
                // Fence-like bushes along the bottom of the house
                for (int i = 0; i < 4; i++)
                {
                    startDecs.Add(new Decoration
                    {
                        Source = new Rectangle(0, 64, 16, 16), // small bush
                        Position = new Vector2(sox + 70 + i * 22, soy + 195),
                        Sheet = _treesSheet,
                        Scale = 2f
                    });
                }
                // Bushes on right side of house
                for (int i = 0; i < 3; i++)
                {
                    startDecs.Add(new Decoration
                    {
                        Source = new Rectangle(32, 64, 16, 16), // berry bush
                        Position = new Vector2(sox + 270, soy + 80 + i * 40),
                        Sheet = _treesSheet,
                        Scale = 2f
                    });
                }
            }

            // ── Spawn collectible pickups in each room ──
            SpawnCollectibles();
        }

        private void SpawnCollectibles()
        {
            var rng = new System.Random(999);
            int w = Game1.ScreenWidth;
            int h = Game1.ScreenHeight;

            // Source rects for collectible sprites (verified via pixel analysis)
            var fruitSrc = new Rectangle(0, 64, 32, 16);     // berry cluster on trees sheet
            var mushroomSrc = new Rectangle(16, 0, 16, 16);  // medium mushroom on rocks sheet
            var flowerSrc = new Rectangle(48, 48, 32, 32);   // sunflower on rocks sheet
            var stoneSrc = new Rectangle(96, 16, 32, 32);    // grey rock on rocks sheet
            var woodSrc = new Rectangle(0, 96, 32, 16);      // stump on trees sheet

            for (int ry = 0; ry < WorldRoomsY; ry++)
            {
                for (int rx = 0; rx < WorldRoomsX; rx++)
                {
                    int ox = rx * w;
                    int oy = ry * h;
                    int biome = ry <= 1 ? 0 : ry <= 3 ? 1 : 2;

                    // Scatter collectibles based on biome
                    int fruitCount = biome == 1 ? rng.Next(1, 4) : rng.Next(0, 2);
                    int stoneCount = biome == 0 ? rng.Next(2, 5) : rng.Next(0, 3);
                    int flowerCount2 = biome == 1 ? rng.Next(1, 3) : 0;
                    int mushroomCount = biome == 1 ? rng.Next(0, 3) : 0;
                    int woodCount = rng.Next(0, 2);

                    for (int i = 0; i < fruitCount; i++)
                    {
                        if (_treesSheet != null)
                            _collectibles.Add(new Collectible
                            {
                                Position = new Vector2(ox + 50 + rng.Next(w - 100), oy + 50 + rng.Next(h - 100)),
                                ItemId = "fruit", Source = fruitSrc, Sheet = _treesSheet,
                                RoomX = rx, RoomY = ry
                            });
                    }
                    for (int i = 0; i < stoneCount; i++)
                    {
                        if (_rocksSheet != null)
                            _collectibles.Add(new Collectible
                            {
                                Position = new Vector2(ox + 50 + rng.Next(w - 100), oy + 50 + rng.Next(h - 100)),
                                ItemId = "stone", Source = stoneSrc, Sheet = _rocksSheet,
                                RoomX = rx, RoomY = ry
                            });
                    }
                    for (int i = 0; i < flowerCount2; i++)
                    {
                        if (_rocksSheet != null)
                            _collectibles.Add(new Collectible
                            {
                                Position = new Vector2(ox + 50 + rng.Next(w - 100), oy + 50 + rng.Next(h - 100)),
                                ItemId = "flower", Source = flowerSrc, Sheet = _rocksSheet,
                                RoomX = rx, RoomY = ry
                            });
                    }
                    for (int i = 0; i < mushroomCount; i++)
                    {
                        if (_rocksSheet != null)
                            _collectibles.Add(new Collectible
                            {
                                Position = new Vector2(ox + 50 + rng.Next(w - 100), oy + 50 + rng.Next(h - 100)),
                                ItemId = "mushroom", Source = mushroomSrc, Sheet = _rocksSheet,
                                RoomX = rx, RoomY = ry
                            });
                    }
                    for (int i = 0; i < woodCount; i++)
                    {
                        if (_treesSheet != null)
                            _collectibles.Add(new Collectible
                            {
                                Position = new Vector2(ox + 50 + rng.Next(w - 100), oy + 50 + rng.Next(h - 100)),
                                ItemId = "wood", Source = woodSrc, Sheet = _treesSheet,
                                RoomX = rx, RoomY = ry
                            });
                    }
                }
            }
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

                    // Horizontal walls run between vertical wall edges (inset by t)
                    // to avoid overlapping corners which cause push-back jitter.
                    int hx0 = ox + t;       // horizontal walls start after left wall
                    int hw = w - t * 2;     // horizontal wall width (between left/right walls)

                    // ── Top wall (opening in center if room above exists) ──
                    if (ry > 0)
                    {
                        walls.Add(new SolidRect(hx0, oy, hw / 2 - 30 + t, t));
                        walls.Add(new SolidRect(ox + w / 2 + 30, oy, hw / 2 - 30 + t, t));
                    }
                    else
                        walls.Add(new SolidRect(hx0, oy, hw, t));

                    // ── Bottom wall ──
                    if (ry < WorldRoomsY - 1)
                    {
                        walls.Add(new SolidRect(hx0, oy + h - t, hw / 2 - 30 + t, t));
                        walls.Add(new SolidRect(ox + w / 2 + 30, oy + h - t, hw / 2 - 30 + t, t));
                    }
                    else
                        walls.Add(new SolidRect(hx0, oy + h - t, hw, t));

                    // ── Left wall (full height — vertical walls own the corners) ──
                    if (rx > 0)
                    {
                        walls.Add(new SolidRect(ox, oy, t, h / 2 - 30));
                        walls.Add(new SolidRect(ox, oy + h / 2 + 30, t, h / 2 - 30));
                    }
                    else
                        walls.Add(new SolidRect(ox, oy, t, h));

                    // ── Right wall (full height) ──
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
            // Layout: 7x5 grid. Starting room is (3,2).

            // Room (3,2) — starting meadow: a few rocks
            AddWall(3, 2, new SolidRect(3 * w + 350, 2 * h + 200, 48, 48));
            AddWall(3, 2, new SolidRect(3 * w + 150, 2 * h + 350, 48, 48));

            // Room (2,2) — raccoon den: arena walls
            AddWall(2, 2, new SolidRect(2 * w + 200, 2 * h + 16, 32, 200));
            AddWall(2, 2, new SolidRect(2 * w + 200, 2 * h + 280, 32, 184));
            AddWall(2, 2, new SolidRect(2 * w + 550, 2 * h + 16, 32, 200));
            AddWall(2, 2, new SolidRect(2 * w + 550, 2 * h + 280, 32, 184));

            // Room (4,2) — east woods: obstacle course
            AddWall(4, 2, new SolidRect(4 * w + 200, 2 * h + 150, 48, 48));
            AddWall(4, 2, new SolidRect(4 * w + 400, 2 * h + 250, 48, 48));
            AddWall(4, 2, new SolidRect(4 * w + 600, 2 * h + 150, 48, 48));

            // Room (4,3) — dungeon gate: pressure plate door
            var doorWall = new SolidRect(4 * w + 370, 3 * h + 200, 60, 16);
            _namedDoors["south_door"] = doorWall;
            AddWall(4, 3, doorWall);
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

            // Room [0,0] (The Village) - Progressive Start
            if (_player != null && GameRef.QuestFlags.Contains("Resonance_Awakened"))
            {
                var v1 = new Bamboo(new Vector2(100, 100));
                v1.LoadContent();
                _rooms.AddEnemy(v1, 0, 0);
            }

            // Room (3,2) — starting meadow: no enemies (safe zone)

            // Room (2,2) — raccoon den: boss fight
            var raccoon = new Raccoon(new Vector2(2 * w + 350, 2 * h + 240));
            raccoon.LoadContent();
            _rooms.AddEnemy(raccoon, 2, 2);

            // Room (4,2) — east woods: 3 bamboos
            for (int i = 0; i < 3; i++)
            {
                var b = new Bamboo(new Vector2(4 * w + 150 + i * 200, 2 * h + 200 + i * 50));
                b.LoadContent();
                _rooms.AddEnemy(b, 4, 2);
            }

            // Room (4,3) — dungeon gate: bamboo guards
            var g1 = new Bamboo(new Vector2(4 * w + 250, 3 * h + 150));
            g1.LoadContent();
            _rooms.AddEnemy(g1, 4, 3);

            var g2 = new Bamboo(new Vector2(4 * w + 550, 3 * h + 300));
            g2.LoadContent();
            _rooms.AddEnemy(g2, 4, 3);

            // Room (1,2) — deep forest: 2 bamboos
            var f1 = new Bamboo(new Vector2(w + 200, 2 * h + 180));
            f1.LoadContent();
            _rooms.AddEnemy(f1, 1, 2);

            var f2 = new Bamboo(new Vector2(w + 600, 2 * h + 300));
            f2.LoadContent();
            _rooms.AddEnemy(f2, 1, 2);

            // Room (5,2) — forest edge: 2 bamboos
            var e1 = new Bamboo(new Vector2(5 * w + 200, 2 * h + 200));
            e1.LoadContent();
            _rooms.AddEnemy(e1, 5, 2);

            var e2 = new Bamboo(new Vector2(5 * w + 500, 2 * h + 350));
            e2.LoadContent();
            _rooms.AddEnemy(e2, 5, 2);

            // Room (3,1) — highland border: 2 bamboos
            var h1 = new Bamboo(new Vector2(3 * w + 300, h + 200));
            h1.LoadContent();
            _rooms.AddEnemy(h1, 3, 1);

            var h2 = new Bamboo(new Vector2(3 * w + 500, h + 300));
            h2.LoadContent();
            _rooms.AddEnemy(h2, 3, 1);

            // Room (3,3) — south woods: 2 bamboos
            var s1 = new Bamboo(new Vector2(3 * w + 200, 3 * h + 180));
            s1.LoadContent();
            _rooms.AddEnemy(s1, 3, 3);

            var s2 = new Bamboo(new Vector2(3 * w + 600, 3 * h + 300));
            s2.LoadContent();
            _rooms.AddEnemy(s2, 3, 3);

            // Room (5,3) — far east outskirts: 3 bamboos
            for (int i = 0; i < 3; i++)
            {
                var b = new Bamboo(new Vector2(5 * w + 100 + i * 250, 3 * h + 150 + i * 60));
                b.LoadContent();
                _rooms.AddEnemy(b, 5, 3);
            }
        }

        private void SpawnBreakables()
        {
            int w = Game1.ScreenWidth;
            int h = Game1.ScreenHeight;

            Texture2D potTex = _potSprite ?? _grassSprite;
            Texture2D crateTex = _crateSprite ?? potTex;

            // Room (3,2) — starting meadow: pots with hearts
            if (potTex != null)
            {
                if (!WorldStateManager.GetFlag("Room_3_2_Pot_0")) _rooms.AddBreakable(new Breakable(new Vector2(3 * w + 100, 2 * h + 100), potTex, DropType.Heart) { UniqueId = "Room_3_2_Pot_0" }, 3, 2);
                if (!WorldStateManager.GetFlag("Room_3_2_Pot_1")) _rooms.AddBreakable(new Breakable(new Vector2(3 * w + 680, 2 * h + 100), potTex, DropType.Heart) { UniqueId = "Room_3_2_Pot_1" }, 3, 2);
            }

            // Room (2,2) — raccoon den: crates with ammo
            if (crateTex != null)
            {
                if (!WorldStateManager.GetFlag("Room_2_2_Crate_0")) _rooms.AddBreakable(new Breakable(new Vector2(2 * w + 100, 2 * h + 350), crateTex, DropType.Ammo) { UniqueId = "Room_2_2_Crate_0" }, 2, 2);
                if (!WorldStateManager.GetFlag("Room_2_2_Crate_1")) _rooms.AddBreakable(new Breakable(new Vector2(2 * w + 650, 2 * h + 200), crateTex, DropType.Ammo) { UniqueId = "Room_2_2_Crate_1" }, 2, 2);
            }

            // Room (4,2) — east woods: grass
            if (_grassSprite != null)
            {
                if (!WorldStateManager.GetFlag("Room_4_2_Grass_0")) _rooms.AddBreakable(new Breakable(new Vector2(4 * w + 300, 2 * h + 350), _grassSprite) { UniqueId = "Room_4_2_Grass_0" }, 4, 2);
                if (!WorldStateManager.GetFlag("Room_4_2_Grass_1")) _rooms.AddBreakable(new Breakable(new Vector2(4 * w + 500, 2 * h + 350), _grassSprite) { UniqueId = "Room_4_2_Grass_1" }, 4, 2);
            }

            // Room (4,3) — dungeon gate: pots
            if (potTex != null)
            {
                if (!WorldStateManager.GetFlag("Room_4_3_Pot_0")) _rooms.AddBreakable(new Breakable(new Vector2(4 * w + 300, 3 * h + 100), potTex, DropType.Heart) { UniqueId = "Room_4_3_Pot_0" }, 4, 3);
                if (!WorldStateManager.GetFlag("Room_4_3_Pot_1")) _rooms.AddBreakable(new Breakable(new Vector2(4 * w + 500, 3 * h + 100), potTex, DropType.None) { UniqueId = "Room_4_3_Pot_1" }, 4, 3);
            }
        }

        private void SpawnTriggers()
        {
            int w = Game1.ScreenWidth;
            int h = Game1.ScreenHeight;

            // Room (4,3) — dungeon gate: pressure plate opens the gate door
            var plate = new PressurePlate(
                new Vector2(4 * w + 400, 3 * h + 350), TriggerMode.Toggle);

            plate.OnStateChanged = (activated) =>
            {
                if (_namedDoors.TryGetValue("south_door", out SolidRect door))
                {
                    string key = "4,3";
                    if (activated)
                        _roomWalls[key].Remove(door);  // open door
                    else
                    {
                        if (!_roomWalls[key].Contains(door))
                            _roomWalls[key].Add(door);  // close door
                    }
                }
            };

            _rooms.AddTrigger(plate, 4, 3);
        }

        // ──────────────────────────────────────────────
        //  Spawning Interactables (NPCs, Signs)
        // ──────────────────────────────────────────────

        private void SpawnInteractables()
        {
            int w = Game1.ScreenWidth;
            int h = Game1.ScreenHeight;

            // Load dialogue data
            var dialogues = LoadDialogues();

            // ── Room (3,2) — Old Man Elam (starting meadow) ──
            if (dialogues.TryGetValue("old_man_elam", out var elamData))
            {
                var elam = new NPC("old_man_elam", elamData.name,
                    new Vector2(3 * w + 480, 2 * h + 120), elamData.pages);
                elam.SetQuestDialogue(elamData.questPages,
                    () => _player.Inventory.HasItem("boomerang"));
                elam.OnInteracted = () => GameRef.QuestFlags.Add("spoke_to_elam");
                AddInteractable(elam, 3, 2);
            }

            // ── Room (3,2) — Direction sign ──
            var sign1 = new SignPost(new Vector2(3 * w + 560, 2 * h + 400),
                "WEST: RACCOON DEN. EAST: RUINS. SOUTH: VILLAGE. NORTH: PEAKS.");
            AddInteractable(sign1, 3, 2);

            // ── Room (2,2) — Warning sign (raccoon den) ──
            var sign2 = new SignPost(new Vector2(2 * w + 120, 2 * h + 380),
                "DANGER. THE RACCOON GUARDIAN LURKS HERE. TURN BACK.");
            AddInteractable(sign2, 2, 2);

            // ── Room (4,3) — Silent Guard (dungeon gate) ──
            if (dialogues.TryGetValue("silent_guard", out var guardData))
            {
                var guard = new NPC("silent_guard", guardData.name,
                    new Vector2(4 * w + 370, 3 * h + 180), guardData.pages);
                guard.SetQuestDialogue(guardData.questPages,
                    () => _player.Inventory.HasItem("boomerang"));
                AddInteractable(guard, 4, 3);
            }

            // ── Room (2,3) — Mysterious Pot (quiet village) ──
            if (dialogues.TryGetValue("mysterious_pot", out var potData))
            {
                var pot = new NPC("mysterious_pot", potData.name,
                    new Vector2(2 * w + 400, 3 * h + 250), potData.pages);
                pot.SetQuestDialogue(potData.questPages,
                    () => GameRef.QuestFlags.Contains("raccoon_defeated"));
                AddInteractable(pot, 2, 3);
            }

            // ── Room (4,2) — Crystal sign (east woods) ──
            var sign3 = new SignPost(new Vector2(4 * w + 300, 2 * h + 400),
                "THE ECHO WAS LAST SEEN HERE. STRIKE THE CRYSTALS.");
            AddInteractable(sign3, 4, 2);

            // ── Room (5,3) — Deep cave hint ──
            var sign4 = new SignPost(new Vector2(5 * w + 400, 3 * h + 380),
                "THE RESONANCE GROWS STRONGER. THE TUNING FORK IS NEAR.");
            AddInteractable(sign4, 5, 3);

            // ── Room (3,1) — Highland border sign ──
            var sign5 = new SignPost(new Vector2(3 * w + 400, h + 380),
                "THE RESONANT PEAKS LIE NORTH. THE AIR HUMS WITH ANCIENT POWER.");
            AddInteractable(sign5, 3, 1);

            // ── Room (3,3) — South woods sign ──
            var sign6 = new SignPost(new Vector2(3 * w + 200, 3 * h + 100),
                "THE SILENT OUTSKIRTS BEGIN HERE. TREAD CAREFULLY.");
            AddInteractable(sign6, 3, 3);

            // ── Room (0,2) — Far west forest sign ──
            var sign7 = new SignPost(new Vector2(300, 2 * h + 400),
                "THE DEEP WOODS. FEW RETURN FROM HERE.");
            AddInteractable(sign7, 0, 2);

            // ── Room (6,2) — Far east edge sign ──
            var sign8 = new SignPost(new Vector2(6 * w + 400, 2 * h + 100),
                "THE EDGE OF THE ECHOING WOODS. BEYOND LIES SILENCE.");
            AddInteractable(sign8, 6, 2);

            // ── Treasure Chests ──

            // Room (2,2) — hidden chest in the forest
            AddInteractable(new TreasureChest(
                new Vector2(2 * w + 600, 2 * h + 350), "forest_chest", 20, "boomerang", "Boomerang"), 2, 2);

            // Room (4,2) — east plains chest
            AddInteractable(new TreasureChest(
                new Vector2(4 * w + 150, 2 * h + 200), "plains_chest", 30), 4, 2);

            // Room (3,1) — highlands chest with bow
            AddInteractable(new TreasureChest(
                new Vector2(3 * w + 600, h + 150), "highland_chest", 15, "bow", "Bow"), 3, 1);

            // Room (5,3) — outskirts chest with bombs
            AddInteractable(new TreasureChest(
                new Vector2(5 * w + 300, 3 * h + 250), "outskirts_chest", 25, "bomb", "Bombs"), 5, 3);

            // Room (1,2) — deep woods chest with health potion
            AddInteractable(new TreasureChest(
                new Vector2(w + 400, 2 * h + 300), "deepwoods_chest", 10, "health_potion", "Health Potion"), 1, 2);

            // Room (4,3) — swamp edge chest
            AddInteractable(new TreasureChest(
                new Vector2(4 * w + 500, 3 * h + 350), "swamp_chest", 40), 4, 3);

            // Room (6,1) — far east mountain chest with axe
            AddInteractable(new TreasureChest(
                new Vector2(6 * w + 200, h + 300), "mountain_chest", 20, "axe", "Axe"), 6, 1);
        }

        private void AddInteractable(Interactable interactable, int roomX, int roomY)
        {
            _interactables.Add(new RoomEntity<Interactable>(interactable, roomX, roomY));
        }

        // ──────────────────────────────────────────────
        //  Warp Tiles — Doors to Interiors / Dungeons
        // ──────────────────────────────────────────────

        private void SpawnWarpTiles()
        {
            int w = Game1.ScreenWidth;
            int h = Game1.ScreenHeight;

            // ── Room (2,1) — Blacksmith building ──
            int bx = 2 * w + 580; // building top-left X (world space)
            int by = h + 50;      // building top-left Y
            int bw = 128;         // building width
            int bh = 120;         // building height (walls only, roof extends above)
            int doorW = 36;
            int doorX = bx + bw / 2 - doorW / 2;
            int doorY = by + bh - 4;

            // Add building definition for rendering
            _buildings.Add(new Building
            {
                X = bx, Y = by, Width = bw, Height = bh,
                DoorX = doorX, DoorY = doorY, DoorW = doorW, DoorH = 8,
                Label = "BLACKSMITH"
            });

            // Collision walls around the building (leaving door opening)
            AddWall(2, 1, new SolidRect(bx, by, bw, 32));              // top wall / roof base
            AddWall(2, 1, new SolidRect(bx, by + 32, 16, bh - 32));    // left wall
            AddWall(2, 1, new SolidRect(bx + bw - 16, by + 32, 16, bh - 32)); // right wall
            AddWall(2, 1, new SolidRect(bx, by + bh - 4, doorX - bx, 16));     // bottom-left
            AddWall(2, 1, new SolidRect(doorX + doorW, by + bh - 4, bx + bw - (doorX + doorW), 16)); // bottom-right

            // Warp tile at the door
            _warpTiles.Add(WarpTile.ToInterior(
                new Rectangle(doorX, doorY, doorW, 12),
                "blacksmith_house",
                new Vector2(160, 180))); // spawn inside the house

            // ── Room (3,2) — Hero's House ──
            int hx = 3 * w + 100;
            int hy = 2 * h + 50;
            int hbw = 160;
            int hbh = 140;
            int hdoorW = 40;
            int hdoorX = hx + hbw / 2 - hdoorW / 2;
            int hdoorY = hy + hbh - 4;

            _buildings.Add(new Building
            {
                X = hx, Y = hy, Width = hbw, Height = hbh,
                DoorX = hdoorX, DoorY = hdoorY, DoorW = hdoorW, DoorH = 8,
                Label = "HERO'S HOUSE"
            });

            AddWall(3, 2, new SolidRect(hx, hy, hbw, 40));
            AddWall(3, 2, new SolidRect(hx, hy + 40, 20, hbh - 40));
            AddWall(3, 2, new SolidRect(hx + hbw - 20, hy + 40, 20, hbh - 40));
            AddWall(3, 2, new SolidRect(hx, hdoorY, hdoorX - hx, 10));
            AddWall(3, 2, new SolidRect(hdoorX + hdoorW, hdoorY, hx + hbw - (hdoorX + hdoorW), 10));

            _warpTiles.Add(WarpTile.ToInterior(
                new Rectangle(hdoorX, hdoorY, hdoorW, 15),
                "hero_house",
                new Vector2(180, 250)));
        }

        private struct DialogueData
        {
            public string name;
            public List<DialogueBox.DialoguePage> pages;
            public List<DialogueBox.DialoguePage> questPages;
        }

        private Dictionary<string, DialogueData> LoadDialogues()
        {
            var result = new Dictionary<string, DialogueData>();
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "Content", "Data", "dialogues.json");

            if (!File.Exists(path)) return result;

            try
            {
                string json = File.ReadAllText(path);
                using var doc = JsonDocument.Parse(json);

                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    var data = new DialogueData
                    {
                        name = prop.Value.GetProperty("name").GetString(),
                        pages = new List<DialogueBox.DialoguePage>(),
                        questPages = new List<DialogueBox.DialoguePage>()
                    };

                    foreach (var page in prop.Value.GetProperty("default").EnumerateArray())
                    {
                        data.pages.Add(new DialogueBox.DialoguePage
                        {
                            Speaker = page.GetProperty("speaker").GetString(),
                            Text = page.GetProperty("text").GetString()
                        });
                    }

                    if (prop.Value.TryGetProperty("quest_complete", out var qc))
                    {
                        foreach (var page in qc.EnumerateArray())
                        {
                            data.questPages.Add(new DialogueBox.DialoguePage
                            {
                                Speaker = page.GetProperty("speaker").GetString(),
                                Text = page.GetProperty("text").GetString()
                            });
                        }
                    }

                    result[prop.Name] = data;
                }
            }
            catch { /* silently ignore bad JSON */ }

            return result;
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

            // ── Always tick camera shake ──
            _camera.UpdateShake(dt);

            // ── Warp cooldown (prevents instant re-entry after exiting interior) ──
            if (_warpCooldown > 0f) _warpCooldown -= dt;

            // ── Dialogue active — freeze gameplay, only update dialogue ──
            if (_dialogueBox.IsActive)
            {
                _dialogueBox.Update(gameTime);
                if (_dialogueBox.JustClosed && _activeInteractable != null)
                {
                    _activeInteractable.OnDialogueComplete();
                    _activeInteractable = null;
                }
                _prevKb = kb;
                return;
            }

            // ── Debug Toggle ──
            if (kb.IsKeyDown(Keys.F3) && _prevKb.IsKeyUp(Keys.F3))
                _showDebug = !_showDebug;

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
                RefreshActiveInteractables();
                _camera.Shake(3f, 0.15f); // subtle shake on room change
                _camera.Update(gameTime);
                _prevKb = kb;
                return;
            }

            // ── Check warp tiles (doors to interiors) ──
            if (_warpCooldown > 0f) goto SkipWarps;
            foreach (var warp in _warpTiles)
            {
                if (warp.IsPlayerOn(_player.BoundingBox))
                {
                    if (warp.Target == WarpTarget.Interior)
                    {
                        GameRef.EnterInterior(warp.InteriorId, warp.SpawnPosition);
                        _prevKb = kb;
                        return;
                    }
                    else if (warp.Target == WarpTarget.Dungeon)
                    {
                        GameRef.ChangeState(GameState.Dungeon);
                        _prevKb = kb;
                        return;
                    }
                }
            }
            SkipWarps:

            // ── Check NPC/Sign interaction (E key) ──
            if (kb.IsKeyDown(Keys.E) && _prevKb.IsKeyUp(Keys.E))
            {
                // First check NPC/Sign
                foreach (var re in _interactables)
                {
                    if (!re.IsActive) continue;
                    if (re.Entity.IsPlayerInRange(_player.BoundingBox))
                    {
                        var pages = re.Entity.GetDialogue();
                        if (pages != null && pages.Count > 0)
                        {
                            _dialogueBox.Open(pages);
                            _activeInteractable = re.Entity;
                            _prevKb = kb;
                            return;
                        }
                    }
                }

            }

            // ── Auto-pickup collectibles on walk-over ──
            {
                Rectangle playerReach = new Rectangle(
                    _player.BoundingBox.X - 20, _player.BoundingBox.Y - 20,
                    _player.BoundingBox.Width + 40, _player.BoundingBox.Height + 40);

                for (int ci = 0; ci < _collectibles.Count; ci++)
                {
                    var c = _collectibles[ci];
                    if (c.Collected) continue;
                    if (c.RoomX != _camera.RoomX || c.RoomY != _camera.RoomY) continue;

                    int cw = c.Source.Width * 2;
                    int ch = c.Source.Height * 2;
                    Rectangle cBounds = new Rectangle((int)c.Position.X, (int)c.Position.Y, cw, ch);
                    if (playerReach.Intersects(cBounds))
                    {
                        _player.Inventory.AddItem(c.ItemId);
                        c.Collected = true;
                        _collectibles[ci] = c;
                        string itemName = c.ItemId.Substring(0, 1).ToUpper() + c.ItemId.Substring(1);
                        _pickupMessage = $"+1 {itemName}";
                        _pickupMessageTimer = 1.5f;
                        break;
                    }
                }
            }

            // Tick pickup message
            if (_pickupMessageTimer > 0f)
                _pickupMessageTimer -= dt;

            // ── Track state for SFX ──
            string prevState = _player.CurrentStateName;

            // ── 1. Update player ──
            _player.Update(gameTime);

            if (_player.CurrentStateName == "attack" && prevState != "attack")
                _swordSfx?.Play(0.5f, 0f, 0f);

            // ── 2. Resolve player vs. walls (axis-separated move + collide) ──
            var walls = GetCurrentWalls();
            CollisionSystem.MoveAndResolve(_player, walls, dt);

            // ── 3. Update enemies (only in current room) ──
            _rooms.UpdateEnemies(gameTime, _player.Position);

            var activeEnemies = _rooms.GetActiveEnemies();
            foreach (var enemy in activeEnemies)
                CollisionSystem.MoveAndResolve(enemy, walls, 0);

            // ── 4. Combat ──
            int prevHealth = _player.CurrentHealth;
            int aliveEnemiesBefore = 0;
            foreach (var e in activeEnemies) if (e.IsAlive) aliveEnemiesBefore++;

            CollisionSystem.CheckAttackHits(_player, activeEnemies);
            CollisionSystem.CheckEnemyContact(_player, activeEnemies);

            int aliveEnemiesAfter = 0;
            foreach (var e in activeEnemies) if (e.IsAlive) aliveEnemiesAfter++;

            if (aliveEnemiesAfter < aliveEnemiesBefore)
            {
                _deathSfx?.Play(0.5f, 0f, 0f);
                _camera.Shake(5f, 0.2f); // shake on enemy death

                // Award coins for each kill
                int kills = aliveEnemiesBefore - aliveEnemiesAfter;
                int coinsEarned = kills * (2 + new System.Random().Next(3)); // 2-4 coins per kill
                _player.Inventory.Coins += coinsEarned;
                _pickupMessage = $"+{coinsEarned} Coins";
                _pickupMessageTimer = 1.5f;

                // Check quest flags — raccoon defeated (room 1,1)
                if (_camera.RoomX == 1 && _camera.RoomY == 1 && aliveEnemiesAfter == 0)
                    GameRef.QuestFlags.Add("raccoon_defeated");
            }

            if (_player.CurrentHealth < prevHealth)
            {
                _hitSfx?.Play(0.6f, 0f, 0f);
                _damageFlashTimer = DamageFlashDuration;
                _camera.Shake(3f, 0.15f); // shake on player hit
            }

            if (_damageFlashTimer > 0f)
                _damageFlashTimer -= dt;

            // ── Low health pulse timer ──
            if (_player.CurrentHealth <= 2)
                _lowHealthTimer += dt;

            // --- Inventory Trigger ---
            if (kb.IsKeyDown(Keys.I) && _prevKb.IsKeyUp(Keys.I))
            {
                GameRef.ChangeState(GameState.Inventory);
                return;
            }

            // --- Capture Pulse Logic ---
            if (_player.CurrentStateName == "capture")
            {
                float catchRange = 80f;
                foreach (var enemy in activeEnemies)
                {
                    if (enemy.IsAlive && Vector2.Distance(_player.Position, enemy.Position) < catchRange)
                    {
                        // Check if enemy is at low health (1 HP or < 30%)
                        if (enemy.CurrentHealth <= 1 || (float)enemy.CurrentHealth / enemy.MaxHealth <= 0.3f)
                        {
                            // Capture!
                            _pulseEffects.Add(new PulseEffect(enemy.Position, 100f, 2f));
                            _camera.Shake(8f, 0.3f);
                            
                            // Create Echo (Generic for now, or based on enemy type)
                            // We use a placeholder texture for the Echo.
                            var echo = new EchoEntity(enemy.GetType().Name, enemy.Sprite, null);
                            if (_player.Party.CaptureEcho(echo, _player))
                            {
                                enemy.TakeDamage(99); // Destroy enemy
                                _deathSfx?.Play(0.7f, 0.5f, 0f);
                            }
                        }
                    }
                }
            }

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
                        _player.Inventory.AddItem("bow", 10);
                }
                // Breakables also drop a few coins
                if (drop != DropType.None)
                {
                    int coins = 1 + new System.Random().Next(3);
                    _player.Inventory.Coins += coins;
                    _pickupMessage = $"+{coins} Coins";
                    _pickupMessageTimer = 1.5f;
                }
            }

            // ── 7. Update triggers ──
            _rooms.UpdateTriggers(gameTime, _player.BoundingBox);

            // ── 8. Update interactables (check proximity for prompts) ──
            foreach (var re in _interactables)
            {
                if (!re.IsActive) continue;
                re.Entity.IsPlayerInRange(_player.BoundingBox);
                re.Entity.Update(gameTime);
            }

            // ── 9. Update Party Echoes ──
            _player.Party.Update(_player.Position, gameTime);

            // ── 10. Update projectiles ──
            for (int i = _projectiles.Count - 1; i >= 0; i--)
            {
                var proj = _projectiles[i];
                proj.Update(gameTime, walls);

                foreach (var enemy in activeEnemies)
                {
                    if (proj.TryHit(enemy))
                    {
                        enemy.TargetPosition = _player.Position;
                        enemy.TakeDamage(proj.Damage);
                    }
                }

                foreach (var b in _rooms.GetActiveBreakables())
                {
                    if (!b.IsDestroyed && proj.BoundingBox.Intersects(b.BoundingBox))
                    {
                        b.Destroy();
                        if (!proj.IsBoomerang) proj.IsAlive = false;
                    }
                }

                // ── Boomerang vs Sound-Crystal tiles (ID 55) ──
                if (proj.IsBoomerang && proj.IsAlive)
                {
                    string roomKey = $"{_camera.RoomX},{_camera.RoomY}";
                    if (_roomTileMaps.TryGetValue(roomKey, out var tileMap))
                    {
                        Vector2 roomOffset = new Vector2(
                            _camera.RoomX * Game1.ScreenWidth,
                            _camera.RoomY * Game1.ScreenHeight);
                        int tileId = tileMap.GetTileAtWorld(proj.Position.X, proj.Position.Y, roomOffset);
                        if (tileId == 55)
                        {
                            _pulseEffects.Add(new PulseEffect(proj.Position, 120f, 3f));
                            _camera.Shake(6f, 0.25f);
                        }
                    }
                }

                if (!proj.IsAlive)
                    _projectiles.RemoveAt(i);
            }

            // ── 10. Update pulse effects ──
            for (int i = _pulseEffects.Count - 1; i >= 0; i--)
            {
                _pulseEffects[i].Update(gameTime);
                if (!_pulseEffects[i].IsActive)
                    _pulseEffects.RemoveAt(i);
            }

            _prevKb = kb;
        }

        /// <summary>
        /// Activates interactables in the current room only.
        /// </summary>
        private void RefreshActiveInteractables()
        {
            foreach (var re in _interactables)
            {
                re.IsActive = (re.RoomX == _camera.RoomX && re.RoomY == _camera.RoomY);
            }
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

            // ── Dialogue box (screen-space, on top of everything) ──
            _dialogueBox.Draw(spriteBatch);
        }

        private void DrawWorld(SpriteBatch sb)
        {
            Texture2D px = Game1.PixelTexture;
            int w = Game1.ScreenWidth;
            int h = Game1.ScreenHeight;

            // ── Draw floor tiles for all visible rooms ──
            for (int ry = _camera.RoomY - 1; ry <= _camera.RoomY + 1; ry++)
            {
                for (int rx = _camera.RoomX - 1; rx <= _camera.RoomX + 1; rx++)
                {
                    if (rx < 0 || rx >= WorldRoomsX || ry < 0 || ry >= WorldRoomsY) continue;

                    int ox = rx * w;
                    int oy = ry * h;
                    string key = $"{rx},{ry}";

                    // ── Use TileMap if available ──
                    if (_roomTileMaps.ContainsKey(key))
                    {
                        _roomTileMaps[key].Draw(sb, new Vector2(ox, oy));
                    }
                    else
                    {
                        // Fallback: solid color + tiled texture
                        Color bgColor = GetRoomColor(rx, ry);
                        sb.Draw(px, new Rectangle(ox, oy, w, h), bgColor);

                        Texture2D floor = ry == 1 ? (_dungeonFloorTile ?? _floorTile) : _floorTile;
                        if (floor != null)
                        {
                            for (int x = ox; x < ox + w; x += floor.Width)
                                for (int y = oy; y < oy + h; y += floor.Height)
                                    sb.Draw(floor, new Vector2(x, y), Color.White * 0.6f);
                        }
                    }

                    // ── Draw wall collision rects on top (debug-style tiled) ──
                    if (_roomWalls.ContainsKey(key))
                    {
                        foreach (var wall in _roomWalls[key])
                        {
                            if (_wallTile != null)
                            {
                                for (int wx = wall.Bounds.X; wx < wall.Bounds.Right; wx += _wallTile.Width)
                                    for (int wy = wall.Bounds.Y; wy < wall.Bounds.Bottom; wy += _wallTile.Height)
                                    {
                                        int dw = System.Math.Min(_wallTile.Width, wall.Bounds.Right - wx);
                                        int dh = System.Math.Min(_wallTile.Height, wall.Bounds.Bottom - wy);
                                        sb.Draw(_wallTile, new Rectangle(wx, wy, dw, dh),
                                            new Rectangle(0, 0, dw, dh), Color.White);
                                    }
                            }
                            else
                            {
                                Color wallColor = ry >= 4 ? new Color(50, 35, 45) : ry <= 1 ? new Color(90, 85, 75) : new Color(80, 55, 40);
                                sb.Draw(px, wall.Bounds, wallColor);
                            }
                        }
                    }
                }
            }

            // ── SORTED RENDER QUEUE ──
            var renderQueue = new List<(float sortY, System.Action draw)>();

            // 1. Decorations
            for (int ry2 = _camera.RoomY - 1; ry2 <= _camera.RoomY + 1; ry2++)
            {
                for (int rx2 = _camera.RoomX - 1; rx2 <= _camera.RoomX + 1; rx2++)
                {
                    string dk = $"{rx2},{ry2}";
                    if (_roomDecorations.TryGetValue(dk, out var decs))
                    {
                        foreach (var dec in decs)
                        {
                            var dCopy = dec;
                            int dw = (int)(dCopy.Source.Width * dCopy.Scale);
                            int dh = (int)(dCopy.Source.Height * dCopy.Scale);
                            renderQueue.Add((dCopy.Position.Y + dh, () =>
                            {
                                sb.Draw(dCopy.Sheet,
                                    new Rectangle((int)dCopy.Position.X, (int)dCopy.Position.Y, dw, dh),
                                    dCopy.Source, Color.White);
                            }));
                        }
                    }
                }
            }

            // 1b. Collectible pickups (with floating bob and glow)
            foreach (var c in _collectibles)
            {
                if (c.Collected) continue;
                if (c.RoomX != _camera.RoomX && c.RoomX != _camera.RoomX - 1 && c.RoomX != _camera.RoomX + 1) continue;
                if (c.RoomY != _camera.RoomY && c.RoomY != _camera.RoomY - 1 && c.RoomY != _camera.RoomY + 1) continue;

                var cc = c;
                int drawW = cc.Source.Width * 2;  // scale up 2x
                int drawH = cc.Source.Height * 2;
                renderQueue.Add((cc.Position.Y + drawH, () =>
                {
                    float bob = MathF.Sin(_lowHealthTimer * 3f + cc.Position.X) * 3f;
                    int dx = (int)cc.Position.X;
                    int dy = (int)cc.Position.Y + (int)bob;

                    // Glow circle
                    sb.Draw(px, new Rectangle(dx - 2, dy - 2, drawW + 4, drawH + 4), Color.Yellow * 0.12f);

                    // Sprite
                    sb.Draw(cc.Sheet, new Rectangle(dx, dy, drawW, drawH), cc.Source, Color.White);

                    // Sparkle indicator (shows it's collectible)
                    float sparkle = MathF.Sin(_lowHealthTimer * 5f + cc.Position.Y) * 0.5f + 0.5f;
                    sb.Draw(px, new Rectangle(dx + drawW / 2 - 2, dy - 4, 4, 4), Color.White * sparkle);
                }));
            }

            // 2. Buildings
            foreach (var bld in _buildings)
            {
                var bCopy = bld;
                renderQueue.Add((bCopy.Y + bCopy.Height, () => DrawBuilding(sb, px, bCopy)));
            }

            // 3. Triggers
            renderQueue.Add((0f, () => _rooms.DrawTriggers(sb)));

            // 4. Interactables
            foreach (var re in _interactables)
            {
                if (re.IsActive)
                {
                    var ent = re.Entity;
                    renderQueue.Add((ent.Position.Y + ent.Height, () => ent.Draw(sb)));
                }
            }

            // 5. Breakables
            foreach (var re in _rooms.Breakables)
            {
                if (re.IsActive)
                {
                    var ent = re.Entity;
                    renderQueue.Add((ent.Position.Y + ent.Height, () => ent.Draw(sb)));
                }
            }

            // 6. Enemies
            foreach (var re in _rooms.Enemies)
            {
                if (re.IsActive)
                {
                    var ent = re.Entity;
                    renderQueue.Add((ent.Position.Y + ent.HitboxOffset.Y + ent.HitboxHeight, () => ent.Draw(sb)));
                }
            }

            // 7. Pulse Effects & Projectiles
            renderQueue.Add((float.MaxValue, () =>
            {
                foreach (var pulse in _pulseEffects) pulse.Draw(sb);
                foreach (var proj in _projectiles) proj.Draw(sb);
            }));

            // 8. Player
            renderQueue.Add((_player.Position.Y + _player.HitboxOffset.Y + _player.HitboxHeight, () => 
            {
                if (_player.CurrentHealth <= 2 && _player.CurrentHealth > 0)
                {
                    float flash = MathF.Sin(_lowHealthTimer * 6f);
                    _player.Tint = flash > 0.3f ? new Color(255, 150, 150) : Color.White;
                }
                else
                {
                    _player.Tint = Color.White;
                }
                _player.Draw(sb);

                // Draw Party Echoes (following player)
                _player.Party.Draw(sb);

                if (_showDebug)
                {
                    _player.DrawHitbox(sb, Color.Lime);
                    foreach (var e in _rooms.GetActiveEnemies())
                        e.DrawHitbox(sb, Color.Red);

                    if (_player.CurrentStateName == "attack" && _player.AttackHitbox != Rectangle.Empty)
                        sb.Draw(px, _player.AttackHitbox, Color.Yellow * 0.4f);
                }
            }));

            // Execute Sorted Queue
            renderQueue.Sort((a, b) => a.sortY.CompareTo(b.sortY));
            foreach (var item in renderQueue)
            {
                item.draw();
            }

        }

        // ──────────────────────────────────────────────
        //  Building Rendering
        // ──────────────────────────────────────────────

        private void DrawBuilding(SpriteBatch sb, Texture2D px, Building bld)
        {
            int x = bld.X, y = bld.Y, bw = bld.Width, bh = bld.Height;

            if (_villageTileset != null)
            {
                // ── Roof (triangular shape using colored rects + tileset decoration) ──
                int roofPeak = y - 28;
                int roofLeft = x - 12;
                int roofRight = x + bw + 12;
                int roofBase = y + 8;

                // Clamp roof so it doesn't bleed into adjacent rooms
                int roomTopEdge = (y / Game1.ScreenHeight) * Game1.ScreenHeight;
                if (roofPeak < roomTopEdge) roofPeak = roomTopEdge;

                // Roof background - layered horizontal strips narrowing to peak
                int roofH = roofBase - roofPeak;
                if (roofH > 0)
                {
                    for (int ry2 = 0; ry2 < roofH; ry2 += 4)
                    {
                        float t = (float)ry2 / roofH;
                        int stripW = (int)((roofRight - roofLeft) * (1f - t * 0.7f));
                        int stripX = x + bw / 2 - stripW / 2;
                        Color roofColor = new Color(
                            (int)(180 - t * 40),
                            (int)(80 - t * 20),
                            (int)(30 + t * 10));
                        sb.Draw(px, new Rectangle(stripX, roofPeak + ry2, stripW, 5), roofColor);
                    }
                }

                // Roof outline (chimney)
                int chimneyY = System.Math.Max(roofPeak - 4, roomTopEdge);
                sb.Draw(px, new Rectangle(x + bw / 2 - 2, chimneyY, 4, 8), new Color(120, 50, 20));

                // ── Walls ──
                // Main wall body
                sb.Draw(px, new Rectangle(x, y + 8, bw, bh - 8), new Color(140, 110, 70));
                // Wall detail - horizontal stone lines
                for (int wy = y + 20; wy < y + bh - 8; wy += 16)
                {
                    sb.Draw(px, new Rectangle(x + 4, wy, bw - 8, 1), new Color(110, 85, 55));
                }
                // Vertical stone lines (brick pattern)
                for (int wx = x + 16; wx < x + bw - 8; wx += 24)
                {
                    int lineY = ((wx / 24) % 2 == 0) ? y + 12 : y + 20;
                    for (int wy = lineY; wy < y + bh - 8; wy += 32)
                    {
                        sb.Draw(px, new Rectangle(wx, wy, 1, 14), new Color(110, 85, 55));
                    }
                }

                // ── Window (left side) ──
                int winX = x + 16, winY = y + 28, winW = 20, winH = 20;
                sb.Draw(px, new Rectangle(winX - 2, winY - 2, winW + 4, winH + 4), new Color(80, 55, 30)); // frame
                sb.Draw(px, new Rectangle(winX, winY, winW, winH), new Color(60, 90, 120)); // glass
                sb.Draw(px, new Rectangle(winX + winW / 2, winY, 1, winH), new Color(80, 55, 30)); // cross
                sb.Draw(px, new Rectangle(winX, winY + winH / 2, winW, 1), new Color(80, 55, 30)); // cross
                // Window glow
                sb.Draw(px, new Rectangle(winX + 2, winY + 2, winW / 2 - 3, winH / 2 - 3), new Color(90, 120, 150));

                // ── Window (right side) ──
                int win2X = x + bw - 36;
                sb.Draw(px, new Rectangle(win2X - 2, winY - 2, winW + 4, winH + 4), new Color(80, 55, 30));
                sb.Draw(px, new Rectangle(win2X, winY, winW, winH), new Color(60, 90, 120));
                sb.Draw(px, new Rectangle(win2X + winW / 2, winY, 1, winH), new Color(80, 55, 30));
                sb.Draw(px, new Rectangle(win2X, winY + winH / 2, winW, 1), new Color(80, 55, 30));
                sb.Draw(px, new Rectangle(win2X + 2, winY + 2, winW / 2 - 3, winH / 2 - 3), new Color(90, 120, 150));

                // ── Door ──
                sb.Draw(px, new Rectangle(bld.DoorX - 2, bld.DoorY - 28, bld.DoorW + 4, 32), new Color(70, 45, 25)); // frame
                sb.Draw(px, new Rectangle(bld.DoorX, bld.DoorY - 26, bld.DoorW, 30), new Color(45, 30, 15)); // dark interior
                // Door arch
                sb.Draw(px, new Rectangle(bld.DoorX + 2, bld.DoorY - 28, bld.DoorW - 4, 3), new Color(90, 60, 30));

                // ── Anvil decoration (outside, right of door) ──
                int anvilX = bld.DoorX + bld.DoorW + 16;
                int anvilY = y + bh - 16;
                sb.Draw(px, new Rectangle(anvilX, anvilY + 4, 16, 8), new Color(80, 80, 90)); // base
                sb.Draw(px, new Rectangle(anvilX - 2, anvilY, 20, 5), new Color(100, 100, 110)); // top
                sb.Draw(px, new Rectangle(anvilX + 6, anvilY - 3, 4, 4), new Color(110, 110, 120)); // horn

                // ── Wall border ──
                sb.Draw(px, new Rectangle(x, y + 8, bw, 2), new Color(100, 70, 40)); // top edge
                sb.Draw(px, new Rectangle(x, y + 8, 2, bh - 8), new Color(100, 70, 40)); // left edge
                sb.Draw(px, new Rectangle(x + bw - 2, y + 8, 2, bh - 8), new Color(100, 70, 40)); // right edge

                // ── Label with background panel ──
                int labelW = PixelFont.MeasureWidth(bld.Label, 1);
                int labelX = x + bw / 2 - labelW / 2;
                int labelY = System.Math.Max(roofPeak - 14, roomTopEdge + 2);
                sb.Draw(px, new Rectangle(labelX - 4, labelY - 2, labelW + 8, 12),
                    Color.Black * 0.6f);
                PixelFont.DrawString(sb, bld.Label, labelX, labelY, Color.Gold, 1);
            }
            else
            {
                // Fallback: simple colored rectangles
                sb.Draw(px, new Rectangle(x, y - 20, bw, 24), new Color(160, 80, 30)); // roof
                sb.Draw(px, new Rectangle(x, y, bw, bh), new Color(120, 90, 60)); // walls
                sb.Draw(px, new Rectangle(bld.DoorX, bld.DoorY - 24, bld.DoorW, 28), new Color(50, 30, 15)); // door
                int labelW = PixelFont.MeasureWidth(bld.Label, 1);
                int labelX2 = x + bw / 2 - labelW / 2;
                sb.Draw(px, new Rectangle(labelX2 - 4, y - 32, labelW + 8, 12),
                    Color.Black * 0.6f);
                PixelFont.DrawString(sb, bld.Label,
                    labelX2, y - 30, Color.Gold, 1);
            }
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
                {
                    // Each heart is now a clean 16x16 extracted sprite — scale up to heartSize
                    sb.Draw(heartTex, new Rectangle((int)pos.X, (int)pos.Y, heartSize, heartSize), Color.White);
                }
                else
                {
                    sb.Draw(px, new Rectangle((int)pos.X, (int)pos.Y, heartSize, heartSize),
                        hpLeft > 0 ? Color.Red : Color.DarkRed);
                }
            }

            // ── Item slot ──
            _player.Inventory.DrawHUD(sb, px);

            // ── Room indicator (small minimap) ──
            int indicatorX = Game1.ScreenWidth / 2 - WorldRoomsX * 8;
            int indicatorY = 8;
            // Minimap background
            sb.Draw(px, new Rectangle(indicatorX - 3, indicatorY - 3,
                WorldRoomsX * 16 + 6, WorldRoomsY * 12 + 6), Color.Black * 0.5f);
            for (int ry = 0; ry < WorldRoomsY; ry++)
            {
                for (int rx = 0; rx < WorldRoomsX; rx++)
                {
                    bool isCurrent = (rx == _camera.RoomX && ry == _camera.RoomY);
                    Color c = isCurrent ? Color.White : Color.Gray * 0.4f;
                    sb.Draw(px, new Rectangle(
                        indicatorX + rx * 16, indicatorY + ry * 12, 14, 10), c);
                    // Border
                    if (isCurrent)
                    {
                        sb.Draw(px, new Rectangle(indicatorX + rx * 16 - 1, indicatorY + ry * 12 - 1, 16, 1), Color.Gold);
                        sb.Draw(px, new Rectangle(indicatorX + rx * 16 - 1, indicatorY + ry * 12 + 10, 16, 1), Color.Gold);
                        sb.Draw(px, new Rectangle(indicatorX + rx * 16 - 1, indicatorY + ry * 12, 1, 10), Color.Gold);
                        sb.Draw(px, new Rectangle(indicatorX + rx * 16 + 14, indicatorY + ry * 12, 1, 10), Color.Gold);
                    }
                }
            }

            // ── Pickup message ──
            if (_pickupMessageTimer > 0f && _pickupMessage != null)
            {
                float alpha = System.Math.Min(1f, _pickupMessageTimer / 0.3f);
                int msgW = PixelFont.MeasureWidth(_pickupMessage, 2);
                int msgX = Game1.ScreenWidth / 2 - msgW / 2;
                int msgY = Game1.ScreenHeight - 60;
                sb.Draw(px, new Rectangle(msgX - 8, msgY - 4, msgW + 16, 24), Color.Black * (0.6f * alpha));
                PixelFont.DrawString(sb, _pickupMessage, msgX, msgY, Color.Lime * alpha, 2);
            }

            // ── Damage flash overlay ──
            if (_damageFlashTimer > 0f)
            {
                float flashAlpha = (_damageFlashTimer / DamageFlashDuration) * 0.35f;
                sb.Draw(px, new Rectangle(0, 0, Game1.ScreenWidth, Game1.ScreenHeight),
                    Color.Red * flashAlpha);
            }
        }
    }
}
