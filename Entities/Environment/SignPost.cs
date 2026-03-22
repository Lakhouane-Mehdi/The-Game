// ============================================================================
// SignPost.cs — Readable sign entity
// Author: Mehdi Lakhouane
// Description: A static world sign that displays text when interacted with.
// ============================================================================

using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheGame.Core;
using TheGame.UI;

namespace TheGame.Entities.Environment
{
    public class SignPost : Interactable
    {
        private readonly List<DialogueBox.DialoguePage> _pages;
        private Texture2D _sprite;

        public SignPost(Vector2 position, string text, string speaker = null)
        {
            Position = position;
            Width = 28;
            Height = 28;
            InteractRange = 24f;

            _pages = new List<DialogueBox.DialoguePage>
            {
                new DialogueBox.DialoguePage { Speaker = speaker ?? "SIGN", Text = text }
            };
        }

        public SignPost(Vector2 position, List<DialogueBox.DialoguePage> pages)
        {
            Position = position;
            Width = 28;
            Height = 28;
            InteractRange = 24f;
            _pages = pages;
        }

        public void SetSprite(Texture2D sprite) => _sprite = sprite;

        public override List<DialogueBox.DialoguePage> GetDialogue() => _pages;

        public override void Draw(SpriteBatch sb)
        {
            Texture2D px = Game1.PixelTexture;

            if (_sprite != null)
            {
                sb.Draw(_sprite, new Rectangle((int)Position.X, (int)Position.Y, Width, Height),
                    Color.White);
            }
            else
            {
                // Fallback: pixel-art sign post
                int x = (int)Position.X;
                int y = (int)Position.Y;

                // Post
                sb.Draw(px, new Rectangle(x + 12, y + 16, 4, 14), new Color(100, 70, 40));
                // Board
                sb.Draw(px, new Rectangle(x + 2, y + 2, 24, 16), new Color(140, 95, 50));
                sb.Draw(px, new Rectangle(x + 3, y + 3, 22, 14), new Color(180, 130, 70));
                // Text lines on board
                sb.Draw(px, new Rectangle(x + 6, y + 6, 16, 2), new Color(80, 55, 30));
                sb.Draw(px, new Rectangle(x + 6, y + 10, 12, 2), new Color(80, 55, 30));
            }

            base.Draw(sb);
        }
    }
}
