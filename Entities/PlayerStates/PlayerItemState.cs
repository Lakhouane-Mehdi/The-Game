// ============================================================================
// PlayerItemState.cs — Player Item-Use State
// Author: Mehdi Lakhouane
// Description: Entered when the player presses X with an equipped item.
//              Spawns a projectile (or boomerang) and transitions back to
//              idle after the item's cooldown expires.
// ============================================================================

using Microsoft.Xna.Framework;
using TheGame.Entities.Items;
using TheGame.Systems;

namespace TheGame.Entities.PlayerStates
{
    public class PlayerItemState : IState<Player>
    {
        public void OnEnter(Player player, StateMachine<Player> sm)
        {
            player.Velocity = Vector2.Zero;
            player.SetAttackSprite(); // reuse the attack sprite for item use

            // Spawn the projectile via the player's public callback
            player.SpawnItemProjectile();
        }

        public void OnUpdate(Player player, StateMachine<Player> sm, GameTime gameTime)
        {
            // Wait for the item cooldown, then return to idle
            float cooldown = player.GetEquippedCooldown();
            if (sm.StateTimer >= cooldown)
            {
                sm.SetState("idle");
                return;
            }
        }

        public void OnExit(Player player, StateMachine<Player> sm)
        {
        }
    }
}
