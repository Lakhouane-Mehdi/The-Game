namespace TheGame.Entities.Interfaces
{
    /// <summary>
    /// Contract for creatures (Echoes) captured by the player.
    /// Provides passive abilities or skill modifiers when equipped in the party.
    /// </summary>
    public interface IResonantAbility
    {
        /// <summary>
        /// Triggered when the Echo rests in the active party slot.
        /// Useful for boosting player stats, giving immunity, etc.
        /// </summary>
        void ApplyPassive(Player player);

        /// <summary>
        /// Triggered when the Echo is unequipped or swapped out.
        /// Must revert the changes made in ApplyPassive.
        /// </summary>
        void RemovePassive(Player player);

        /// <summary>
        /// Hook invoked during the Player's sword attack phase to modify elemental logic.
        /// E.g. "Flame Raccoon" adds fire damage properties.
        /// </summary>
        void ModifyAttack(Player player, ref int damageAmount, ref string damageElement);
    }
}
