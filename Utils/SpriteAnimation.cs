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
        // Old style
        public Texture2D[] Frames { get; }
        
        // New style (Sprite Sheet)
        public Texture2D Sheet { get; }
        public int FrameWidth { get; }
        public int FrameHeight { get; }
        public int FrameCount { get; }
        public int StartFrameIndex { get; }

        public float FrameDuration { get; set; }
        public bool IsLooping { get; set; }
        public bool IsFinished { get; private set; }

        private int _currentFrame;
        private float _timer;
        private readonly bool _isSheet;

        // Legacy constructor
        public SpriteAnimation(Texture2D[] frames, float frameDuration = 0.15f, bool loop = true)
        {
            Frames = frames;
            FrameCount = frames.Length;
            FrameDuration = frameDuration;
            IsLooping = loop;
            _isSheet = false;
            Reset();
        }

        // New Sprite Sheet constructor
        public SpriteAnimation(Texture2D sheet, int frameWidth, int frameHeight, int frameCount, int startFrameIndex = 0, float frameDuration = 0.15f, bool loop = true)
        {
            Sheet = sheet;
            FrameWidth = frameWidth;
            FrameHeight = frameHeight;
            FrameCount = frameCount;
            StartFrameIndex = startFrameIndex;
            FrameDuration = frameDuration;
            IsLooping = loop;
            _isSheet = true;
            Reset();
        }

        public Texture2D CurrentTexture => _isSheet ? Sheet : Frames[_currentFrame];

        public Rectangle? CurrentSourceRect
        {
            get
            {
                if (!_isSheet) return null;
                int cols = Sheet.Width / FrameWidth;
                int absFrame = StartFrameIndex + _currentFrame;
                int row = absFrame / cols;
                int col = absFrame % cols;
                return new Rectangle(col * FrameWidth, row * FrameHeight, FrameWidth, FrameHeight);
            }
        }

        public void Update(float deltaTime)
        {
            if (IsFinished) return;

            _timer += deltaTime;
            if (_timer >= FrameDuration)
            {
                _timer -= FrameDuration;
                _currentFrame++;

                if (_currentFrame >= FrameCount)
                {
                    if (IsLooping)
                        _currentFrame = 0;
                    else
                    {
                        _currentFrame = FrameCount - 1;
                        IsFinished = true;
                    }
                }
            }
        }

        public void Reset()
        {
            _currentFrame = 0;
            _timer = 0f;
            IsFinished = false;
        }

        public void Draw(SpriteBatch spriteBatch, Vector2 position, Color color)
        {
            spriteBatch.Draw(CurrentTexture, position, CurrentSourceRect, color);
        }
    }
}
