// ============================================================================
// InteriorState.cs — Interior Map Handler (Houses, Caves)
// Author: Mehdi Lakhouane
// Description: Handles localized interior maps loaded from JSON. Supports
//              two camera modes: 'Locked' for small rooms (houses) and
//              'Follow' for larger spaces (caves). The Player instance is
//              shared with the Overworld via Game1.SharedPlayer.
// ============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
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
    public class InteriorState : GameStateBase
    {
        // ── Interior data ──
        private string _interiorId;
        private TileMap _floorMap;
        private TileMap _decorMap;
        private readonly List<SolidRect> _walls = new();
        private readonly List<Interactable> _interactables = new();
        private readonly List<Projectile> _projectiles = new();
        private readonly List<PulseEffect> _pulseEffects = new();

        // ── Camera mode ──
        private bool _cameraFollow; // true = follow player (caves), false = locked (houses)
        private Vector2 _cameraOffset;
        private int _mapPixelWidth;
        private int _mapPixelHeight;

        // ── Warp exit ──
        private WarpTile _exitWarp;

        // ── Shared player ──
        private Player _player;

        // ── Dialogue ──
        private readonly DialogueBox _dialogueBox = new();
        private Interactable _activeInteractable;

        // ── Tileset ──
        private Texture2D _tileset;

        // ── Input ──
        private KeyboardState _prevKb = Keyboard.GetState();

        // ── Interior light pulses (fireplaces, crystals) ──
        private readonly List<PulseEffect> _lightSources = new();
        private float _warpCooldown;

        public InteriorState(Game1 game, ContentManager content)
            : base(game, content)
        {
        }

        /// <summary>
        /// Loads and activates an interior map. Called during fade midpoint.
        /// </summary>
        public void LoadInterior(string interiorId, Player player, Vector2 spawnPos)
        {
            _interiorId = interiorId;
            _player = player;
            _player.Position = spawnPos;

            // Clear old data
            _walls.Clear();
            _interactables.Clear();
            _projectiles.Clear();
            _pulseEffects.Clear();
            _lightSources.Clear();
            _floorMap = null;
            _decorMap = null;

            // Load tileset
            if (_tileset == null && AssetLoader.Exists("Tiles/game_tileset.png"))
                _tileset = AssetLoader.LoadTexture("Tiles/game_tileset.png");

            // Load JSON definition
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "Content", "Data", $"{interiorId}.json");

            if (!File.Exists(path)) return;

            try
            {
                string json = File.ReadAllText(path);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // ── Map dimensions ──
                int cols = root.GetProperty("width").GetInt32();
                int rows = root.GetProperty("height").GetInt32();
                int scale = root.TryGetProperty("scale", out var s) ? s.GetInt32() : 2;

                _mapPixelWidth = cols * 16 * scale;
                _mapPixelHeight = rows * 16 * scale;

                // Camera mode
                _cameraFollow = root.TryGetProperty("camera", out var cam)
                    && cam.GetString() == "follow";

                // ── Floor layer ──
                if (root.TryGetProperty("floor", out var floorArr))
                {
                    var floorData = ParseTileLayer(floorArr, rows, cols);
                    _floorMap = new TileMap(16, 16);
                    if (_tileset != null) _floorMap.SetTileset(_tileset);
                    _floorMap.RenderScale = scale;
                    _floorMap.LoadFromArray(floorData);
                }

                // ── Decor layer (drawn on top of floor) ──
                if (root.TryGetProperty("decor", out var decorArr))
                {
                    var decorData = ParseTileLayer(decorArr, rows, cols);
                    _decorMap = new TileMap(16, 16);
                    if (_tileset != null) _decorMap.SetTileset(_tileset);
                    _decorMap.RenderScale = scale;
                    _decorMap.LoadFromArray(decorData);
                }

                // ── Walls ──
                if (root.TryGetProperty("walls", out var wallsArr))
                {
                    foreach (var w in wallsArr.EnumerateArray())
                    {
                        _walls.Add(new SolidRect(
                            w.GetProperty("x").GetInt32(),
                            w.GetProperty("y").GetInt32(),
                            w.GetProperty("w").GetInt32(),
                            w.GetProperty("h").GetInt32()));
                    }
                }

                // ── Exit warp ──
                if (root.TryGetProperty("exit", out var exit))
                {
                    var zone = new Rectangle(
                        exit.GetProperty("x").GetInt32(),
                        exit.GetProperty("y").GetInt32(),
                        exit.GetProperty("w").GetInt32(),
                        exit.GetProperty("h").GetInt32());

                    var spawnOut = new Vector2(
                        exit.GetProperty("spawn_x").GetSingle(),
                        exit.GetProperty("spawn_y").GetSingle());

                    int rx = exit.GetProperty("room_x").GetInt32();
                    int ry = exit.GetProperty("room_y").GetInt32();

                    _exitWarp = WarpTile.ToOverworld(zone, spawnOut, rx, ry);
                }

                // ── NPCs / Signs ──
                if (root.TryGetProperty("npcs", out var npcs))
                {
                    foreach (var n in npcs.EnumerateArray())
                    {
                        string name = n.GetProperty("name").GetString();
                        string text = n.GetProperty("text").GetString();
                        var pos = new Vector2(
                            n.GetProperty("x").GetSingle(),
                            n.GetProperty("y").GetSingle());

                        var npc = new NPC(name.ToLower(), name, pos,
                            new List<DialogueBox.DialoguePage>
                            {
                                new() { Speaker = name, Text = text }
                            });
                        _interactables.Add(npc);
                    }
                }

                // ── Light sources (fireplaces, crystals) ──
                if (root.TryGetProperty("lights", out var lights))
                {
                    foreach (var l in lights.EnumerateArray())
                    {
                        float lx = l.GetProperty("x").GetSingle();
                        float ly = l.GetProperty("y").GetSingle();
                        float radius = l.TryGetProperty("radius", out var r) ? r.GetSingle() : 60f;
                        _lightSources.Add(new PulseEffect(new Vector2(lx, ly), radius, 999f));
                    }
                }
            }
            catch { /* silently skip bad JSON */ }

            // Wire projectile spawning
            _player.OnSpawnProjectile = (proj) => _projectiles.Add(proj);

            // Add furniture collision walls and interactable furniture
            AddFurnitureWalls();
            AddFurnitureInteractables();

            // Prevent instant exit after entering
            _warpCooldown = 0.5f;
        }

        private void AddFurnitureInteractables()
        {
            if (_interiorId == "hero_house")
            {
                _interactables.Add(MakeInvisible(new Vector2(62, 70),
                    "The fire crackles warmly. It feels like home."));
                _interactables.Add(MakeInvisible(new Vector2(316, 190),
                    "Dusty tomes line the shelves. Tales of ancient heroes and faraway lands."));
                _interactables.Add(new TreasureChest(
                    new Vector2(50, 240), "hero_house_chest", 15, "health_potion", "Health Potion"));
                _interactables.Add(MakeInvisible(new Vector2(280, 80),
                    "Your bed. Looks inviting after a long adventure."));
                _interactables.Add(MakeInvisible(new Vector2(186, 52),
                    "A little green friend. It seems to be thriving."));
            }
            else if (_interiorId == "blacksmith_house")
            {
                _interactables.Add(MakeInvisible(new Vector2(152, 130),
                    "A well-used anvil. You can almost hear the ring of hammers."));
                _interactables.Add(MakeInvisible(new Vector2(130, 70),
                    "The forge burns hot. Perfect for shaping metal."));
                _interactables.Add(MakeInvisible(new Vector2(270, 100),
                    "Fine weapons on display. The blacksmith's best work."));
                _interactables.Add(MakeInvisible(new Vector2(56, 210),
                    "A barrel of supplies. Coal, iron ore, and a few scraps."));
                _interactables.Add(new TreasureChest(
                    new Vector2(260, 160), "blacksmith_chest", 25, "bomb", "Bombs x5"));
            }

            // Make light sources interactable too
            foreach (var light in _lightSources)
            {
                _interactables.Add(MakeInvisible(light.Origin - new Vector2(14, 14),
                    "A soft, warm glow emanates from here. It feels magical."));
            }
        }

        /// <summary>
        /// Creates an invisible interactable (no sign sprite drawn, just E prompt + dialogue).
        /// </summary>
        private static SignPost MakeInvisible(Vector2 pos, string text)
        {
            var sign = new SignPost(pos, text) { Visible = false };
            return sign;
        }

        private void AddFurnitureWalls()
        {
            if (_interiorId == "hero_house")
            {
                _walls.Add(new SolidRect(260, 40, 72, 80));   // Bed
                _walls.Add(new SolidRect(46, 32, 64, 52));     // Fireplace
                _walls.Add(new SolidRect(50, 150, 56, 44));    // Table
                _walls.Add(new SolidRect(310, 150, 32, 80));   // Bookshelf
                _walls.Add(new SolidRect(50, 240, 36, 24));    // Chest
                _walls.Add(new SolidRect(180, 44, 24, 28));    // Potted plant
            }
            else if (_interiorId == "blacksmith_house")
            {
                _walls.Add(new SolidRect(146, 120, 40, 24));   // Anvil
                _walls.Add(new SolidRect(100, 40, 80, 40));    // Forge
                _walls.Add(new SolidRect(260, 60, 40, 80));    // Weapon rack
                _walls.Add(new SolidRect(50, 200, 28, 32));    // Barrel
            }
        }

        private int[,] ParseTileLayer(JsonElement arr, int rows, int cols)
        {
            int[,] data = new int[rows, cols];
            int r = 0;
            foreach (var row in arr.EnumerateArray())
            {
                int c = 0;
                foreach (var val in row.EnumerateArray())
                {
                    if (r < rows && c < cols)
                        data[r, c] = val.GetInt32();
                    c++;
                }
                r++;
            }
            return data;
        }

        public override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            KeyboardState kb = Keyboard.GetState();

            // ── Warp cooldown ──
            if (_warpCooldown > 0f) _warpCooldown -= dt;

            // ── Dialogue ──
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

            // ── Interaction (E key) ──
            if (kb.IsKeyDown(Keys.E) && _prevKb.IsKeyUp(Keys.E))
            {
                foreach (var interactable in _interactables)
                {
                    if (interactable.IsPlayerInRange(_player.BoundingBox))
                    {
                        var pages = interactable.GetDialogue();
                        if (pages != null && pages.Count > 0)
                        {
                            _dialogueBox.Open(pages);
                            _activeInteractable = interactable;
                            _prevKb = kb;
                            return;
                        }
                    }
                }
            }

            // ── Inventory (I key) ──
            if (kb.IsKeyDown(Keys.I) && _prevKb.IsKeyUp(Keys.I))
            {
                GameRef.ChangeState(GameState.Inventory);
                _prevKb = kb;
                return;
            }

            // ── Player ──
            _player.Update(gameTime);
            CollisionSystem.MoveAndResolve(_player, _walls, dt);

            // ── Clamp player to interior bounds ──
            if (_player.Position.X < 0) _player.Position = new Vector2(0, _player.Position.Y);
            if (_player.Position.Y < 0) _player.Position = new Vector2(_player.Position.X, 0);
            if (_player.BoundingBox.Right > _mapPixelWidth)
                _player.Position = new Vector2(_mapPixelWidth - _player.HitboxWidth - _player.HitboxOffset.X, _player.Position.Y);
            if (_player.BoundingBox.Bottom > _mapPixelHeight)
                _player.Position = new Vector2(_player.Position.X, _mapPixelHeight - _player.HitboxHeight - _player.HitboxOffset.Y);

            // ── Check exit warp ──
            if (_warpCooldown <= 0f && _exitWarp != null && _exitWarp.IsPlayerOn(_player.BoundingBox))
            {
                GameRef.EnterOverworld(_exitWarp.SpawnPosition,
                    _exitWarp.TargetRoomX, _exitWarp.TargetRoomY);
                _prevKb = kb;
                return;
            }

            // ── Interactable proximity check ──
            foreach (var interactable in _interactables)
            {
                interactable.IsPlayerInRange(_player.BoundingBox);
                interactable.Update(gameTime);
            }

            // ── Projectiles ──
            for (int i = _projectiles.Count - 1; i >= 0; i--)
            {
                _projectiles[i].Update(gameTime, _walls);
                if (!_projectiles[i].IsAlive)
                    _projectiles.RemoveAt(i);
            }

            // ── Light sources ──
            foreach (var light in _lightSources)
                light.Update(gameTime);

            // ── Camera follow ──
            if (_cameraFollow)
            {
                // Center camera on player, clamped to map bounds
                float targetX = _player.Position.X + _player.HitboxWidth / 2f - Game1.ScreenWidth / 2f;
                float targetY = _player.Position.Y + _player.HitboxHeight / 2f - Game1.ScreenHeight / 2f;
                targetX = MathHelper.Clamp(targetX, 0, Math.Max(0, _mapPixelWidth - Game1.ScreenWidth));
                targetY = MathHelper.Clamp(targetY, 0, Math.Max(0, _mapPixelHeight - Game1.ScreenHeight));
                _cameraOffset = Vector2.Lerp(_cameraOffset, new Vector2(targetX, targetY), 0.1f);
            }
            else
            {
                // Locked camera — center the map on screen
                // Negative offset shifts the view right/down to center a small room
                _cameraOffset = new Vector2(
                    (_mapPixelWidth - Game1.ScreenWidth) / 2f,
                    (_mapPixelHeight - Game1.ScreenHeight) / 2f);
            }

            _prevKb = kb;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            // End the default batch, restart with camera transform
            spriteBatch.End();

            Matrix transform = Matrix.CreateTranslation(
                -(int)_cameraOffset.X, -(int)_cameraOffset.Y, 0f);

            // ── World-space ──
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.PointClamp, null, null, null, transform);

            // Dark ambient for interiors — covers full visible screen area
            Texture2D px = Game1.PixelTexture;
            int bgX = Math.Min(0, (int)_cameraOffset.X);
            int bgY = Math.Min(0, (int)_cameraOffset.Y);
            int bgW = Math.Max(Game1.ScreenWidth, _mapPixelWidth + Math.Abs((int)_cameraOffset.X));
            int bgH = Math.Max(Game1.ScreenHeight, _mapPixelHeight + Math.Abs((int)_cameraOffset.Y));
            spriteBatch.Draw(px, new Rectangle(bgX, bgY, bgW, bgH),
                new Color(15, 12, 10));

            // Floor
            _floorMap?.Draw(spriteBatch);

            // Decor layer
            _decorMap?.Draw(spriteBatch);

            // Walls — stone/wood textured
            foreach (var wall in _walls)
            {
                // Base wall color
                spriteBatch.Draw(px, wall.Bounds, new Color(60, 45, 35));
                // Stone brick pattern
                for (int wx = wall.Bounds.X; wx < wall.Bounds.Right; wx += 20)
                {
                    for (int wy = wall.Bounds.Y; wy < wall.Bounds.Bottom; wy += 12)
                    {
                        int offset = ((wy / 12) % 2 == 0) ? 0 : 10;
                        spriteBatch.Draw(px, new Rectangle(wx + offset, wy, 18, 10),
                            new Color(70, 52, 40));
                        spriteBatch.Draw(px, new Rectangle(wx + offset + 1, wy + 1, 16, 1),
                            new Color(80, 62, 48));
                    }
                }
            }

            // ── Procedural furniture ──
            DrawFurniture(spriteBatch, px);

            // Light source glow
            foreach (var light in _lightSources)
                light.Draw(spriteBatch);

            // Interactables
            foreach (var interactable in _interactables)
                interactable.Draw(spriteBatch);

            // Projectiles
            foreach (var proj in _projectiles)
                proj.Draw(spriteBatch);

            // Player
            _player.Draw(spriteBatch);

            spriteBatch.End();

            // ── Screen-space HUD ──
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.PointClamp, null, null, null, null);

            // Room name
            PixelFont.DrawCentered(spriteBatch, _interiorId.Replace("_", " ").ToUpper(),
                8, Color.Gold * 0.6f, 2);

            // Dialogue
            _dialogueBox.Draw(spriteBatch);
        }
        // ──────────────────────────────────────────────
        //  Procedural Interior Furniture
        // ──────────────────────────────────────────────

        private void DrawFurniture(SpriteBatch sb, Texture2D px)
        {
            if (_interiorId == "hero_house")
                DrawHeroHouseFurniture(sb, px);
            else if (_interiorId == "blacksmith_house")
                DrawBlacksmithFurniture(sb, px);
        }

        private void DrawHeroHouseFurniture(SpriteBatch sb, Texture2D px)
        {
            // ── Rug (center of room) ──
            int rugX = 120, rugY = 130, rugW = 140, rugH = 100;
            sb.Draw(px, new Rectangle(rugX - 2, rugY - 2, rugW + 4, rugH + 4), new Color(120, 40, 30));
            sb.Draw(px, new Rectangle(rugX, rugY, rugW, rugH), new Color(150, 55, 40));
            // Rug pattern — diamond border
            sb.Draw(px, new Rectangle(rugX + 8, rugY + 8, rugW - 16, rugH - 16), new Color(160, 70, 50));
            sb.Draw(px, new Rectangle(rugX + 16, rugY + 16, rugW - 32, rugH - 32), new Color(140, 50, 35));
            // Center medallion
            sb.Draw(px, new Rectangle(rugX + rugW / 2 - 12, rugY + rugH / 2 - 8, 24, 16), new Color(180, 140, 50));
            sb.Draw(px, new Rectangle(rugX + rugW / 2 - 8, rugY + rugH / 2 - 4, 16, 8), new Color(200, 160, 60));

            // ── Bed (top-right corner) ──
            int bedX = 260, bedY = 40;
            // Frame
            sb.Draw(px, new Rectangle(bedX, bedY, 72, 80), new Color(90, 60, 35));
            // Mattress
            sb.Draw(px, new Rectangle(bedX + 4, bedY + 4, 64, 72), new Color(220, 210, 190));
            // Pillow
            sb.Draw(px, new Rectangle(bedX + 10, bedY + 8, 52, 18), Color.White);
            sb.Draw(px, new Rectangle(bedX + 12, bedY + 10, 22, 14), new Color(240, 240, 250));
            sb.Draw(px, new Rectangle(bedX + 36, bedY + 10, 22, 14), new Color(240, 240, 250));
            // Blanket
            sb.Draw(px, new Rectangle(bedX + 6, bedY + 30, 60, 42), new Color(70, 100, 160));
            sb.Draw(px, new Rectangle(bedX + 8, bedY + 32, 56, 2), new Color(90, 120, 180));
            // Blanket fold line
            sb.Draw(px, new Rectangle(bedX + 6, bedY + 28, 60, 3), new Color(200, 195, 180));

            // ── Fireplace (top-left) ──
            int fpX = 52, fpY = 36;
            // Hearth base
            sb.Draw(px, new Rectangle(fpX - 6, fpY, 60, 48), new Color(80, 55, 40));
            sb.Draw(px, new Rectangle(fpX - 4, fpY + 2, 56, 44), new Color(40, 25, 18));
            // Fire opening
            sb.Draw(px, new Rectangle(fpX + 4, fpY + 12, 40, 32), new Color(20, 12, 8));
            // Fire glow
            // Animated fire flicker (unused var, just for future use)

            sb.Draw(px, new Rectangle(fpX + 8, fpY + 24, 32, 18), new Color(200, 80, 20) * 0.6f);
            sb.Draw(px, new Rectangle(fpX + 14, fpY + 20, 20, 22), new Color(240, 140, 30) * 0.7f);
            sb.Draw(px, new Rectangle(fpX + 18, fpY + 18, 12, 20), new Color(255, 200, 50) * 0.5f);
            // Mantle
            sb.Draw(px, new Rectangle(fpX - 8, fpY - 4, 64, 6), new Color(100, 70, 45));

            // ── Table (center-left) ──
            int tblX = 50, tblY = 150;
            // Table top
            sb.Draw(px, new Rectangle(tblX, tblY, 56, 36), new Color(110, 75, 45));
            sb.Draw(px, new Rectangle(tblX + 2, tblY + 2, 52, 32), new Color(130, 90, 55));
            // Table legs
            sb.Draw(px, new Rectangle(tblX + 2, tblY + 34, 4, 8), new Color(90, 60, 35));
            sb.Draw(px, new Rectangle(tblX + 50, tblY + 34, 4, 8), new Color(90, 60, 35));
            // Plate on table
            sb.Draw(px, new Rectangle(tblX + 18, tblY + 8, 18, 14), new Color(200, 195, 185));
            sb.Draw(px, new Rectangle(tblX + 20, tblY + 10, 14, 10), new Color(180, 175, 165));
            // Apple on plate
            sb.Draw(px, new Rectangle(tblX + 24, tblY + 12, 6, 6), new Color(200, 40, 30));
            sb.Draw(px, new Rectangle(tblX + 26, tblY + 10, 2, 3), new Color(60, 120, 40));

            // ── Chair (below table) ──
            int chX = tblX + 14, chY = tblY + 42;
            sb.Draw(px, new Rectangle(chX, chY, 24, 20), new Color(100, 65, 40));
            sb.Draw(px, new Rectangle(chX + 2, chY + 2, 20, 12), new Color(120, 80, 50));
            // Chair back
            sb.Draw(px, new Rectangle(chX + 2, chY - 8, 20, 10), new Color(100, 65, 40));

            // ── Bookshelf (right wall) ──
            int bsX = 310, bsY = 150;
            // Frame
            sb.Draw(px, new Rectangle(bsX, bsY, 32, 80), new Color(90, 58, 35));
            // Shelves
            for (int sy = 0; sy < 4; sy++)
            {
                int shelfY = bsY + 4 + sy * 20;
                sb.Draw(px, new Rectangle(bsX + 2, shelfY, 28, 18), new Color(70, 45, 28));
                // Books (varied colors)
                Color[] bookColors = {
                    new Color(140, 40, 40), new Color(40, 80, 140),
                    new Color(40, 120, 60), new Color(160, 120, 40)
                };
                int bx = bsX + 4;
                for (int b = 0; b < 4; b++)
                {
                    int bw = 4 + (b % 2) * 2;
                    sb.Draw(px, new Rectangle(bx, shelfY + 2, bw, 14), bookColors[(sy + b) % 4]);
                    bx += bw + 1;
                }
            }

            // ── Chest (bottom-left) ──
            int cstX = 50, cstY = 240;
            sb.Draw(px, new Rectangle(cstX, cstY, 36, 24), new Color(110, 75, 40));
            sb.Draw(px, new Rectangle(cstX + 2, cstY + 2, 32, 20), new Color(130, 90, 50));
            // Metal band
            sb.Draw(px, new Rectangle(cstX, cstY + 10, 36, 3), new Color(160, 150, 130));
            // Lock
            sb.Draw(px, new Rectangle(cstX + 15, cstY + 8, 6, 8), new Color(200, 180, 60));

            // ── Potted plant (top, between bed and fireplace) ──
            int ptX = 186, ptY = 44;
            // Pot
            sb.Draw(px, new Rectangle(ptX, ptY + 12, 20, 16), new Color(160, 90, 50));
            sb.Draw(px, new Rectangle(ptX + 2, ptY + 10, 16, 4), new Color(140, 75, 40));
            // Soil
            sb.Draw(px, new Rectangle(ptX + 3, ptY + 11, 14, 2), new Color(80, 50, 30));
            // Plant leaves
            sb.Draw(px, new Rectangle(ptX + 6, ptY, 8, 12), new Color(50, 140, 50));
            sb.Draw(px, new Rectangle(ptX + 2, ptY + 2, 6, 8), new Color(60, 150, 60));
            sb.Draw(px, new Rectangle(ptX + 12, ptY + 2, 6, 8), new Color(60, 150, 60));

            // ── Window on right wall (decorative) ──
            int wnX = 340, wnY = 80;
            sb.Draw(px, new Rectangle(wnX, wnY, 6, 40), new Color(80, 55, 35));
            sb.Draw(px, new Rectangle(wnX + 1, wnY + 1, 4, 38), new Color(100, 140, 180));
            // Cross pane
            sb.Draw(px, new Rectangle(wnX, wnY + 19, 6, 2), new Color(80, 55, 35));
            // Light beam from window
            sb.Draw(px, new Rectangle(wnX - 30, wnY + 5, 30, 30), new Color(255, 240, 200) * 0.06f);

            // ── Small rug by door ──
            sb.Draw(px, new Rectangle(164, 270, 56, 20), new Color(130, 60, 40));
            sb.Draw(px, new Rectangle(166, 272, 52, 16), new Color(150, 75, 50));
            sb.Draw(px, new Rectangle(172, 276, 40, 8), new Color(170, 90, 55));
        }

        private void DrawBlacksmithFurniture(SpriteBatch sb, Texture2D px)
        {
            // ── Anvil (center) ──
            int aX = 150, aY = 120;
            sb.Draw(px, new Rectangle(aX, aY + 8, 32, 16), new Color(80, 80, 90));
            sb.Draw(px, new Rectangle(aX - 4, aY, 40, 10), new Color(100, 100, 110));
            sb.Draw(px, new Rectangle(aX + 12, aY - 6, 8, 8), new Color(110, 110, 120));

            // ── Forge (top wall) ──
            int fgX = 100, fgY = 40;
            sb.Draw(px, new Rectangle(fgX, fgY, 80, 40), new Color(60, 40, 30));
            sb.Draw(px, new Rectangle(fgX + 10, fgY + 10, 60, 25), new Color(30, 15, 10));
            sb.Draw(px, new Rectangle(fgX + 18, fgY + 16, 44, 16), new Color(200, 80, 20) * 0.6f);
            sb.Draw(px, new Rectangle(fgX + 24, fgY + 14, 32, 18), new Color(240, 140, 30) * 0.5f);

            // ── Weapon rack (right wall) ──
            int wrX = 260, wrY = 60;
            sb.Draw(px, new Rectangle(wrX, wrY, 40, 80), new Color(80, 55, 35));
            // Swords
            sb.Draw(px, new Rectangle(wrX + 6, wrY + 8, 4, 50), new Color(160, 160, 170));
            sb.Draw(px, new Rectangle(wrX + 16, wrY + 12, 4, 46), new Color(160, 160, 170));
            sb.Draw(px, new Rectangle(wrX + 26, wrY + 6, 4, 52), new Color(160, 160, 170));
            // Guards
            sb.Draw(px, new Rectangle(wrX + 2, wrY + 50, 12, 3), new Color(140, 120, 50));
            sb.Draw(px, new Rectangle(wrX + 12, wrY + 52, 12, 3), new Color(140, 120, 50));
            sb.Draw(px, new Rectangle(wrX + 22, wrY + 48, 12, 3), new Color(140, 120, 50));

            // ── Barrel (bottom-left) ──
            int brX = 50, brY = 200;
            sb.Draw(px, new Rectangle(brX, brY, 28, 32), new Color(110, 75, 40));
            sb.Draw(px, new Rectangle(brX + 2, brY + 2, 24, 28), new Color(130, 90, 55));
            sb.Draw(px, new Rectangle(brX, brY + 8, 28, 3), new Color(80, 80, 90));
            sb.Draw(px, new Rectangle(brX, brY + 20, 28, 3), new Color(80, 80, 90));
        }
    }
}
