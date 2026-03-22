using Microsoft.Xna.Framework;
using System;

namespace TheGame.Systems
{
    /// <summary>
    /// Handles Game "Juice": Hit-Stops (Engine freezing) and Camera Zoom bursting.
    /// This should be updated right before the main gameplay ECS/systems.
    /// </summary>
    public class JuiceManager
    {
        // Hit-Stop frames tracker
        private int _hitStopFramesLeft = 0;

        // Camera zoom burst tracker
        private float _baseZoom = 2.0f; // Scale assumed from README 2x scale
        private float _currentZoomTarget = 2.0f;
        public float CurrentZoom { get; private set; } = 2.0f;

        /// <summary>
        /// Freezes the main update loop for N frames to emphasize a heavy hit.
        /// </summary>
        public void AddHitStop(int frames)
        {
            _hitStopFramesLeft = Math.Max(_hitStopFramesLeft, frames);
        }

        /// <summary>
        /// Dynamically punches the camera zoom in temporarily.
        /// </summary>
        public void TriggerZoom(float targetScale)
        {
            _currentZoomTarget = targetScale;
        }

        /// <summary>
        /// Returns true if the game logic should freeze this frame.
        /// Put this check at the very top of Game1.Update() or OverworldState.Update().
        /// </summary>
        public bool UpdateHitStop()
        {
            if (_hitStopFramesLeft > 0)
            {
                _hitStopFramesLeft--;
                return true; // Skip game update this frame
            }
            return false; // Proceed normally
        }

        /// <summary>
        /// Updates dynamic lerping for the camera zoom.
        /// </summary>
        public void UpdateZoom(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Lerp current zoom towards target
            CurrentZoom = MathHelper.Lerp(CurrentZoom, _currentZoomTarget, 10f * dt);

            // If we are close to the target, spring back to the base zoom
            if (Math.Abs(CurrentZoom - _currentZoomTarget) < 0.05f)
            {
                _currentZoomTarget = _baseZoom;
            }
        }
    }
}
