using System.Collections.Generic;
using System.Linq;

namespace TheGame.Systems
{
    /// <summary>
    /// Represents a data-driven crafting recipe parsed from items.json.
    /// </summary>
    public class CraftingRecipe
    {
        public string ResultItemId { get; set; }
        
        // Dictionary mapping required material string ID to required quantity.
        // e.g. {"EchoDust": 3, "IronKey": 1}
        public Dictionary<string, int> RequiredMaterials { get; set; } = new Dictionary<string, int>();
    }

    /// <summary>
    /// Handles verifying and consuming inventory materials to forge new items.
    /// Zero-allocation validation checks.
    /// </summary>
    public static class CraftingSystem
    {
        /// <summary>
        /// Checks if the provided inventory has enough materials to satisfy the recipe.
        /// </summary>
        public static bool CanCraft(CraftingRecipe recipe, Entities.Items.Inventory playerInventory)
        {
            foreach (var kvp in recipe.RequiredMaterials)
            {
                if (playerInventory.GetQuantityById(kvp.Key) < kvp.Value)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Consumes the required materials and adds the result item to the inventory.
        /// Returns true if successful.
        /// </summary>
        public static bool TryCraft(CraftingRecipe recipe, Entities.Items.Inventory playerInventory)
        {
            if (!CanCraft(recipe, playerInventory))
                return false;

            // Consume
            foreach (var kvp in recipe.RequiredMaterials)
            {
                playerInventory.AddItem(kvp.Key, -kvp.Value);
            }

            // Grant forged item
            playerInventory.AddItem(recipe.ResultItemId, 1);
            return true;
        }
    }
}
