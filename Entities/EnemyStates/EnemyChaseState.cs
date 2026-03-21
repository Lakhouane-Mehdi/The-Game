// ============================================================================
// EnemyChaseState.cs — Enemy Chase State
// Author: Mehdi Lakhouane
// Description: Enemy moves toward the player. Transitions to Attack when
//              close enough, or back to Idle when the player escapes range.
// ============================================================================

using Microsoft.Xna.Framework;
using TheGame.Systems;

namespace TheGame.Entities.EnemyStates
{
    public class EnemyChaseState : IState<Enemy>
    {
        public void OnEnter(Enemy enemy, StateMachine<Enemy> sm)
        {
            enemy.PlayAnimation("move");
        }

        public void OnUpdate(Enemy enemy, StateMachine<Enemy> sm, GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            float dist = Vector2.Distance(enemy.Position, enemy.TargetPosition);

            // Close enough to attack?
            if (dist <= enemy.AttackRangeValue)
            {
                sm.SetState("attack");
                return;
            }

            // Player escaped detection range?
            if (dist > enemy.DetectionRangeValue)
            {
                sm.SetState("idle");
                return;
            }

            // Move toward player
            Vector2 dir = enemy.TargetPosition - enemy.Position;
            if (dir != Vector2.Zero) dir.Normalize();

            enemy.Velocity = dir * enemy.Speed;
            enemy.Position += enemy.Velocity * dt;

            // Update facing
            if (System.MathF.Abs(dir.X) > System.MathF.Abs(dir.Y))
                enemy.Facing = dir.X > 0 ? FacingDirection.Right : FacingDirection.Left;
            else
                enemy.Facing = dir.Y > 0 ? FacingDirection.Down : FacingDirection.Up;

            enemy.TickAnimation(dt);
        }

        public void OnExit(Enemy enemy, StateMachine<Enemy> sm)
        {
            enemy.Velocity = Vector2.Zero;
        }
    }
}
