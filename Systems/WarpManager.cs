using Microsoft.Xna.Framework;
using System;

namespace TheGame.Systems
{
    /// <summary>
    /// Coordinates transitions between the 5x5 Overworld and interior sub-maps (houses, caves),
    /// capturing the player's last position before they entered the interior.
    /// </summary>
    public static class WarpManager
    {
        // Maintains the player's origin position in the overworld to spawn them back correctly.
        private static Vector2 _lastOverworldPosition;
        private static string _currentInteriorId;

        /// <summary>
        /// Event fired when a warp is requested. Hooked up to the GameState machine.
        /// Action parameters: (interiorId, spawnPositionTarget)
        /// </summary>
        public static event Action<string, Vector2> OnWarpToInteriorRequested;

        /// <summary>
        /// Event fired when wrapping back to the Overworld. Hooked up to the GameState machine.
        /// Action parameters: (spawnPositionTarget)
        /// </summary>
        public static event Action<Vector2> OnWarpToOverworldRequested;

        /// <summary>
        /// Warps the player from the Overworld into a specific Interior map.
        /// </summary>
        /// <param name="interiorId">The JSON key for the interior room.</param>
        /// <param name="playerOverworldPos">Where the player stood before warping.</param>
        /// <param name="interiorSpawnPos">Where the player will spawn inside the room.</param>
        public static void WarpToInterior(string interiorId, Vector2 playerOverworldPos, Vector2 interiorSpawnPos)
        {
            _lastOverworldPosition = playerOverworldPos;
            _currentInteriorId = interiorId;

            OnWarpToInteriorRequested?.Invoke(interiorId, interiorSpawnPos);
        }

        /// <summary>
        /// Warps the player back to the Overworld where they originally entered from.
        /// </summary>
        /// <param name="offsetSpawn">Offset applied to prevent getting stuck in the door trigger.</param>
        public static void WarpToOverworld(Vector2 offsetSpawn)
        {
            _currentInteriorId = null;
            Vector2 returnPos = _lastOverworldPosition + offsetSpawn;

            OnWarpToOverworldRequested?.Invoke(returnPos);
        }
    }
}
