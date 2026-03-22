// ============================================================================
// Interactable.cs — Base class for NPCs, Signs, and interactive objects
// Author: Mehdi Lakhouane
// Description: Abstract trigger entity that activates when the player is
//              within range and presses the interaction key (Space).
//              Subclasses define what happens on interaction.
// ============================================================================

using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheGame.Core;
using TheGame.UI;

namespace TheGame.Entities.Environment
{
    public abstract class Interactable
    {
        public Vector2 Position { get; set; }
        public int Width { get; protected set; } = 32;
        public int Height { get; protected set; } = 32;
        public float InteractRange { get; protected set; } = 20f;
        public bool ShowPrompt { get; private set; }

        public Rectangle BoundingBox => new Rectangle(
            (int)Position.X, (int)Position.Y, Width, Height);

        /// <summary>
        /// Returns true if the player hitbox is within interaction range.
        /// </summary>
        public bool IsPlayerInRange(Rectangle playerBB)
        {
            // Expand the bounding box by the interact range
            Rectangle zone = new Rectangle(
                BoundingBox.X - (int)InteractRange,
                BoundingBox.Y - (int)InteractRange,
                BoundingBox.Width + (int)InteractRange * 2,
                BoundingBox.Height + (int)InteractRange * 2);

            ShowPrompt = zone.Intersects(playerBB);
            return ShowPrompt;
        }

        /// <summary>
        /// Called when the player presses Space within range.
        /// Returns dialogue pages to display (or null for no dialogue).
        /// </summary>
        public abstract List<DialogueBox.DialoguePage> GetDialogue();

        /// <summary>
        /// Called after dialogue is closed. Override for quest logic.
        /// </summary>
        public virtual void OnDialogueComplete() { }

        public virtual void Update(GameTime gameTime) { }

        public virtual void Draw(SpriteBatch sb)
        {
            // Draw interaction prompt when player is in range
            if (ShowPrompt)
            {
                Texture2D px = Game1.PixelTexture;
                int promptX = (int)Position.X + Width / 2 - 8;
                int promptY = (int)Position.Y - 16;

                // Small "SPACE" hint bubble
                sb.Draw(px, new Rectangle(promptX - 2, promptY - 2, 20, 12),
                    Color.Black * 0.7f);
                Utils.PixelFont.DrawString(sb, "E", promptX + 5, promptY, Color.Gold, 1);
            }
        }
    }
}
