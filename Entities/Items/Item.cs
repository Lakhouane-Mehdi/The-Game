// ============================================================================
// ItemData.cs — Item Definition (matches items.json schema)
// Author: Mehdi Lakhouane
// Description: Data class representing one item entry from the JSON file.
//              Used by the Inventory to create concrete item instances.
// ============================================================================

using System;
using Microsoft.Xna.Framework;
using TheGame.Entities;

namespace TheGame.Entities.Items
{
    /// <summary>
    /// The type of effect an item produces when used.
    /// </summary>
    public enum EffectType
    {
        Melee,              // sword swing — hitbox in front of player
        Projectile,         // arrow / fireball — flies forward, dies on hit or max range
        ProjectileReturn,   // boomerang — flies out, then returns to player
        AOE,                 // bomb — placed, detonates after fuse time
        Key,                // Just a key
        Resource            // collectible resource (fruit, stone, etc.)
    }

    /// <summary>
    /// The category of item.
    /// </summary>
    public enum ItemType
    {
        Weapon,
        Consumable,
        KeyItem,
        Resource
    }

    /// <summary>
    /// Pure data — one entry from items.json.
    /// </summary>
    public class Item
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string SpritePath { get; set; }
        public ItemType Type { get; set; }
        public EffectType EffectType { get; set; }
        public System.Action<Player> Effect { get; set; }
        
        // Item properties from JSON
        public int Damage { get; set; }
        public float Range { get; set; }
        public float Cooldown { get; set; }
        public float Speed { get; set; }
        public float FuseTime { get; set; }

        /// <summary>
        /// Optional source rectangle for sprite sheet items. Null = use full texture.
        /// </summary>
        public Rectangle? SourceRect { get; set; }
    }
}
