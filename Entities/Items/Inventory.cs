// ============================================================================
// Inventory.cs — Player Inventory System
// Author: Mehdi Lakhouane
// Description: Stores the player's items. Supports equipping an active item
//              (used with X key), cycling items, and ammo tracking.
//              Items are loaded from items.json at startup.
// ============================================================================

using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheGame.Core;
using TheGame.Utils;

namespace TheGame.Entities.Items
{
    public class Inventory
    {
        // ── Owned items ──
        private readonly List<ItemData> _items = new();

        // ── Currently equipped item index ──
        private int _equippedIndex = -1;

        /// <summary>
        /// The currently equipped item, or null if nothing is equipped.
        /// </summary>
        public ItemData EquippedItem =>
            _equippedIndex >= 0 && _equippedIndex < _items.Count
                ? _items[_equippedIndex]
                : null;

        /// <summary>
        /// All items in the inventory (read-only view).
        /// </summary>
        public IReadOnlyList<ItemData> Items => _items;

        // ── Item sprites (loaded per item) ──
        private readonly Dictionary<string, Texture2D> _itemSprites = new();

        // ── Ammo tracking (keyed by item ID) ──
        private readonly Dictionary<string, int> _ammo = new();

        // ── Master item catalog (loaded from JSON) ──
        private static readonly Dictionary<string, ItemData> _catalog = new();

        // ──────────────────────────────────────────────
        //  Loading from JSON
        // ──────────────────────────────────────────────

        /// <summary>
        /// Loads the item catalog from Content/Data/items.json.
        /// Call once at game startup.
        /// </summary>
        public static void LoadCatalog(string contentRoot)
        {
            string jsonPath = Path.Combine(contentRoot, "Data", "items.json");
            if (!File.Exists(jsonPath)) return;

            string json = File.ReadAllText(jsonPath);
            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement itemsArray = doc.RootElement.GetProperty("items");

            foreach (JsonElement elem in itemsArray.EnumerateArray())
            {
                var data = new ItemData
                {
                    Id = elem.GetProperty("id").GetString(),
                    Name = elem.GetProperty("name").GetString(),
                    SpriteIndex = elem.GetProperty("spriteIndex").GetInt32(),
                    SpritePath = elem.GetProperty("spritePath").GetString(),
                    AmmoCount = elem.GetProperty("ammoCount").GetInt32(),
                    Effect = ParseEffect(elem.GetProperty("effectType").GetString()),
                    Damage = elem.GetProperty("damage").GetInt32(),
                    Range = elem.GetProperty("range").GetSingle(),
                    Cooldown = elem.GetProperty("cooldown").GetSingle(),
                    Speed = elem.TryGetProperty("speed", out var sp) ? sp.GetSingle() : 0f,
                    FuseTime = elem.TryGetProperty("fuseTime", out var ft) ? ft.GetSingle() : 0f,
                    Description = elem.GetProperty("description").GetString(),
                };
                _catalog[data.Id] = data;
            }
        }

        private static EffectType ParseEffect(string s) => s switch
        {
            "melee" => EffectType.Melee,
            "projectile" => EffectType.Projectile,
            "projectile_return" => EffectType.ProjectileReturn,
            "aoe" => EffectType.AOE,
            _ => EffectType.Melee
        };

        // ──────────────────────────────────────────────
        //  Adding / Removing Items
        // ──────────────────────────────────────────────

        /// <summary>
        /// Grants the player an item by catalog ID.
        /// </summary>
        public void AddItem(string itemId)
        {
            if (!_catalog.ContainsKey(itemId)) return;

            // Don't add duplicates
            foreach (var i in _items)
                if (i.Id == itemId) return;

            var data = _catalog[itemId];
            _items.Add(data);

            // Set initial ammo
            if (data.AmmoCount > 0)
                _ammo[data.Id] = data.AmmoCount;

            // Load sprite
            if (AssetLoader.Exists(data.SpritePath))
                _itemSprites[data.Id] = AssetLoader.LoadTexture(data.SpritePath);

            // Auto-equip if it's the first item
            if (_equippedIndex < 0)
                _equippedIndex = 0;
        }

        /// <summary>
        /// Cycle to the next item in the inventory.
        /// </summary>
        public void CycleNext()
        {
            if (_items.Count == 0) return;
            _equippedIndex = (_equippedIndex + 1) % _items.Count;
        }

        /// <summary>
        /// Cycle to the previous item.
        /// </summary>
        public void CyclePrev()
        {
            if (_items.Count == 0) return;
            _equippedIndex--;
            if (_equippedIndex < 0) _equippedIndex = _items.Count - 1;
        }

        // ──────────────────────────────────────────────
        //  Ammo
        // ──────────────────────────────────────────────

        /// <summary>
        /// Returns remaining ammo for an item. -1 = unlimited.
        /// </summary>
        public int GetAmmo(string itemId)
        {
            if (_ammo.ContainsKey(itemId)) return _ammo[itemId];
            return -1; // unlimited
        }

        /// <summary>
        /// Consumes 1 ammo. Returns false if out of ammo.
        /// </summary>
        public bool ConsumeAmmo(string itemId)
        {
            if (!_ammo.ContainsKey(itemId)) return true; // unlimited
            if (_ammo[itemId] <= 0) return false;
            _ammo[itemId]--;
            return true;
        }

        /// <summary>
        /// Adds ammo to an item (e.g. picked up arrow bundle).
        /// </summary>
        public void AddAmmo(string itemId, int amount)
        {
            if (_ammo.ContainsKey(itemId))
                _ammo[itemId] += amount;
        }

        // ──────────────────────────────────────────────
        //  Can-Use check
        // ──────────────────────────────────────────────

        /// <summary>
        /// Returns true if the equipped item can be used right now.
        /// Checks ammo.
        /// </summary>
        public bool CanUseEquipped()
        {
            var item = EquippedItem;
            if (item == null) return false;
            if (item.AmmoCount > 0 && GetAmmo(item.Id) <= 0) return false;
            return true;
        }

        // ──────────────────────────────────────────────
        //  HUD Drawing
        // ──────────────────────────────────────────────

        /// <summary>
        /// Draws the item HUD slot in the top-right corner.
        /// Shows the equipped item sprite and ammo count.
        /// </summary>
        public void DrawHUD(SpriteBatch sb, Texture2D pixel)
        {
            int slotSize = 40;
            int margin = 10;
            int x = Game1.ScreenWidth - slotSize - margin;
            int y = margin;

            // Slot background
            sb.Draw(pixel, new Rectangle(x - 2, y - 2, slotSize + 4, slotSize + 4), Color.White * 0.6f);
            sb.Draw(pixel, new Rectangle(x, y, slotSize, slotSize), Color.Black * 0.8f);

            // Item sprite
            var item = EquippedItem;
            if (item != null && _itemSprites.ContainsKey(item.Id))
            {
                var tex = _itemSprites[item.Id];
                sb.Draw(tex, new Rectangle(x + 4, y + 4, slotSize - 8, slotSize - 8), Color.White);
            }

            // Ammo indicator (small bar below the slot)
            if (item != null && item.AmmoCount > 0)
            {
                int ammo = GetAmmo(item.Id);
                int maxAmmo = item.AmmoCount;
                float ratio = (float)ammo / maxAmmo;
                int barW = (int)(slotSize * ratio);
                sb.Draw(pixel, new Rectangle(x, y + slotSize + 2, barW, 4), Color.Cyan);
                sb.Draw(pixel, new Rectangle(x + barW, y + slotSize + 2, slotSize - barW, 4), Color.DarkSlateGray);
            }

            // "X" key hint
            sb.Draw(pixel, new Rectangle(x + slotSize / 2 - 4, y + slotSize + 8, 8, 3), Color.Gray * 0.5f);
        }

        /// <summary>
        /// Get the sprite for an item (for projectile rendering).
        /// </summary>
        public Texture2D GetSprite(string itemId)
        {
            return _itemSprites.ContainsKey(itemId) ? _itemSprites[itemId] : null;
        }
    }
}
