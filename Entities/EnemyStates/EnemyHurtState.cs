// ============================================================================
// EnemyHurtState.cs — Enemy Hurt / Knockback State
// Author: Mehdi Lakhouane
// Description: Enemy was hit. Applies knockback impulse and briefly stuns
//              the enemy before returning to Chase or Idle.
// ============================================================================

using Microsoft.Xna.Framework;
using TheGame.Systems;

namespace TheGame.Entities.EnemyStates
{
    public class EnemyHurtState : IState<Enemy>
    {
        private const float KnockbackDuration = 0.2f;
        private const float KnockbackForce = 250f;

        public void OnEnter(Enemy enemy, StateMachine<Enemy> sm)
        {
            enemy.Velocity = Vector2.Zero;

            // Compute knockback direction (away from the damage source)
            Vector2 dir = enemy.Position - enemy.TargetPosition;
            if (dir != Vector2.Zero) dir.Normalize();
            enemy.KnockbackVelocity = dir * KnockbackForce;
        }

        public void OnUpdate(Enemy enemy, StateMachine<Enemy> sm, GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Apply knockback movement
            if (sm.StateTimer < KnockbackDuration)
            {
                enemy.Position += enemy.KnockbackVelocity * dt;
            }
            else
            {
                // Knockback finished — return to AI
                if (!enemy.IsAlive)
                {
                    sm.SetState("dead");
                    return;
                }

                float dist = Vector2.Distance(enemy.Position, enemy.TargetPosition);
                if (dist <= enemy.DetectionRangeValue)
                    sm.SetState("chase");
                else
                    sm.SetState("idle");
            }
        }

        public void OnExit(Enemy enemy, StateMachine<Enemy> sm)
        {
            enemy.KnockbackVelocity = Vector2.Zero;
        }
    }
}
