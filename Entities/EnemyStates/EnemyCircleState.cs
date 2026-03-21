// ============================================================================
// EnemyCircleState.cs — Circle-Player Steering Behavior
// Author: Mehdi Lakhouane
// Description: The enemy orbits around the player at a fixed radius,
//              occasionally dashing in for an attack. Used for smarter
//              enemies like the Raccoon.
//
// Circular movement math:
//   1. Compute the vector from player to enemy.
//   2. Get the current angle: atan2(dy, dx).
//   3. Each frame, increment angle by (AngularSpeed * dt).
//   4. New position = player + (cos(angle), sin(angle)) * OrbitRadius.
//   5. Every DashInterval seconds, switch to a "dash" sub-phase that
//      moves straight toward the player for DashDuration seconds.
// ============================================================================

using System;
using Microsoft.Xna.Framework;
using TheGame.Systems;

namespace TheGame.Entities.EnemyStates
{
    public class EnemyCircleState : IState<Enemy>
    {
        // ── Orbit parameters ──
        private const float OrbitRadius = 60f;       // distance to maintain from player
        private const float AngularSpeed = 1.8f;     // radians per second
        private const float ApproachSpeed = 100f;    // speed to reach orbit distance

        // ── Dash sub-phase ──
        private const float DashInterval = 3.0f;     // seconds between dashes
        private const float DashDuration = 0.4f;     // how long the dash lasts
        private const float DashSpeed = 220f;        // speed during dash

        private float _currentAngle;
        private float _dashTimer;
        private bool _isDashing;
        private float _dashClock;
        private Vector2 _dashDir;

        public void OnEnter(Enemy enemy, StateMachine<Enemy> sm)
        {
            enemy.PlayAnimation("move");

            // Calculate starting angle from current relative position
            Vector2 toEnemy = enemy.Position - enemy.TargetPosition;
            _currentAngle = MathF.Atan2(toEnemy.Y, toEnemy.X);
            _dashTimer = DashInterval * 0.5f; // first dash comes faster
            _isDashing = false;
        }

        public void OnUpdate(Enemy enemy, StateMachine<Enemy> sm, GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // If player is out of range, go back to chase
            float dist = Vector2.Distance(enemy.Position, enemy.TargetPosition);
            if (dist > enemy.DetectionRangeValue * 1.5f)
            {
                sm.SetState("chase");
                return;
            }

            if (_isDashing)
            {
                // ── Dash sub-phase: charge toward player ──
                _dashClock += dt;
                enemy.Position += _dashDir * DashSpeed * dt;
                enemy.PlayAnimation("attack");

                if (_dashClock >= DashDuration)
                {
                    _isDashing = false;
                    _dashTimer = 0f;
                    enemy.PlayAnimation("move");

                    // Recalculate angle after dash
                    Vector2 toEnemy = enemy.Position - enemy.TargetPosition;
                    _currentAngle = MathF.Atan2(toEnemy.Y, toEnemy.X);
                }
            }
            else
            {
                // ── Orbit sub-phase ──

                // Increment the orbit angle
                _currentAngle += AngularSpeed * dt;

                // Calculate desired position on the circle
                Vector2 desiredPos = enemy.TargetPosition + new Vector2(
                    MathF.Cos(_currentAngle) * OrbitRadius,
                    MathF.Sin(_currentAngle) * OrbitRadius);

                // Move toward the desired orbit position
                Vector2 toDesired = desiredPos - enemy.Position;
                float distToDesired = toDesired.Length();

                if (distToDesired > 1f)
                {
                    toDesired.Normalize();
                    float moveSpeed = MathF.Min(ApproachSpeed, distToDesired / dt);
                    enemy.Velocity = toDesired * moveSpeed;
                    enemy.Position += enemy.Velocity * dt;
                }

                // Update facing toward the player
                Vector2 toPlayer = enemy.TargetPosition - enemy.Position;
                if (MathF.Abs(toPlayer.X) > MathF.Abs(toPlayer.Y))
                    enemy.Facing = toPlayer.X > 0 ? FacingDirection.Right : FacingDirection.Left;
                else
                    enemy.Facing = toPlayer.Y > 0 ? FacingDirection.Down : FacingDirection.Up;

                // Dash timer
                _dashTimer += dt;
                if (_dashTimer >= DashInterval)
                {
                    _isDashing = true;
                    _dashClock = 0f;
                    _dashDir = toPlayer;
                    if (_dashDir != Vector2.Zero) _dashDir.Normalize();
                }

                enemy.TickAnimation(dt);
            }
        }

        public void OnExit(Enemy enemy, StateMachine<Enemy> sm)
        {
            enemy.Velocity = Vector2.Zero;
        }
    }
}
