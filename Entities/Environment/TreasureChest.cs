// ============================================================================
// TreasureChest.cs — Openable treasure chest
// Author: Mehdi Lakhouane
// Description: A chest that can be opened once by pressing E. Awards coins
//              and/or items to the player. Uses chest.png sprite sheet.
// ============================================================================

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheGame.Core;
using TheGame.Entities.Items;
using TheGame.UI;
using TheGame.Utils;

namespace TheGame.Entities.Environment
{
    public class TreasureChest : Interactable
    {
        private readonly string _uniqueId;
        private readonly int _coinReward;
        private readonly string _itemReward;   // item ID or null
        private readonly string _rewardName;
        private bool _opened;
        private bool _rewarded;
        private Texture2D _chestSheet;

        // Chest sprite rects from chest.png (240x96, 5 columns x 2 rows, 48x48 each)
        private static readonly Rectangle ClosedRect = new(0, 0, 48, 48);
        private static readonly Rectangle OpenRect = new(96, 0, 48, 48);

        public bool IsOpened => _opened;

        public TreasureChest(Vector2 position, string uniqueId, int coinReward, string itemReward = null, string rewardName = null)
        {
            Position = position;
            Width = 32;
            Height = 32;
            InteractRange = 28f;
            _uniqueId = uniqueId;
            _coinReward = coinReward;
            _itemReward = itemReward;
            _rewardName = rewardName ?? itemReward;

            // Check if already opened from world state
            if (Systems.WorldStateManager.GetFlag($"Chest_{uniqueId}"))
                _opened = true;

            // Load chest sprite sheet
            if (AssetLoader.Exists("Tiles/Buildings/chest.png"))
                _chestSheet = AssetLoader.LoadTexture("Tiles/Buildings/chest.png");
        }

        public override List<DialogueBox.DialoguePage> GetDialogue()
        {
            if (_opened)
            {
                return new List<DialogueBox.DialoguePage>
                {
                    new() { Speaker = "CHEST", Text = "It's empty." }
                };
            }

            // Open the chest!
            _opened = true;
            Systems.WorldStateManager.SetFlag($"Chest_{_uniqueId}", true);

            // Build reward message
            string msg = "";
            if (_coinReward > 0)
                msg += $"Found {_coinReward} coins!";

            if (_itemReward != null)
            {
                if (msg.Length > 0) msg += " ";
                string name = _rewardName ?? _itemReward;
                msg += $"Got {name}!";
            }

            if (msg.Length == 0)
                msg = "The chest is empty...";

            return new List<DialogueBox.DialoguePage>
            {
                new() { Speaker = "CHEST", Text = msg }
            };
        }

        /// <summary>
        /// Called after the dialogue closes — actually award the loot.
        /// </summary>
        public override void OnDialogueComplete()
        {
            if (_rewarded) return;
            _rewarded = true;

            // Award coins
            if (_coinReward > 0 && Game1.Instance != null)
                Game1.Instance.SharedPlayer.Inventory.Coins += _coinReward;

            // Award item
            if (_itemReward != null && Game1.Instance != null)
                Game1.Instance.SharedPlayer.Inventory.AddItem(_itemReward);
        }

        public override void Draw(SpriteBatch sb)
        {
            var dest = new Rectangle((int)Position.X, (int)Position.Y, Width, Height);

            if (_chestSheet != null)
            {
                var src = _opened ? OpenRect : ClosedRect;
                sb.Draw(_chestSheet, dest, src, Color.White);
            }
            else
            {
                // Fallback procedural chest
                Texture2D px = Game1.PixelTexture;
                Color bodyColor = _opened ? new Color(90, 65, 35) : new Color(130, 90, 50);
                sb.Draw(px, dest, bodyColor);
                sb.Draw(px, new Rectangle(dest.X, dest.Y + dest.Height / 2 - 1, dest.Width, 3),
                    new Color(180, 160, 60));
                if (!_opened)
                    sb.Draw(px, new Rectangle(dest.X + dest.Width / 2 - 3, dest.Y + dest.Height / 2 - 3, 6, 6),
                        new Color(220, 200, 60));
            }

            // Sparkle on unopened chests
            if (!_opened)
            {
                Texture2D px2 = Game1.PixelTexture;
                float sparkle = 0.3f + 0.3f * MathF.Sin((float)System.Environment.TickCount64 / 300f);
                sb.Draw(px2, new Rectangle((int)Position.X + Width / 2 - 2, (int)Position.Y - 6, 4, 4),
                    Color.Gold * sparkle);
            }

            base.Draw(sb);
        }
    }
}
