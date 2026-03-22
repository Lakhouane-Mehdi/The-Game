using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace TheGame.Systems
{
    /// <summary>
    /// A strictly zero-allocation particle system using a fixed-size struct array (Object Pool).
    /// Used for bursting "Echo Dust" when an enemy is defeated.
    /// </summary>
    public class ParticleSystem
    {
        private struct Particle
        {
            public bool IsActive;
            public Vector2 Position;
            public Vector2 Velocity;
            public float LifeLeft;
            public float MaxLife;
            public Color Tint;
        }

        private const int MAX_PARTICLES = 500;
        private readonly Particle[] _particles = new Particle[MAX_PARTICLES];
        private int _poolIndex = 0;
        
        private readonly Texture2D _particleTexture;
        private readonly Random _rand = new Random();

        public ParticleSystem(Texture2D pixelTexture)
        {
            _particleTexture = pixelTexture;
        }

        /// <summary>
        /// Emits a burst of Echo Dust. Allocates nothing.
        /// </summary>
        public void EmitEchoDust(Vector2 origin, int count)
        {
            for (int i = 0; i < count; i++)
            {
                // Advance pool pointer
                _poolIndex = (_poolIndex + 1) % MAX_PARTICLES;

                // Fire particles in a circle
                float angle = (float)(_rand.NextDouble() * Math.PI * 2);
                float speed = (float)(_rand.NextDouble() * 50f + 20f); // 20 to 70 pixels per sec

                _particles[_poolIndex] = new Particle
                {
                    IsActive = true,
                    Position = origin,
                    Velocity = new Vector2((float)Math.Cos(angle) * speed, (float)Math.Sin(angle) * speed),
                    LifeLeft = 1.0f,
                    MaxLife = 1.0f,
                    Tint = Color.Cyan * 0.8f // Resonant Echo blue
                };
            }
        }

        public void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            for (int i = 0; i < MAX_PARTICLES; i++)
            {
                if (_particles[i].IsActive)
                {
                    _particles[i].LifeLeft -= dt;
                    if (_particles[i].LifeLeft <= 0)
                    {
                        _particles[i].IsActive = false;
                        continue;
                    }

                    _particles[i].Position += _particles[i].Velocity * dt;
                    
                    // Simple friction/drag
                    _particles[i].Velocity *= 0.95f;
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            for (int i = 0; i < MAX_PARTICLES; i++)
            {
                if (_particles[i].IsActive)
                {
                    float alpha = _particles[i].LifeLeft / _particles[i].MaxLife;
                    spriteBatch.Draw(_particleTexture, _particles[i].Position, _particles[i].Tint * alpha);
                }
            }
        }
    }
}
