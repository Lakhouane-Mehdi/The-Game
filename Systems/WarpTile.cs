// ============================================================================
// WarpTile.cs — Warp / Door Trigger
// Author: Mehdi Lakhouane
// Description: Defines a zone in the world that teleports the player to
//              another location — either a different room, an interior,
//              or back to the overworld. Triggers a fade-to-black transition.
// ============================================================================

using Microsoft.Xna.Framework;

namespace TheGame.Systems
{
    public enum WarpTarget
    {
        Interior,   // Load an interior map (house, cave)
        Overworld,  // Return to overworld at a specific position
        Dungeon     // Enter a dungeon state
    }

    public class WarpTile
    {
        /// <summary>World-space rectangle the player must step on.</summary>
        public Rectangle Zone { get; }

        /// <summary>Where the warp leads.</summary>
        public WarpTarget Target { get; }

        /// <summary>Interior JSON file to load (for Interior warps).</summary>
        public string InteriorId { get; }

        /// <summary>Position to place the player after warping.</summary>
        public Vector2 SpawnPosition { get; }

        /// <summary>Room coordinates for overworld warps.</summary>
        public int TargetRoomX { get; }
        public int TargetRoomY { get; }

        public WarpTile(Rectangle zone, WarpTarget target, string interiorId,
            Vector2 spawnPos, int targetRoomX = 0, int targetRoomY = 0)
        {
            Zone = zone;
            Target = target;
            InteriorId = interiorId;
            SpawnPosition = spawnPos;
            TargetRoomX = targetRoomX;
            TargetRoomY = targetRoomY;
        }

        /// <summary>Creates a warp into an interior.</summary>
        public static WarpTile ToInterior(Rectangle zone, string interiorId, Vector2 spawnInside)
        {
            return new WarpTile(zone, WarpTarget.Interior, interiorId, spawnInside);
        }

        /// <summary>Creates a warp back to the overworld.</summary>
        public static WarpTile ToOverworld(Rectangle zone, Vector2 spawnOutside,
            int roomX, int roomY)
        {
            return new WarpTile(zone, WarpTarget.Overworld, null, spawnOutside, roomX, roomY);
        }

        /// <summary>Creates a warp into a dungeon.</summary>
        public static WarpTile ToDungeon(Rectangle zone)
        {
            return new WarpTile(zone, WarpTarget.Dungeon, null, Vector2.Zero);
        }

        public bool IsPlayerOn(Rectangle playerBB)
        {
            return Zone.Intersects(playerBB);
        }
    }
}
