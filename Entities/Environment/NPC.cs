// ============================================================================
// NPC.cs — Non-Player Character with dialogue and quest flags
// Author: Mehdi Lakhouane
// Description: An interactive character that the player can talk to.
//              Supports conditional dialogue based on quest flags,
//              idle animation, and facing the player during interaction.
// ============================================================================

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheGame.Core;
using TheGame.UI;
using TheGame.Utils;

namespace TheGame.Entities.Environment
{
    public class NPC : Interactable
    {
        public string Name { get; }
        public string Id { get; }

        private Texture2D _sprite;
        private readonly List<DialogueBox.DialoguePage> _defaultDialogue;
        private List<DialogueBox.DialoguePage> _questCompleteDialogue;
        private Func<bool> _questCheck;
        private float _bobTimer;
        private bool _hasSpokenOnce;

        // Callback for when the NPC's dialogue completes
        public Action OnInteracted { get; set; }

        public NPC(string id, string name, Vector2 position,
            List<DialogueBox.DialoguePage> dialogue)
        {
            Id = id;
            Name = name;
            Position = position;
            Width = 32;
            Height = 32;
            InteractRange = 20f;
            _defaultDialogue = dialogue;
        }

        /// <summary>
        /// Sets alternate dialogue shown after a quest condition is met.
        /// </summary>
        public void SetQuestDialogue(List<DialogueBox.DialoguePage> dialogue, Func<bool> check)
        {
            _questCompleteDialogue = dialogue;
            _questCheck = check;
        }

        public void SetSprite(Texture2D sprite) => _sprite = sprite;

        public override List<DialogueBox.DialoguePage> GetDialogue()
        {
            _hasSpokenOnce = true;

            // Check if quest condition is met
            if (_questCheck != null && _questCheck() && _questCompleteDialogue != null)
                return _questCompleteDialogue;

            return _defaultDialogue;
        }

        public override void OnDialogueComplete()
        {
            OnInteracted?.Invoke();
        }

        public override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _bobTimer += dt * 2f;
        }

        public override void Draw(SpriteBatch sb)
        {
            Texture2D px = Game1.PixelTexture;
            int x = (int)Position.X;
            int y = (int)Position.Y;

            // Gentle idle bob
            int bobY = y + (int)(MathF.Sin(_bobTimer) * 2f);

            // Shadow (ellipse-like)
            int shadowW = (int)(Width * 0.7f);
            int shadowH = 6;
            sb.Draw(px, new Rectangle(x + Width / 2 - shadowW / 2, y + Height - 2, shadowW, shadowH),
                Color.Black * 0.2f);

            if (_sprite != null)
            {
                sb.Draw(_sprite, new Rectangle(x, bobY, Width, Height), Color.White);
            }
            else
            {
                bool isFemale = IsFemaleNPC();
                int cx = x + Width / 2;

                if (isFemale)
                    DrawFemaleCharacter(sb, px, cx, bobY);
                else
                    DrawMaleCharacter(sb, px, cx, bobY);
            }

            // Name tag above head
            if (ShowPrompt)
            {
                int nameW = PixelFont.MeasureWidth(Name, 1);
                int nameX = x + Width / 2 - nameW / 2;
                // Background panel for readability
                sb.Draw(px, new Rectangle(nameX - 2, bobY - 14, nameW + 4, 10), Color.Black * 0.5f);
                PixelFont.DrawString(sb, Name, nameX, bobY - 13, Color.Gold, 1);
            }

            base.Draw(sb);
        }

        private bool IsFemaleNPC()
        {
            string lower = Id.ToLower();
            return lower == "mom" || lower == "mother" || lower == "queen"
                || lower == "witch" || lower == "priestess" || lower == "wife"
                || lower == "sister" || lower == "daughter" || lower == "grandma"
                || lower == "nurse" || lower == "lady" || lower == "girl";
        }

        private void DrawFemaleCharacter(SpriteBatch sb, Texture2D px, int cx, int by)
        {
            Color skinColor = new Color(230, 190, 150);
            Color hairColor = new Color(130, 60, 30);  // auburn
            Color dressColor = new Color(140, 60, 100); // purple-ish dress
            Color dressAccent = new Color(160, 80, 120);

            // Long hair (flows down sides)
            sb.Draw(px, new Rectangle(cx - 8, by - 2, 16, 8), hairColor);
            sb.Draw(px, new Rectangle(cx - 9, by + 4, 4, 14), hairColor);  // left strand
            sb.Draw(px, new Rectangle(cx + 5, by + 4, 4, 14), hairColor);  // right strand
            // Hair highlight
            sb.Draw(px, new Rectangle(cx - 4, by - 1, 6, 3), new Color(160, 85, 45));

            // Head
            sb.Draw(px, new Rectangle(cx - 6, by + 3, 12, 10), skinColor);
            // Eyes (with lashes)
            sb.Draw(px, new Rectangle(cx - 4, by + 6, 2, 2), new Color(40, 60, 40));
            sb.Draw(px, new Rectangle(cx + 2, by + 6, 2, 2), new Color(40, 60, 40));
            sb.Draw(px, new Rectangle(cx - 5, by + 5, 3, 1), new Color(50, 40, 35)); // left lash
            sb.Draw(px, new Rectangle(cx + 2, by + 5, 3, 1), new Color(50, 40, 35)); // right lash
            // Blush
            sb.Draw(px, new Rectangle(cx - 5, by + 8, 2, 1), new Color(240, 160, 150));
            sb.Draw(px, new Rectangle(cx + 3, by + 8, 2, 1), new Color(240, 160, 150));
            // Lips
            sb.Draw(px, new Rectangle(cx - 1, by + 10, 3, 1), new Color(200, 100, 90));

            // Neck
            sb.Draw(px, new Rectangle(cx - 2, by + 13, 4, 2), new Color(220, 180, 140));

            // Dress (top — fitted)
            sb.Draw(px, new Rectangle(cx - 7, by + 14, 14, 6), dressColor);
            // Dress (bottom — flared)
            sb.Draw(px, new Rectangle(cx - 9, by + 20, 18, 8), dressColor);
            sb.Draw(px, new Rectangle(cx - 10, by + 24, 20, 4), dressAccent);
            // Dress neckline
            sb.Draw(px, new Rectangle(cx - 2, by + 14, 4, 2), new Color(220, 180, 140));
            // Dress hem detail
            sb.Draw(px, new Rectangle(cx - 9, by + 27, 18, 1), new Color(120, 45, 80));

            // Shoes
            sb.Draw(px, new Rectangle(cx - 7, by + 28, 5, 3), new Color(80, 40, 50));
            sb.Draw(px, new Rectangle(cx + 2, by + 28, 5, 3), new Color(80, 40, 50));
        }

        private void DrawMaleCharacter(SpriteBatch sb, Texture2D px, int cx, int by)
        {
            // Hair
            sb.Draw(px, new Rectangle(cx - 7, by + 0, 14, 4), new Color(90, 55, 30));
            // Head
            sb.Draw(px, new Rectangle(cx - 6, by + 3, 12, 10), new Color(230, 190, 150));
            // Eyes
            sb.Draw(px, new Rectangle(cx - 4, by + 6, 2, 2), new Color(40, 40, 50));
            sb.Draw(px, new Rectangle(cx + 2, by + 6, 2, 2), new Color(40, 40, 50));
            // Mouth
            sb.Draw(px, new Rectangle(cx - 1, by + 10, 3, 1), new Color(180, 120, 100));
            // Neck
            sb.Draw(px, new Rectangle(cx - 2, by + 13, 4, 2), new Color(220, 180, 140));
            // Body / shirt
            sb.Draw(px, new Rectangle(cx - 8, by + 14, 16, 10), new Color(70, 120, 170));
            // Belt
            sb.Draw(px, new Rectangle(cx - 7, by + 23, 14, 2), new Color(100, 70, 40));
            // Legs
            sb.Draw(px, new Rectangle(cx - 6, by + 25, 5, 6), new Color(80, 65, 50));
            sb.Draw(px, new Rectangle(cx + 1, by + 25, 5, 6), new Color(80, 65, 50));
            // Boots
            sb.Draw(px, new Rectangle(cx - 7, by + 30, 6, 3), new Color(60, 40, 25));
            sb.Draw(px, new Rectangle(cx + 1, by + 30, 6, 3), new Color(60, 40, 25));
        }
    }
}
