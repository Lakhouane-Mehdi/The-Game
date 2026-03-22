using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace TheGame.Entities
{
    /// <summary>
    /// Represents a captured Echo that trails the player.
    /// Uses a queue of historical positions to create a smooth snake-like follow effect.
    /// </summary>
    public class EchoEntity : Entity
    {
        private const int DELAY_FRAMES = 12;
        private readonly Queue<Vector2> _positionHistory = new Queue<Vector2>();

        public string EchoName { get; private set; }
        public Interfaces.IResonantAbility Ability { get; private set; }

        public EchoEntity(string name, Texture2D spritesheet, Interfaces.IResonantAbility ability)
            : base(Vector2.Zero, 3) 
        {
            Sprite = spritesheet;
            EchoName = name;
            Ability = ability;
        }

        /// <summary>
        /// Call this every frame with the Player's current position to record history.
        /// Then advances the Echo's position.
        /// </summary>
        public void UpdateFollowerLogic(Vector2 targetPlayerPosition, GameTime gameTime)
        {
            // Record target position
            _positionHistory.Enqueue(targetPlayerPosition);

            // Wait until we buffer enough frames before moving to ensure physical distance
            if (_positionHistory.Count > DELAY_FRAMES)
            {
                // Dequeue oldest position and move towards it
                Vector2 targetPos = _positionHistory.Dequeue();
                
                // We offset it slightly above and to the left/right of the exact player pixel if needed.
                // For a tighter follow, just snap (or lerp) to `targetPos`.
                this.Position = targetPos;
                
                // Calculate simple direction for sprite flipping if necessary (assuming base Entity has sprite logic)
                if (targetPlayerPosition.X < this.Position.X)
                {
                    // face left
                }
                else if (targetPlayerPosition.X > this.Position.X)
                {
                    // face right
                }
            }
        }
    }
}
