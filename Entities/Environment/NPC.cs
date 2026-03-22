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
            Height = 48;
            InteractRange = 22f;
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
            int shadowW = (int)(Width * 0.8f);
            int shadowH = 8;
            sb.Draw(px, new Rectangle(x + Width / 2 - shadowW / 2, y + Height - 4, shadowW, shadowH),
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
                sb.Draw(px, new Rectangle(nameX - 2, bobY - 16, nameW + 4, 12), Color.Black * 0.5f);
                PixelFont.DrawString(sb, Name, nameX, bobY - 14, Color.Gold, 1);
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
            // Offset to center in bounding box (match male sizing ~40px tall)
            int oy = by + 6;
            Color skinColor = new Color(230, 190, 150);
            Color hairColor = new Color(130, 60, 30);  // auburn
            Color dressColor = new Color(140, 60, 100); // purple-ish dress
            Color dressAccent = new Color(160, 80, 120);

            // Long hair (flows down sides)
            sb.Draw(px, new Rectangle(cx - 9, oy - 2, 18, 8), hairColor);
            sb.Draw(px, new Rectangle(cx - 11, oy + 4, 4, 14), hairColor);  // left strand
            sb.Draw(px, new Rectangle(cx + 7, oy + 4, 4, 14), hairColor);   // right strand
            // Hair highlight
            sb.Draw(px, new Rectangle(cx - 5, oy - 1, 8, 4), new Color(160, 85, 45));

            // Head
            sb.Draw(px, new Rectangle(cx - 8, oy + 4, 16, 12), skinColor);
            // Eyes (with lashes)
            sb.Draw(px, new Rectangle(cx - 5, oy + 8, 3, 3), new Color(40, 60, 40));
            sb.Draw(px, new Rectangle(cx + 3, oy + 8, 3, 3), new Color(40, 60, 40));
            sb.Draw(px, new Rectangle(cx - 6, oy + 7, 4, 1), new Color(50, 40, 35)); // left lash
            sb.Draw(px, new Rectangle(cx + 3, oy + 7, 4, 1), new Color(50, 40, 35)); // right lash
            // Blush
            sb.Draw(px, new Rectangle(cx - 6, oy + 11, 3, 1), new Color(240, 160, 150));
            sb.Draw(px, new Rectangle(cx + 4, oy + 11, 3, 1), new Color(240, 160, 150));
            // Lips
            sb.Draw(px, new Rectangle(cx - 2, oy + 13, 4, 1), new Color(200, 100, 90));

            // Neck
            sb.Draw(px, new Rectangle(cx - 2, oy + 16, 5, 2), new Color(220, 180, 140));

            // Dress (top — fitted)
            sb.Draw(px, new Rectangle(cx - 10, oy + 17, 20, 8), dressColor);
            // Dress neckline
            sb.Draw(px, new Rectangle(cx - 3, oy + 17, 6, 3), new Color(220, 180, 140));
            // Dress (bottom — flared)
            sb.Draw(px, new Rectangle(cx - 11, oy + 25, 22, 9), dressColor);
            sb.Draw(px, new Rectangle(cx - 12, oy + 30, 24, 4), dressAccent);
            // Dress hem detail
            sb.Draw(px, new Rectangle(cx - 11, oy + 33, 22, 1), new Color(120, 45, 80));

            // Shoes
            sb.Draw(px, new Rectangle(cx - 9, oy + 34, 8, 4), new Color(80, 40, 50));
            sb.Draw(px, new Rectangle(cx + 1, oy + 34, 8, 4), new Color(80, 40, 50));
        }

        private void DrawMaleCharacter(SpriteBatch sb, Texture2D px, int cx, int by)
        {
            // Offset to center in bounding box (start drawing ~8px down)
            int oy = by + 6;
            // Hair
            sb.Draw(px, new Rectangle(cx - 9, oy + 0, 18, 5), new Color(90, 55, 30));
            // Head
            sb.Draw(px, new Rectangle(cx - 8, oy + 4, 16, 12), new Color(230, 190, 150));
            // Eyes
            sb.Draw(px, new Rectangle(cx - 5, oy + 8, 3, 3), new Color(40, 40, 50));
            sb.Draw(px, new Rectangle(cx + 3, oy + 8, 3, 3), new Color(40, 40, 50));
            // Mouth
            sb.Draw(px, new Rectangle(cx - 2, oy + 13, 4, 1), new Color(180, 120, 100));
            // Neck
            sb.Draw(px, new Rectangle(cx - 2, oy + 16, 5, 2), new Color(220, 180, 140));
            // Body / shirt
            sb.Draw(px, new Rectangle(cx - 10, oy + 17, 20, 12), new Color(70, 120, 170));
            // Belt
            sb.Draw(px, new Rectangle(cx - 9, oy + 28, 18, 2), new Color(100, 70, 40));
            // Legs
            sb.Draw(px, new Rectangle(cx - 8, oy + 30, 7, 7), new Color(80, 65, 50));
            sb.Draw(px, new Rectangle(cx + 1, oy + 30, 7, 7), new Color(80, 65, 50));
            // Boots
            sb.Draw(px, new Rectangle(cx - 9, oy + 36, 8, 4), new Color(60, 40, 25));
            sb.Draw(px, new Rectangle(cx + 1, oy + 36, 8, 4), new Color(60, 40, 25));
        }
    }
}
