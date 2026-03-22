// ============================================================================
// LockedDoor.cs — Door that requires a Key Item to open
// Author: Mehdi Lakhouane
// Description: A collision barrier that checks the player's inventory for
//              a specific key item. When unlocked, plays an animation and
//              permanently removes its collision. Unlock state is tracked
//              in a global HashSet so it persists across room visits.
// ============================================================================

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheGame.Core;
using TheGame.Utils;

namespace TheGame.Entities.Environment
{
    public class LockedDoor : Interactable
    {
        public string DoorId { get; }
        public string RequiredKeyId { get; }
        public bool IsUnlocked { get; private set; }

        // Unlock animation
        private float _unlockTimer;
        private const float UnlockDuration = 0.6f;
        private bool _unlocking;

        // Collision rect that gets removed on unlock
        public Rectangle CollisionRect => new Rectangle(
            (int)Position.X, (int)Position.Y, Width, Height);

        public LockedDoor(string doorId, Vector2 position, string requiredKeyId,
            int width = 48, int height = 16)
        {
            DoorId = doorId;
            RequiredKeyId = requiredKeyId;
            Position = position;
            Width = width;
            Height = height;
            InteractRange = 24f;
        }

        /// <summary>
        /// Marks this door as permanently unlocked.
        /// </summary>
        public void Unlock()
        {
            if (IsUnlocked) return;
            _unlocking = true;
            _unlockTimer = 0f;
        }

        /// <summary>
        /// Called when unlock animation finishes.
        /// </summary>
        public Action OnUnlocked { get; set; }

        public override System.Collections.Generic.List<UI.DialogueBox.DialoguePage> GetDialogue()
        {
            if (IsUnlocked)
            {
                return new System.Collections.Generic.List<UI.DialogueBox.DialoguePage>
                {
                    new UI.DialogueBox.DialoguePage { Text = "THE DOOR IS OPEN." }
                };
            }

            return new System.Collections.Generic.List<UI.DialogueBox.DialoguePage>
            {
                new UI.DialogueBox.DialoguePage
                {
                    Text = "THE DOOR IS LOCKED. YOU NEED A KEY."
                }
            };
        }

        /// <summary>
        /// Try to unlock with a key item. Returns true if unlocked.
        /// </summary>
        public bool TryUnlock(Items.Inventory inventory)
        {
            if (IsUnlocked) return true;
            if (inventory.HasItem(RequiredKeyId))
            {
                Unlock();
                return true;
            }
            return false;
        }

        public override void Update(GameTime gameTime)
        {
            if (_unlocking)
            {
                _unlockTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (_unlockTimer >= UnlockDuration)
                {
                    _unlocking = false;
                    IsUnlocked = true;
                    OnUnlocked?.Invoke();
                }
            }
        }

        public override void Draw(SpriteBatch sb)
        {
            if (IsUnlocked) return; // invisible once opened

            Texture2D px = Game1.PixelTexture;
            int x = (int)Position.X;
            int y = (int)Position.Y;

            if (_unlocking)
            {
                // Unlock animation: door splits apart
                float t = _unlockTimer / UnlockDuration;
                int offset = (int)(t * 20f);
                float alpha = 1f - t;

                // Left half
                sb.Draw(px, new Rectangle(x - offset, y, Width / 2, Height),
                    new Color(100, 70, 40) * alpha);
                // Right half
                sb.Draw(px, new Rectangle(x + Width / 2 + offset, y, Width / 2, Height),
                    new Color(100, 70, 40) * alpha);
            }
            else
            {
                // Solid locked door
                sb.Draw(px, new Rectangle(x, y, Width, Height), new Color(80, 55, 35));
                // Lock icon
                sb.Draw(px, new Rectangle(x + Width / 2 - 4, y + 2, 8, 8), new Color(180, 160, 40));
                sb.Draw(px, new Rectangle(x + Width / 2 - 2, y + 4, 4, 4), new Color(60, 40, 20));
                // Keyhole
                sb.Draw(px, new Rectangle(x + Width / 2 - 1, y + Height / 2, 2, 4), Color.Black);
            }

            // Show prompt when in range
            if (ShowPrompt && !_unlocking)
            {
                PixelFont.DrawString(sb, "E", x + Width / 2 - 3, y - 14, Color.Gold, 1);
            }
        }
    }
}
