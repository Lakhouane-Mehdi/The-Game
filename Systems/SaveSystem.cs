// ============================================================================
// SaveSystem.cs — Save/Load Game State
// Author: Mehdi Lakhouane
// Description: Serializes and deserializes game state to/from a JSON file.
//              Persists: player position, health, inventory, coins, quest flags,
//              world state flags (broken pots, opened chests, defeated bosses).
// ============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using TheGame.Core;
using TheGame.Entities;
using TheGame.Entities.Items;

namespace TheGame.Systems
{
    public static class SaveSystem
    {
        private static readonly string SaveDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TheFadingResonance");
        private static readonly string SavePath = Path.Combine(SaveDir, "save.json");

        public class SaveData
        {
            public float PlayerX { get; set; }
            public float PlayerY { get; set; }
            public int PlayerHealth { get; set; }
            public int Coins { get; set; }
            public List<string> QuestFlags { get; set; } = new();
            public Dictionary<string, bool> WorldFlags { get; set; } = new();
            public List<SavedItem> Items { get; set; } = new();
            public string EquippedItemId { get; set; }
            public int CurrentRoomX { get; set; }
            public int CurrentRoomY { get; set; }
        }

        public class SavedItem
        {
            public string Id { get; set; }
            public int Quantity { get; set; }
        }

        /// <summary>
        /// Save the current game state to disk.
        /// </summary>
        public static bool Save(Game1 game, Player player, int roomX, int roomY)
        {
            try
            {
                var data = new SaveData
                {
                    PlayerX = player.Position.X,
                    PlayerY = player.Position.Y,
                    PlayerHealth = player.CurrentHealth,
                    Coins = player.Inventory.Coins,
                    CurrentRoomX = roomX,
                    CurrentRoomY = roomY
                };

                // Quest flags
                foreach (var flag in game.QuestFlags)
                    data.QuestFlags.Add(flag);

                // World state flags — use reflection-free approach
                // We expose GetAllFlags from WorldStateManager
                var allFlags = WorldStateManager.GetAllFlags();
                foreach (var kv in allFlags)
                    data.WorldFlags[kv.Key] = kv.Value;

                // Inventory items
                var items = player.Inventory.GetAllItems();
                foreach (var item in items)
                {
                    data.Items.Add(new SavedItem
                    {
                        Id = item.Id,
                        Quantity = player.Inventory.GetQuantity(item.Id)
                    });
                }

                // Equipped item
                if (player.Inventory.EquippedItem != null)
                    data.EquippedItemId = player.Inventory.EquippedItem.Id;

                // Write to disk
                if (!Directory.Exists(SaveDir))
                    Directory.CreateDirectory(SaveDir);

                string json = JsonSerializer.Serialize(data, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(SavePath, json);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Load game state from disk. Returns null if no save exists.
        /// </summary>
        public static SaveData Load()
        {
            try
            {
                if (!File.Exists(SavePath))
                    return null;

                string json = File.ReadAllText(SavePath);
                return JsonSerializer.Deserialize<SaveData>(json);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Apply loaded save data to the game state.
        /// </summary>
        public static void ApplySave(Game1 game, Player player, SaveData data)
        {
            if (data == null) return;

            player.Position = new Vector2(data.PlayerX, data.PlayerY);
            player.SetHealth(data.PlayerHealth);
            player.Inventory.Coins = data.Coins;

            // Restore quest flags
            game.QuestFlags.Clear();
            foreach (var flag in data.QuestFlags)
                game.QuestFlags.Add(flag);

            // Restore world state
            WorldStateManager.Clear();
            foreach (var kv in data.WorldFlags)
                WorldStateManager.SetFlag(kv.Key, kv.Value);

            // Restore inventory
            foreach (var item in data.Items)
            {
                for (int i = 0; i < item.Quantity; i++)
                    player.Inventory.AddItem(item.Id);
            }

            // Restore equipped item
            if (!string.IsNullOrEmpty(data.EquippedItemId))
                player.Inventory.EquipById(data.EquippedItemId);
        }

        /// <summary>
        /// Check if a save file exists.
        /// </summary>
        public static bool HasSave() => File.Exists(SavePath);

        /// <summary>
        /// Delete save file.
        /// </summary>
        public static void DeleteSave()
        {
            if (File.Exists(SavePath))
                File.Delete(SavePath);
        }
    }
}
