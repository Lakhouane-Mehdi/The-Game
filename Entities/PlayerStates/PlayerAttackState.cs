// ============================================================================
// PlayerAttackState.cs — Player Attack State
// Author: Mehdi Lakhouane
// Description: Locks movement, activates the attack hitbox in front of the
//              player for a fixed duration, then transitions to Cooldown.
// ============================================================================

using Microsoft.Xna.Framework;
using TheGame.Systems;

namespace TheGame.Entities.PlayerStates
{
    public class PlayerAttackState : IState<Player>
    {
        private const float Duration = 0.3f;   // how long the attack hitbox is active

        public void OnEnter(Player player, StateMachine<Player> sm)
        {
            player.Velocity = Vector2.Zero;     // lock movement
            player.SetAttackSprite();
            player.ComputeAttackHitbox();
        }

        public void OnUpdate(Player player, StateMachine<Player> sm, GameTime gameTime)
        {
            // Keep the hitbox updated (in case position shifts from knockback)
            player.ComputeAttackHitbox();

            // Transition after duration expires
            if (sm.StateTimer >= Duration)
            {
                sm.SetState("cooldown");
                return;
            }
        }

        public void OnExit(Player player, StateMachine<Player> sm)
        {
            player.ClearAttackHitbox();
        }
    }
}
