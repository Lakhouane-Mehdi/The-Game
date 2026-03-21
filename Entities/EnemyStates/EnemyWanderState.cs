// ============================================================================
// EnemyWanderState.cs — Random Wander Behavior
// Author: Mehdi Lakhouane
// Description: Enemy moves in a random direction for a random duration,
//              then picks a new direction. Transitions to Chase when the
//              player enters detection range. More natural than standing
//              still in Idle.
//
// Steering math:
//   - Pick a random angle every 1–3 seconds.
//   - Move at 40% of Speed in that direction.
//   - If player enters DetectionRange, switch to Chase.
// ============================================================================

using System;
using Microsoft.Xna.Framework;
using TheGame.Systems;

namespace TheGame.Entities.EnemyStates
{
    public class EnemyWanderState : IState<Enemy>
    {
        private static readonly Random _rng = new();
        private Vector2 _wanderDir;
        private float _wanderTimer;
        private float _wanderDuration;
        private const float WanderSpeedFactor = 0.4f;

        public void OnEnter(Enemy enemy, StateMachine<Enemy> sm)
        {
            PickNewDirection();
            enemy.PlayAnimation("move");
        }

        public void OnUpdate(Enemy enemy, StateMachine<Enemy> sm, GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Check if player is in range
            float dist = Vector2.Distance(enemy.Position, enemy.TargetPosition);
            if (dist <= enemy.DetectionRangeValue)
            {
                sm.SetState("chase");
                return;
            }

            // Wander movement
            _wanderTimer += dt;
            if (_wanderTimer >= _wanderDuration)
                PickNewDirection();

            enemy.Velocity = _wanderDir * (enemy.Speed * WanderSpeedFactor);
            enemy.Position += enemy.Velocity * dt;

            // Update facing
            if (MathF.Abs(_wanderDir.X) > MathF.Abs(_wanderDir.Y))
                enemy.Facing = _wanderDir.X > 0 ? FacingDirection.Right : FacingDirection.Left;
            else if (_wanderDir != Vector2.Zero)
                enemy.Facing = _wanderDir.Y > 0 ? FacingDirection.Down : FacingDirection.Up;

            enemy.TickAnimation(dt);
        }

        public void OnExit(Enemy enemy, StateMachine<Enemy> sm)
        {
            enemy.Velocity = Vector2.Zero;
        }

        private void PickNewDirection()
        {
            float angle = (float)(_rng.NextDouble() * MathF.PI * 2f);
            _wanderDir = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            _wanderTimer = 0f;
            _wanderDuration = 1f + (float)_rng.NextDouble() * 2f; // 1–3 seconds
        }
    }
}
