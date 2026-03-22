using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using TheGame.Systems;

namespace TheGame.Entities.PlayerStates
{
    /// <summary>
    /// PlayerCaptureState — The 'Echo Pulse' state.
    /// When active, the player emits a resonance wave. 
    /// If a low-health enemy is caught in the wave, it is captured as an Echo.
    /// </summary>
    public class PlayerCaptureState : IState<Player>
    {
        private float _timer;
        private const float Duration = 0.5f;
        private bool _captured;

        public void OnEnter(Player player, StateMachine<Player> sm)
        {
            _timer = 0f;
            _captured = false;
            player.Velocity = Vector2.Zero;
            
            // Trigger a visual pulse effect if the system supports it
            // OverworldState will handle the PulseEffect list.
        }

        public void OnUpdate(Player player, StateMachine<Player> sm, GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _timer += dt;

            if (!_captured)
            {
                // Range for 'Echo Pulse'
                float pulseRange = 64f;
                Rectangle pulseArea = new Rectangle(
                    (int)(player.BoundingBox.Center.X - pulseRange),
                    (int)(player.BoundingBox.Center.Y - pulseRange),
                    (int)(pulseRange * 2),
                    (int)(pulseRange * 2)
                );

                // This logic normally lives in OverworldState, 
                // but we can signal it or do a rough check here if enemies are accessible.
                // However, states shouldn't ideally know about the global enemy list.
                // We'll rely on a callback or the OverworldState checking for this state.
            }

            if (_timer >= Duration)
            {
                sm.SetState("idle");
            }
        }

        public void OnExit(Player player, StateMachine<Player> sm)
        {
        }
    }
}
