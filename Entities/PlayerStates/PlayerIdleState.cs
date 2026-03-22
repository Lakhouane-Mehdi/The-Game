// ============================================================================
// PlayerIdleState.cs — Player Idle State
// Author: Mehdi Lakhouane
// Description: Player is standing still. Listens for movement (→Walk),
//              attack (→Attack), item use (→Item), or inventory cycle (Q/E).
// ============================================================================

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TheGame.Systems;

namespace TheGame.Entities.PlayerStates
{
    public class PlayerIdleState : IState<Player>
    {
        public void OnEnter(Player player, StateMachine<Player> sm)
        {
            player.Velocity = Vector2.Zero;
            player.ResetWalkAnimations();
            player.SetIdleSprite();
        }

        public void OnUpdate(Player player, StateMachine<Player> sm, GameTime gameTime)
        {
            KeyboardState kb = Keyboard.GetState();

            // Attack (edge-triggered)
            if (kb.IsKeyDown(Keys.Space) && player.PrevKeyboard.IsKeyUp(Keys.Space))
            {
                sm.SetState("attack");
                return;
            }

            // Item use — X key (edge-triggered)
            if (kb.IsKeyDown(Keys.X) && player.PrevKeyboard.IsKeyUp(Keys.X))
            {
                if (player.Inventory.CanUseEquipped())
                {
                    sm.SetState("item");
                    return;
                }
            }

            // Echo Pulse — R key (edge-triggered)
            if (kb.IsKeyDown(Keys.R) && player.PrevKeyboard.IsKeyUp(Keys.R))
            {
                sm.SetState("capture");
                return;
            }

            // Cycle inventory — Q / E (edge-triggered)
            if (kb.IsKeyDown(Keys.Q) && player.PrevKeyboard.IsKeyUp(Keys.Q))
                player.Inventory.CyclePrev();
            if (kb.IsKeyDown(Keys.E) && player.PrevKeyboard.IsKeyUp(Keys.E))
                player.Inventory.CycleNext();

            // Movement
            Vector2 input = Player.ReadMovementInput(kb);
            if (input != Vector2.Zero)
            {
                sm.SetState("walk");
                return;
            }

            player.SetIdleSprite();
        }

        public void OnExit(Player player, StateMachine<Player> sm)
        {
        }
    }
}
