// ============================================================================
// Entity.cs — Base Entity Class
// Author: Mehdi Lakhouane
// Description: Abstract base for every game object that has a position,
//              a bounding box, and participates in collision detection.
//              Both the Player and all Enemies inherit from this.
// ============================================================================

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheGame.Core;

namespace TheGame.Entities
{
    /// <summary>
    /// Which way an entity is facing — used for sprite selection.
    /// </summary>
    public enum FacingDirection
    {
        Down,
        Up,
        Left,
        Right
    }

    /// <summary>
    /// Base class shared by Player and Enemy.
    /// Encapsulates position, velocity, sprite drawing, and an AABB hitbox.
    /// </summary>
    public abstract class Entity
    {
        // ── Transform ──
        public Vector2 Position;
        public Vector2 Velocity;

        // ── Rendering ──
        public Texture2D Sprite { get; set; }
        public Color Tint { get; set; } = Color.White;
        public FacingDirection Facing { get; set; } = FacingDirection.Down;

        // ── Collision ──
        public int HitboxWidth { get; protected set; } = 32;
        public int HitboxHeight { get; protected set; } = 32;
        public Vector2 HitboxOffset { get; protected set; } = Vector2.Zero;

        // ── Stats ──
        public int MaxHealth { get; protected set; } = 3;
        public int CurrentHealth { get; protected set; }
        public bool IsAlive => CurrentHealth > 0;
        public float Speed { get; protected set; } = 150f;

        // ── Invincibility frames ──
        protected float InvincibilityTimer;
        protected float InvincibilityDuration = 0.8f;
        public bool IsInvincible => InvincibilityTimer > 0f;

        protected Entity(Vector2 startPosition, int maxHealth)
        {
            Position = startPosition;
            Velocity = Vector2.Zero;
            MaxHealth = maxHealth;
            CurrentHealth = maxHealth;
        }

        /// <summary>
        /// Returns the axis-aligned bounding box used for collision checks.
        /// </summary>
        public Rectangle BoundingBox => new Rectangle(
            (int)(Position.X + HitboxOffset.X),
            (int)(Position.Y + HitboxOffset.Y),
            HitboxWidth,
            HitboxHeight
        );

        public virtual void TakeDamage(int amount)
        {
            if (IsInvincible || !IsAlive) return;
            CurrentHealth -= amount;
            InvincibilityTimer = InvincibilityDuration;
        }

        public virtual void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (InvincibilityTimer > 0f)
                InvincibilityTimer -= dt;
        }

        public virtual void Draw(SpriteBatch spriteBatch)
        {
            if (Sprite != null && IsAlive)
            {
                // Blink during invincibility
                Color drawColor = Tint;
                if (IsInvincible && ((int)(InvincibilityTimer * 10) % 2 == 0))
                    drawColor = Color.Transparent;

                spriteBatch.Draw(Sprite, Position, drawColor);
            }
        }

        /// <summary>
        /// Debug: draws the bounding box as a coloured outline.
        /// </summary>
        public void DrawHitbox(SpriteBatch spriteBatch, Color color)
        {
            Texture2D px = Game1.PixelTexture;
            Rectangle bb = BoundingBox;
            int t = 1; // thickness
            spriteBatch.Draw(px, new Rectangle(bb.X, bb.Y, bb.Width, t), color);
            spriteBatch.Draw(px, new Rectangle(bb.X, bb.Bottom - t, bb.Width, t), color);
            spriteBatch.Draw(px, new Rectangle(bb.X, bb.Y, t, bb.Height), color);
            spriteBatch.Draw(px, new Rectangle(bb.Right - t, bb.Y, t, bb.Height), color);
        }
    }
}
