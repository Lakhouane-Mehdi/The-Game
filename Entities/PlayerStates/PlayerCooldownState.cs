// ============================================================================
// PlayerCooldownState.cs — Post-Attack Cooldown
// Author: Mehdi Lakhouane
// Description: Brief lockout after attacking. Prevents attack spam.
//              Transitions back to Idle once the cooldown expires.
// ============================================================================

using Microsoft.Xna.Framework;
using TheGame.Systems;

namespace TheGame.Entities.PlayerStates
{
    public class PlayerCooldownState : IState<Player>
    {
        private const float Duration = 0.15f;

        public void OnEnter(Player player, StateMachine<Player> sm)
        {
            player.Velocity = Vector2.Zero;
            player.SetIdleSprite();
        }

        public void OnUpdate(Player player, StateMachine<Player> sm, GameTime gameTime)
        {
            if (sm.StateTimer >= Duration)
            {
                sm.SetState("idle");
                return;
            }
        }

        public void OnExit(Player player, StateMachine<Player> sm)
        {
            // Nothing to clean up
        }
    }
}
