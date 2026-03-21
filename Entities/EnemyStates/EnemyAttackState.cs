// ============================================================================
// EnemyAttackState.cs — Enemy Attack State
// Author: Mehdi Lakhouane
// Description: Enemy stops moving and plays attack animation.
//              After the attack duration, transitions back to Chase or Idle.
// ============================================================================

using Microsoft.Xna.Framework;
using TheGame.Systems;

namespace TheGame.Entities.EnemyStates
{
    public class EnemyAttackState : IState<Enemy>
    {
        private const float AttackDuration = 0.5f;

        public void OnEnter(Enemy enemy, StateMachine<Enemy> sm)
        {
            enemy.Velocity = Vector2.Zero;
            enemy.PlayAnimation("attack");
        }

        public void OnUpdate(Enemy enemy, StateMachine<Enemy> sm, GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            enemy.TickAnimation(dt);

            if (sm.StateTimer >= AttackDuration)
            {
                // After attacking, decide next state based on distance
                float dist = Vector2.Distance(enemy.Position, enemy.TargetPosition);

                if (dist <= enemy.DetectionRangeValue)
                    sm.SetState("chase");
                else
                    sm.SetState("idle");
                return;
            }
        }

        public void OnExit(Enemy enemy, StateMachine<Enemy> sm)
        {
        }
    }
}
