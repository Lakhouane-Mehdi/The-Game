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
        // ── Owned items (Item -> Quantity) ──
        private readonly Dictionary<Item, int> _items = new();
        
        // ── List for stable UI ordering ──
        private readonly List<Item> _itemList = new();

        // ── Currently equipped item index in _itemList ──
        private int _equippedIndex = -1;

        /// <summary>
        /// The currently equipped item, or null if nothing is equipped.
        /// </summary>
        public Item EquippedItem =>
            _equippedIndex >= 0 && _equippedIndex < _itemList.Count
                ? _itemList[_equippedIndex]
                : null;

        public int EquippedIndex => _equippedIndex;

        public void SetEquippedIndex(int index)
        {
            if (index >= 0 && index < _itemList.Count)
                _equippedIndex = index;
        }

        /// <summary>
        /// All items in the inventory (read-only view).
        /// </summary>
        public IReadOnlyList<Item> Items => _itemList;

        // ── Item sprites (loaded per item) ──
        private readonly Dictionary<string, Texture2D> _itemSprites = new();

        // ── Master item catalog (loaded from JSON) ──
        private static readonly Dictionary<string, Item> _catalog = new();

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
                var itemTypeStr = elem.TryGetProperty("type", out var t) ? t.GetString() : "Weapon";
                var item = new Item
                {
                    Id = elem.GetProperty("id").GetString(),
                    Name = elem.GetProperty("name").GetString(),
                    SpritePath = elem.GetProperty("spritePath").GetString(),
                    Type = ParseItemType(itemTypeStr),
                    EffectType = ParseEffect(elem.GetProperty("effectType").GetString()),
                    Damage = elem.GetProperty("damage").GetInt32(),
                    Range = elem.GetProperty("range").GetSingle(),
                    Cooldown = elem.GetProperty("cooldown").GetSingle(),
                    Speed = elem.TryGetProperty("speed", out var sp) ? sp.GetSingle() : 0f,
                    FuseTime = elem.TryGetProperty("fuseTime", out var ft) ? ft.GetSingle() : 0f,
                    Description = elem.GetProperty("description").GetString(),
                };

                // Optional source rectangle for sprite sheet items
                if (elem.TryGetProperty("srcX", out var sx))
                {
                    item.SourceRect = new Rectangle(
                        sx.GetInt32(),
                        elem.GetProperty("srcY").GetInt32(),
                        elem.GetProperty("srcW").GetInt32(),
                        elem.GetProperty("srcH").GetInt32());
                }

                _catalog[item.Id] = item;
            }
        }

        private static ItemType ParseItemType(string s) => s switch
        {
            "weapon" => ItemType.Weapon,
            "consumable" => ItemType.Consumable,
            "key" => ItemType.KeyItem,
            "resource" => ItemType.Resource,
            _ => ItemType.Weapon
        };

        private static EffectType ParseEffect(string s) => s switch
        {
            "melee" => EffectType.Melee,
            "projectile" => EffectType.Projectile,
            "projectile_return" => EffectType.ProjectileReturn,
            "aoe" => EffectType.AOE,
            "key" => EffectType.Key,
            "resource" => EffectType.Resource,
            _ => EffectType.Melee
        };

        // ──────────────────────────────────────────────
        //  Adding / Removing Items
        // ──────────────────────────────────────────────

        /// <summary>
        /// Grants the player an item by catalog ID.
        /// </summary>
        /// <summary>
        /// Checks if the player has a specific item.
        /// </summary>
        public bool HasItem(string itemId)
        {
            foreach (var i in _itemList)
                if (i.Id == itemId) return true;
            return false;
        }

        public void AddItem(string itemId, int amount = 1)
        {
            if (!_catalog.ContainsKey(itemId)) return;

            var item = _catalog[itemId];
            
            if (_items.ContainsKey(item))
            {
                _items[item] += amount;
            }
            else
            {
                _items[item] = amount;
                _itemList.Add(item);

                // Load sprite
                if (AssetLoader.Exists(item.SpritePath))
                    _itemSprites[item.Id] = AssetLoader.LoadTexture(item.SpritePath);
            }

            // Auto-equip if it's the first item
            if (_equippedIndex < 0)
                _equippedIndex = 0;
        }

        /// <summary>
        /// Cycle to the next item in the inventory.
        /// </summary>
        public void CycleNext()
        {
            if (_itemList.Count == 0) return;
            _equippedIndex = (_equippedIndex + 1) % _itemList.Count;
        }

        /// <summary>
        /// Cycle to the previous item.
        /// </summary>
        public void CyclePrev()
        {
            if (_itemList.Count == 0) return;
            _equippedIndex--;
            if (_equippedIndex < 0) _equippedIndex = _itemList.Count - 1;
        }

        // ──────────────────────────────────────────────
        //  Ammo
        // ──────────────────────────────────────────────

        /// <summary>
        /// Returns quantity of an item.
        /// </summary>
        public int GetQuantity(Item item)
        {
            if (item != null && _items.ContainsKey(item)) return _items[item];
            return 0;
        }

        public int GetQuantityById(string itemId)
        {
            if (_catalog.ContainsKey(itemId))
                return GetQuantity(_catalog[itemId]);
            return 0;
        }

        /// <summary>
        /// Consumes 1 unit. Returns false if out.
        /// Melee and Key items are never consumed.
        /// </summary>
        public bool ConsumeItem(Item item)
        {
            if (item == null || !_items.ContainsKey(item)) return false;

            // Melee weapons and key items are unlimited — never consume
            if (item.EffectType == EffectType.Melee || item.EffectType == EffectType.Key)
                return true;

            if (_items[item] <= 0) return false;

            _items[item]--;
            return true;
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
            // Melee and key items are always usable
            if (item.EffectType == EffectType.Melee || item.EffectType == EffectType.Key)
                return true;
            if (GetQuantity(item) <= 0) return false;
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
                var dest = new Rectangle(x + 4, y + 4, slotSize - 8, slotSize - 8);
                if (item.SourceRect.HasValue)
                    sb.Draw(tex, dest, item.SourceRect.Value, Color.White);
                else
                    sb.Draw(tex, dest, Color.White);
            }

            // Ammo indicator
            if (item != null)
            {
                int quantity = GetQuantity(item);
                if (quantity > 1)
                {
                    PixelFont.DrawString(sb, quantity.ToString(), x + slotSize - 12, y + slotSize - 15, Color.White, 1);
                }
            }

            // "X" key hint
            sb.Draw(pixel, new Rectangle(x + slotSize / 2 - 4, y + slotSize + 8, 8, 3), Color.Gray * 0.5f);

            // ── Coin counter ──
            int coinX = x - 4;
            int coinY = y + slotSize + 16;
            // Draw coin icon (procedural)
            sb.Draw(pixel, new Rectangle(coinX, coinY, 10, 10), new Color(220, 180, 40));
            sb.Draw(pixel, new Rectangle(coinX + 1, coinY + 1, 8, 8), new Color(255, 215, 50));
            sb.Draw(pixel, new Rectangle(coinX + 3, coinY + 2, 4, 6), new Color(200, 160, 30));
            PixelFont.DrawString(sb, Coins.ToString(), coinX + 14, coinY + 1, Color.Gold, 1);
        }

        /// <summary>
        /// Exports the current inventory to Content/Data/inventory.json.
        /// </summary>
        public void SaveItems(string contentRoot)
        {
            var saveData = new List<object>();
            foreach (var kv in _items)
            {
                saveData.Add(new { id = kv.Key.Id, count = kv.Value });
            }

            string json = JsonSerializer.Serialize(saveData, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Path.Combine(contentRoot, "Data", "inventory.json"), json);
        }

        public Texture2D GetSprite(string itemId)
        {
            return _itemSprites.ContainsKey(itemId) ? _itemSprites[itemId] : null;
        }

        /// <summary>
        /// Gets the source rectangle for an item (for sprite sheet items), or null for full texture.
        /// </summary>
        public Rectangle? GetSourceRect(string itemId)
        {
            if (_catalog.ContainsKey(itemId))
                return _catalog[itemId].SourceRect;
            return null;
        }

        /// <summary>
        /// Gets an item definition from the catalog by ID.
        /// </summary>
        public static Item GetCatalogItem(string itemId)
        {
            return _catalog.ContainsKey(itemId) ? _catalog[itemId] : null;
        }

        /// <summary>
        /// Total coins the player has.
        /// </summary>
        public int Coins { get; set; }

        /// <summary>
        /// Returns all items in the inventory for serialization.
        /// </summary>
        public IReadOnlyList<Item> GetAllItems() => _itemList;

        /// <summary>
        /// Returns the quantity of an item by ID.
        /// </summary>
        public int GetQuantity(string itemId)
        {
            foreach (var kv in _items)
                if (kv.Key.Id == itemId)
                    return kv.Value;
            return 0;
        }

        /// <summary>
        /// Equip an item by its ID. Used for save/load.
        /// </summary>
        public void EquipById(string itemId)
        {
            for (int i = 0; i < _itemList.Count; i++)
            {
                if (_itemList[i].Id == itemId)
                {
                    _equippedIndex = i;
                    return;
                }
            }
        }
    }
}
