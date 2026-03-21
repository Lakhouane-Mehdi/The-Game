// ============================================================================
// EnemyIdleState.cs — Enemy Idle State
// Author: Mehdi Lakhouane
// Description: Enemy stands still, plays idle animation. Transitions to
//              Chase when the player enters detection range.
// ============================================================================

using Microsoft.Xna.Framework;
using TheGame.Systems;

namespace TheGame.Entities.EnemyStates
{
    public class EnemyIdleState : IState<Enemy>
    {
        public void OnEnter(Enemy enemy, StateMachine<Enemy> sm)
        {
            enemy.Velocity = Vector2.Zero;
            enemy.PlayAnimation("idle");
        }

        public void OnUpdate(Enemy enemy, StateMachine<Enemy> sm, GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            enemy.TickAnimation(dt);

            // Check if player is in range
            float dist = Vector2.Distance(enemy.Position, enemy.TargetPosition);

            if (dist <= enemy.AttackRangeValue)
            {
                sm.SetState("attack");
                return;
            }

            if (dist <= enemy.DetectionRangeValue)
            {
                sm.SetState("chase");
                return;
            }
        }

        public void OnExit(Enemy enemy, StateMachine<Enemy> sm)
        {
        }
    }
}
