// ============================================================================
// FadeTransition.cs — Screen Fade Effect
// Author: Mehdi Lakhouane
// Description: Manages fade-to-black and fade-from-black transitions used
//              when entering/exiting interiors. Calls an Action at midpoint
//              (when screen is fully black) to swap game state.
// ============================================================================

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheGame.Core;

namespace TheGame.Systems
{
    public class FadeTransition
    {
        public bool IsActive { get; private set; }

        private float _timer;
        private float _halfDuration;
        private float _totalDuration;
        private bool _midpointFired;
        private Action _onMidpoint;

        private const float DefaultDuration = 0.8f; // total fade cycle

        /// <summary>
        /// Starts a fade-out → action → fade-in cycle.
        /// </summary>
        public void Start(Action onMidpoint, float duration = DefaultDuration)
        {
            IsActive = true;
            _timer = 0f;
            _totalDuration = duration;
            _halfDuration = duration / 2f;
            _midpointFired = false;
            _onMidpoint = onMidpoint;
        }

        public void Update(float dt)
        {
            if (!IsActive) return;

            _timer += dt;

            // Fire midpoint action when fully black
            if (!_midpointFired && _timer >= _halfDuration)
            {
                _midpointFired = true;
                _onMidpoint?.Invoke();
            }

            if (_timer >= _totalDuration)
            {
                IsActive = false;
            }
        }

        public void Draw(SpriteBatch sb)
        {
            if (!IsActive) return;

            float alpha;
            if (_timer < _halfDuration)
            {
                // Fade out (0 → 1)
                alpha = _timer / _halfDuration;
            }
            else
            {
                // Fade in (1 → 0)
                alpha = 1f - (_timer - _halfDuration) / _halfDuration;
            }

            sb.Draw(Game1.PixelTexture,
                new Rectangle(0, 0, Game1.ScreenWidth, Game1.ScreenHeight),
                Color.Black * alpha);
        }
    }
}
