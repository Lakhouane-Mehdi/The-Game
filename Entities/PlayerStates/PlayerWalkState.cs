// ============================================================================
// PlayerWalkState.cs — Player Walk State
// Author: Mehdi Lakhouane
// Description: 8-directional movement with normalised diagonal speed.
//              Transitions to Idle (no input), Attack (Space), or Item (X).
// ============================================================================

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TheGame.Systems;

namespace TheGame.Entities.PlayerStates
{
    public class PlayerWalkState : IState<Player>
    {
        public void OnEnter(Player player, StateMachine<Player> sm)
        {
        }

        public void OnUpdate(Player player, StateMachine<Player> sm, GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
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

            // Cycle inventory — Q / E
            if (kb.IsKeyDown(Keys.Q) && player.PrevKeyboard.IsKeyUp(Keys.Q))
                player.Inventory.CyclePrev();
            if (kb.IsKeyDown(Keys.E) && player.PrevKeyboard.IsKeyUp(Keys.E))
                player.Inventory.CycleNext();

            // Movement
            Vector2 input = Player.ReadMovementInput(kb);

            if (input == Vector2.Zero)
            {
                sm.SetState("idle");
                return;
            }

            input.Normalize();
            player.Velocity = input * player.Speed;

            if (System.MathF.Abs(input.X) > System.MathF.Abs(input.Y))
                player.Facing = input.X > 0 ? FacingDirection.Right : FacingDirection.Left;
            else
                player.Facing = input.Y > 0 ? FacingDirection.Down : FacingDirection.Up;

            player.Position += player.Velocity * dt;
            player.UpdateWalkAnimation(dt);
        }

        public void OnExit(Player player, StateMachine<Player> sm)
        {
            player.Velocity = Vector2.Zero;
        }
    }
}
