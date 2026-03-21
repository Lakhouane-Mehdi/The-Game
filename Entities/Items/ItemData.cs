// ============================================================================
// ItemData.cs — Item Definition (matches items.json schema)
// Author: Mehdi Lakhouane
// Description: Data class representing one item entry from the JSON file.
//              Used by the Inventory to create concrete item instances.
// ============================================================================

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
        AOE                 // bomb — placed, detonates after fuse time
    }

    /// <summary>
    /// Pure data — one entry from items.json.
    /// </summary>
    public class ItemData
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int SpriteIndex { get; set; }
        public string SpritePath { get; set; }
        public int AmmoCount { get; set; }     // -1 = unlimited
        public EffectType Effect { get; set; }
        public int Damage { get; set; }
        public float Range { get; set; }
        public float Cooldown { get; set; }
        public float Speed { get; set; }       // for projectiles
        public float FuseTime { get; set; }    // for bombs
        public string Description { get; set; }
    }
}
