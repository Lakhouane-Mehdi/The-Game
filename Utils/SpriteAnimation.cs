// ============================================================================
// SpriteAnimation.cs — Frame-based Sprite Animation
// Author: Mehdi Lakhouane
// Description: Manages a sequence of texture frames with configurable
//              frame duration. Used for player walk cycles, enemy animations, etc.
// ============================================================================

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TheGame.Utils
{
    /// <summary>
    /// Plays through an array of textures at a fixed frame rate.
    /// Supports looping and one-shot playback.
    /// </summary>
    public class SpriteAnimation
    {
        public Texture2D[] Frames { get; }
        public float FrameDuration { get; set; }
        public bool IsLooping { get; set; }
        public bool IsFinished { get; private set; }

        private int _currentFrame;
        private float _timer;

        public SpriteAnimation(Texture2D[] frames, float frameDuration = 0.15f, bool loop = true)
        {
            Frames = frames;
            FrameDuration = frameDuration;
            IsLooping = loop;
            _currentFrame = 0;
            _timer = 0f;
            IsFinished = false;
        }

        /// <summary>
        /// Returns the current frame texture.
        /// </summary>
        public Texture2D CurrentFrame => Frames[_currentFrame];

        /// <summary>
        /// Advances the animation timer.
        /// </summary>
        public void Update(float deltaTime)
        {
            if (IsFinished) return;

            _timer += deltaTime;
            if (_timer >= FrameDuration)
            {
                _timer -= FrameDuration;
                _currentFrame++;

                if (_currentFrame >= Frames.Length)
                {
                    if (IsLooping)
                        _currentFrame = 0;
                    else
                    {
                        _currentFrame = Frames.Length - 1;
                        IsFinished = true;
                    }
                }
            }
        }

        /// <summary>
        /// Reset to the first frame.
        /// </summary>
        public void Reset()
        {
            _currentFrame = 0;
            _timer = 0f;
            IsFinished = false;
        }

        /// <summary>
        /// Draw the current frame at the given position.
        /// </summary>
        public void Draw(SpriteBatch spriteBatch, Vector2 position, Color color)
        {
            spriteBatch.Draw(CurrentFrame, position, color);
        }
    }
}
