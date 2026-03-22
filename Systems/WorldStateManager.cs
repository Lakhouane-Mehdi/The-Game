using System.Collections.Generic;

namespace TheGame.Systems
{
    /// <summary>
    /// Governs the persistent state of the world based purely on string flags.
    /// This adheres to our zero-allocation, purely data-driven constraint.
    /// Example: FlagState["Room_2_2_Pot_5_Broken"] = true
    /// </summary>
    public static class WorldStateManager
    {
        // Dictionary mapping a unique string key to a boolean state.
        // E.g. broken pots, opened chests, defeated bosses.
        private static readonly Dictionary<string, bool> _flagState = new Dictionary<string, bool>();

        /// <summary>
        /// Retrieves the boolean state of a specific flag. Returns false if not found.
        /// </summary>
        public static bool GetFlag(string flagId)
        {
            if (_flagState.TryGetValue(flagId, out bool state))
            {
                return state;
            }
            return false;
        }

        /// <summary>
        /// Sets a persistent flag for the world state.
        /// </summary>
        public static void SetFlag(string flagId, bool state)
        {
            _flagState[flagId] = state;
        }

        /// <summary>
        /// Toggles the boolean state of a specific flag.
        /// </summary>
        public static void ToggleFlag(string flagId)
        {
            SetFlag(flagId, !GetFlag(flagId));
        }

        /// <summary>
        /// Resets the entire world state (useful for New Game or full resets).
        /// </summary>
        public static void Clear()
        {
            _flagState.Clear();
        }
    }
}
