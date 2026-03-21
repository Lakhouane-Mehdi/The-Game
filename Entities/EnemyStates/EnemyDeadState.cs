// ============================================================================
// EnemyDeadState.cs — Enemy Death State
// Author: Mehdi Lakhouane
// Description: Plays death fade-out. Once complete, marks the enemy for
//              removal from the world.
// ============================================================================

using Microsoft.Xna.Framework;
using TheGame.Systems;

namespace TheGame.Entities.EnemyStates
{
    public class EnemyDeadState : IState<Enemy>
    {
        public void OnEnter(Enemy enemy, StateMachine<Enemy> sm)
        {
            enemy.Velocity = Vector2.Zero;
            enemy.IsMarkedForRemoval = false;  // will be set true after fade
        }

        public void OnUpdate(Enemy enemy, StateMachine<Enemy> sm, GameTime gameTime)
        {
            // DeathFadeTime is handled by Enemy.Draw — we just wait
            if (sm.StateTimer >= Enemy.DeathFadeTime)
            {
                enemy.IsMarkedForRemoval = true;
            }
        }

        public void OnExit(Enemy enemy, StateMachine<Enemy> sm)
        {
        }
    }
}
