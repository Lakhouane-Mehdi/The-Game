// ============================================================================
// NPC.cs — Non-Player Character with dialogue and quest flags
// Author: Mehdi Lakhouane
// Description: An interactive character that the player can talk to.
//              Supports conditional dialogue based on quest flags,
//              idle animation, and facing the player during interaction.
//              Each NPC gets a unique appearance based on their ID.
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

        // Per-NPC appearance (set from ID hash)
        private readonly NPCAppearance _appearance;

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
            _appearance = NPCAppearance.FromId(id);
        }

        public void SetQuestDialogue(List<DialogueBox.DialoguePage> dialogue, Func<bool> check)
        {
            _questCompleteDialogue = dialogue;
            _questCheck = check;
        }

        public void SetSprite(Texture2D sprite) => _sprite = sprite;

        /// <summary>
        /// Returns the NPC's solid collision rectangle (for wall-like blocking).
        /// </summary>
        public Rectangle CollisionBox => new Rectangle(
            (int)Position.X + 4, (int)Position.Y + Height / 2,
            Width - 8, Height / 2);

        public override List<DialogueBox.DialoguePage> GetDialogue()
        {
            _hasSpokenOnce = true;
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

            // Shadow
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
                int cx = x + Width / 2;
                if (_appearance.IsFemale)
                    DrawFemaleCharacter(sb, px, cx, bobY, _appearance);
                else
                    DrawMaleCharacter(sb, px, cx, bobY, _appearance);
            }

            // Name tag above head
            if (ShowPrompt)
            {
                int nameW = PixelFont.MeasureWidth(Name, 1);
                int nameX = x + Width / 2 - nameW / 2;
                sb.Draw(px, new Rectangle(nameX - 2, bobY - 16, nameW + 4, 12), Color.Black * 0.5f);
                PixelFont.DrawString(sb, Name, nameX, bobY - 14, Color.Gold, 1);
            }

            base.Draw(sb);
        }

        private static void DrawMaleCharacter(SpriteBatch sb, Texture2D px, int cx, int by, NPCAppearance a)
        {
            int oy = by + 6;
            // Hair
            sb.Draw(px, new Rectangle(cx - 9, oy + 0, 18, 5), a.HairColor);
            // Head
            sb.Draw(px, new Rectangle(cx - 8, oy + 4, 16, 12), a.SkinColor);
            // Eyes
            sb.Draw(px, new Rectangle(cx - 5, oy + 8, 3, 3), a.EyeColor);
            sb.Draw(px, new Rectangle(cx + 3, oy + 8, 3, 3), a.EyeColor);
            // Mouth
            sb.Draw(px, new Rectangle(cx - 2, oy + 13, 4, 1), new Color(180, 120, 100));
            // Neck
            sb.Draw(px, new Rectangle(cx - 2, oy + 16, 5, 2), a.SkinAccent);
            // Body / shirt
            sb.Draw(px, new Rectangle(cx - 10, oy + 17, 20, 12), a.ShirtColor);
            // Shirt detail (collar or stripe)
            sb.Draw(px, new Rectangle(cx - 3, oy + 17, 6, 3), a.ShirtAccent);
            // Belt
            sb.Draw(px, new Rectangle(cx - 9, oy + 28, 18, 2), a.BeltColor);
            // Legs
            sb.Draw(px, new Rectangle(cx - 8, oy + 30, 7, 7), a.PantsColor);
            sb.Draw(px, new Rectangle(cx + 1, oy + 30, 7, 7), a.PantsColor);
            // Boots
            sb.Draw(px, new Rectangle(cx - 9, oy + 36, 8, 4), a.BootColor);
            sb.Draw(px, new Rectangle(cx + 1, oy + 36, 8, 4), a.BootColor);

            // Hat (some NPCs)
            if (a.HasHat)
            {
                sb.Draw(px, new Rectangle(cx - 11, oy - 4, 22, 5), a.HatColor);
                sb.Draw(px, new Rectangle(cx - 8, oy - 7, 16, 4), a.HatColor);
            }

            // Beard (some NPCs)
            if (a.HasBeard)
            {
                sb.Draw(px, new Rectangle(cx - 6, oy + 12, 12, 5), a.HairColor * 0.8f);
                sb.Draw(px, new Rectangle(cx - 4, oy + 16, 8, 3), a.HairColor * 0.6f);
            }
        }

        private static void DrawFemaleCharacter(SpriteBatch sb, Texture2D px, int cx, int by, NPCAppearance a)
        {
            int oy = by + 6;
            // Long hair
            sb.Draw(px, new Rectangle(cx - 9, oy - 2, 18, 8), a.HairColor);
            sb.Draw(px, new Rectangle(cx - 11, oy + 4, 4, 14), a.HairColor);
            sb.Draw(px, new Rectangle(cx + 7, oy + 4, 4, 14), a.HairColor);
            // Hair highlight
            sb.Draw(px, new Rectangle(cx - 5, oy - 1, 8, 4), a.HairAccent);
            // Head
            sb.Draw(px, new Rectangle(cx - 8, oy + 4, 16, 12), a.SkinColor);
            // Eyes (with lashes)
            sb.Draw(px, new Rectangle(cx - 5, oy + 8, 3, 3), a.EyeColor);
            sb.Draw(px, new Rectangle(cx + 3, oy + 8, 3, 3), a.EyeColor);
            sb.Draw(px, new Rectangle(cx - 6, oy + 7, 4, 1), new Color(50, 40, 35));
            sb.Draw(px, new Rectangle(cx + 3, oy + 7, 4, 1), new Color(50, 40, 35));
            // Blush
            sb.Draw(px, new Rectangle(cx - 6, oy + 11, 3, 1), new Color(240, 160, 150));
            sb.Draw(px, new Rectangle(cx + 4, oy + 11, 3, 1), new Color(240, 160, 150));
            // Lips
            sb.Draw(px, new Rectangle(cx - 2, oy + 13, 4, 1), new Color(200, 100, 90));
            // Neck
            sb.Draw(px, new Rectangle(cx - 2, oy + 16, 5, 2), a.SkinAccent);
            // Dress top
            sb.Draw(px, new Rectangle(cx - 10, oy + 17, 20, 8), a.ShirtColor);
            sb.Draw(px, new Rectangle(cx - 3, oy + 17, 6, 3), a.SkinAccent);
            // Dress bottom
            sb.Draw(px, new Rectangle(cx - 11, oy + 25, 22, 9), a.ShirtColor);
            sb.Draw(px, new Rectangle(cx - 12, oy + 30, 24, 4), a.ShirtAccent);
            sb.Draw(px, new Rectangle(cx - 11, oy + 33, 22, 1), a.BeltColor);
            // Shoes
            sb.Draw(px, new Rectangle(cx - 9, oy + 34, 8, 4), a.BootColor);
            sb.Draw(px, new Rectangle(cx + 1, oy + 34, 8, 4), a.BootColor);

            // Hair accessory (some NPCs)
            if (a.HasHat)
            {
                sb.Draw(px, new Rectangle(cx + 5, oy - 1, 6, 5), a.HatColor);
            }
        }
    }

    /// <summary>
    /// Defines unique visual appearance for each NPC based on their ID.
    /// </summary>
    internal struct NPCAppearance
    {
        public Color HairColor, HairAccent;
        public Color SkinColor, SkinAccent;
        public Color EyeColor;
        public Color ShirtColor, ShirtAccent;
        public Color PantsColor;
        public Color BeltColor;
        public Color BootColor;
        public Color HatColor;
        public bool HasHat, HasBeard, IsFemale;

        public static NPCAppearance FromId(string id)
        {
            string lower = id.ToLower();

            // Default male appearance
            var a = new NPCAppearance
            {
                SkinColor = new Color(230, 190, 150),
                SkinAccent = new Color(220, 180, 140),
                EyeColor = new Color(40, 40, 50),
                HairColor = new Color(90, 55, 30),
                HairAccent = new Color(120, 75, 45),
                ShirtColor = new Color(70, 120, 170),
                ShirtAccent = new Color(90, 140, 190),
                PantsColor = new Color(80, 65, 50),
                BeltColor = new Color(100, 70, 40),
                BootColor = new Color(60, 40, 25),
                HatColor = Color.Brown,
            };

            // Determine gender
            a.IsFemale = lower.Contains("mother") || lower.Contains("mom")
                || lower.Contains("herbalist") || lower.Contains("mira")
                || lower.Contains("nara") || lower.Contains("saya")
                || lower.Contains("lena") || lower.Contains("vera")
                || lower.Contains("girl") || lower.Contains("wife")
                || lower.Contains("queen") || lower.Contains("witch")
                || lower.Contains("priestess") || lower.Contains("nurse")
                || lower.Contains("lady") || lower.Contains("sister");

            // Unique palettes per NPC
            switch (lower)
            {
                case "old_man_elam":
                    a.HairColor = new Color(180, 175, 170); // grey hair
                    a.HairAccent = new Color(200, 195, 190);
                    a.ShirtColor = new Color(100, 80, 60);  // brown robes
                    a.ShirtAccent = new Color(130, 100, 70);
                    a.PantsColor = new Color(70, 55, 40);
                    a.HasBeard = true;
                    break;

                case "village_elder":
                    a.HairColor = new Color(200, 200, 200); // white hair
                    a.HairAccent = new Color(220, 215, 210);
                    a.ShirtColor = new Color(140, 50, 50);  // deep red robes
                    a.ShirtAccent = new Color(180, 80, 60);
                    a.PantsColor = new Color(60, 40, 35);
                    a.BeltColor = new Color(150, 120, 40);  // gold belt
                    a.HasBeard = true;
                    a.HasHat = true;
                    a.HatColor = new Color(140, 50, 50);
                    break;

                case "village_guard":
                    a.HairColor = new Color(40, 35, 30);    // dark hair
                    a.HairAccent = new Color(60, 50, 40);
                    a.ShirtColor = new Color(80, 90, 100);  // steel armor
                    a.ShirtAccent = new Color(120, 130, 140);
                    a.PantsColor = new Color(60, 65, 70);
                    a.BeltColor = new Color(80, 80, 80);
                    a.BootColor = new Color(50, 50, 55);
                    a.HasHat = true;
                    a.HatColor = new Color(90, 95, 100);    // helmet
                    break;

                case "village_child":
                    a.IsFemale = true;
                    a.HairColor = new Color(220, 180, 100); // blonde
                    a.HairAccent = new Color(240, 200, 120);
                    a.SkinColor = new Color(240, 200, 165);
                    a.ShirtColor = new Color(255, 180, 100); // orange dress
                    a.ShirtAccent = new Color(255, 200, 130);
                    a.BootColor = new Color(140, 90, 50);
                    break;

                case "herbalist":
                    a.IsFemale = true;
                    a.HairColor = new Color(50, 100, 50);   // green-tinted hair
                    a.HairAccent = new Color(70, 130, 70);
                    a.ShirtColor = new Color(60, 130, 80);  // green dress
                    a.ShirtAccent = new Color(80, 160, 100);
                    a.BootColor = new Color(80, 100, 60);
                    a.HasHat = true;
                    a.HatColor = new Color(200, 180, 50);   // flower in hair
                    break;

                case "village_mother":
                    a.IsFemale = true;
                    a.HairColor = new Color(100, 60, 30);   // brown hair
                    a.HairAccent = new Color(140, 85, 45);
                    a.ShirtColor = new Color(100, 80, 140);  // purple dress
                    a.ShirtAccent = new Color(130, 100, 170);
                    a.BootColor = new Color(70, 50, 40);
                    break;

                case "wandering_merchant":
                    a.HairColor = new Color(60, 50, 40);    // dark brown
                    a.HairAccent = new Color(80, 65, 50);
                    a.ShirtColor = new Color(150, 100, 50);  // merchant gold
                    a.ShirtAccent = new Color(180, 130, 60);
                    a.PantsColor = new Color(60, 50, 40);
                    a.BeltColor = new Color(160, 130, 50);
                    a.HasHat = true;
                    a.HatColor = new Color(130, 90, 40);    // wide-brim hat
                    break;

                case "lost_scholar":
                    a.IsFemale = true;
                    a.HairColor = new Color(30, 30, 60);    // dark blue-black
                    a.HairAccent = new Color(50, 50, 90);
                    a.ShirtColor = new Color(60, 60, 120);  // blue scholar robes
                    a.ShirtAccent = new Color(80, 80, 150);
                    a.EyeColor = new Color(40, 50, 80);
                    a.BootColor = new Color(40, 40, 60);
                    break;

                case "silent_guard":
                    a.HairColor = new Color(50, 50, 50);
                    a.HairAccent = new Color(70, 70, 70);
                    a.ShirtColor = new Color(40, 45, 50);   // dark armor
                    a.ShirtAccent = new Color(60, 65, 70);
                    a.PantsColor = new Color(35, 35, 40);
                    a.BeltColor = new Color(50, 50, 55);
                    a.BootColor = new Color(30, 30, 35);
                    a.SkinColor = new Color(200, 180, 160); // pale
                    a.HasHat = true;
                    a.HatColor = new Color(40, 45, 50);
                    break;

                case "mysterious_pot":
                    // The pot is special — draw as a pot
                    a.ShirtColor = new Color(160, 100, 60);
                    a.ShirtAccent = new Color(180, 120, 70);
                    a.HairColor = new Color(160, 100, 60);
                    break;

                // Interior NPCs (from JSON)
                default:
                    if (lower.Contains("blacksmith") || lower.Contains("toran"))
                    {
                        a.HairColor = new Color(60, 40, 30);
                        a.ShirtColor = new Color(120, 70, 40);
                        a.ShirtAccent = new Color(160, 90, 50);
                        a.PantsColor = new Color(50, 45, 40);
                        a.BeltColor = new Color(80, 60, 40);
                        a.SkinColor = new Color(200, 160, 130); // tanned
                        a.HasBeard = true;
                    }
                    else if (lower.Contains("shopkeeper") || lower.Contains("nara"))
                    {
                        a.IsFemale = true;
                        a.HairColor = new Color(180, 120, 60);
                        a.HairAccent = new Color(210, 150, 80);
                        a.ShirtColor = new Color(180, 60, 80);
                        a.ShirtAccent = new Color(210, 80, 100);
                    }
                    else if (lower.Contains("innkeeper") || lower.Contains("mira"))
                    {
                        a.IsFemale = true;
                        a.HairColor = new Color(150, 80, 50);
                        a.HairAccent = new Color(180, 100, 60);
                        a.ShirtColor = new Color(170, 130, 80);
                        a.ShirtAccent = new Color(200, 160, 100);
                    }
                    else if (lower.Contains("mom"))
                    {
                        a.IsFemale = true;
                        a.HairColor = new Color(120, 70, 40);
                        a.HairAccent = new Color(150, 90, 55);
                        a.ShirtColor = new Color(120, 100, 160);
                        a.ShirtAccent = new Color(150, 130, 190);
                    }
                    else
                    {
                        // Random-ish based on hash
                        int hash = Math.Abs(id.GetHashCode());
                        a.ShirtColor = new Color(50 + hash % 150, 50 + (hash / 3) % 150, 50 + (hash / 7) % 150);
                        a.ShirtAccent = new Color(
                            Math.Min(255, a.ShirtColor.R + 30),
                            Math.Min(255, a.ShirtColor.G + 30),
                            Math.Min(255, a.ShirtColor.B + 30));
                        a.HairColor = new Color(30 + (hash / 11) % 100, 20 + (hash / 13) % 60, 10 + (hash / 17) % 40);
                        a.HairAccent = new Color(
                            Math.Min(255, a.HairColor.R + 20),
                            Math.Min(255, a.HairColor.G + 20),
                            Math.Min(255, a.HairColor.B + 20));
                    }
                    break;
            }

            return a;
        }
    }
}
