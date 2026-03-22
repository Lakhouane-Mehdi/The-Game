using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TheGame.Core;
using TheGame.Entities.Items;
using TheGame.Utils;

namespace TheGame.States
{
    public class InventoryState : GameStateBase
    {
        private int _cursorX = 0;
        private int _cursorY = 0;
        private const int GridWidth = 5;
        private const int GridHeight = 3;
        private const int SlotSize = 52;
        private const int Padding = 10;

        private Texture2D _panelTex;
        private Texture2D _selectionTex;
        private KeyboardState _prevKb = Keyboard.GetState();
        private float _animTimer;

        public InventoryState(Game1 game, ContentManager content) : base(game, content)
        {
            _panelTex = AssetLoader.CreatePixel(new Color(25, 22, 35, 220));
            _selectionTex = AssetLoader.CreatePixel(new Color(255, 215, 0, 150));
        }

        public override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _animTimer += dt;
            KeyboardState kb = Keyboard.GetState();

            // Navigation
            if (kb.IsKeyDown(Keys.W) && _prevKb.IsKeyUp(Keys.W)) _cursorY = Math.Max(0, _cursorY - 1);
            if (kb.IsKeyDown(Keys.S) && _prevKb.IsKeyUp(Keys.S)) _cursorY = Math.Min(GridHeight - 1, _cursorY + 1);
            if (kb.IsKeyDown(Keys.A) && _prevKb.IsKeyUp(Keys.A)) _cursorX = Math.Max(0, _cursorX - 1);
            if (kb.IsKeyDown(Keys.D) && _prevKb.IsKeyUp(Keys.D)) _cursorX = Math.Min(GridWidth - 1, _cursorX + 1);

            // Equip / Interact
            if (kb.IsKeyDown(Keys.Space) && _prevKb.IsKeyUp(Keys.Space))
            {
                int index = _cursorY * GridWidth + _cursorX;
                var items = GameRef.SharedPlayer.Inventory.Items;
                if (index < items.Count)
                {
                    var item = items[index];
                    // Only equip weapons/key items, not resources
                    if (item.Type == ItemType.Weapon || item.Type == ItemType.KeyItem)
                        GameRef.SharedPlayer.Inventory.SetEquippedIndex(index);
                }
            }

            // Exit
            if ((kb.IsKeyDown(Keys.Escape) && _prevKb.IsKeyUp(Keys.Escape)) ||
                (kb.IsKeyDown(Keys.Enter) && _prevKb.IsKeyUp(Keys.Enter)) ||
                (kb.IsKeyDown(Keys.I) && _prevKb.IsKeyUp(Keys.I)))
            {
                GameRef.ResumeFromPause();
            }

            _prevKb = kb;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            Texture2D px = Game1.PixelTexture;

            // Dim background
            spriteBatch.Draw(px, new Rectangle(0, 0, Game1.ScreenWidth, Game1.ScreenHeight), Color.Black * 0.6f);

            var items = GameRef.SharedPlayer.Inventory.Items;
            int totalGridW = GridWidth * (SlotSize + Padding) - Padding;
            int totalGridH = GridHeight * (SlotSize + Padding) - Padding;
            int panelPad = 20;
            int headerH = 30;
            int descH = 90;
            int controlsH = 22;
            int panelW = totalGridW + panelPad * 2;
            int panelH = headerH + totalGridH + descH + controlsH + panelPad * 2;
            int panelX = Game1.ScreenWidth / 2 - panelW / 2;
            int panelY = Game1.ScreenHeight / 2 - panelH / 2;

            // ── Panel background with border ──
            spriteBatch.Draw(px, new Rectangle(panelX - 2, panelY - 2, panelW + 4, panelH + 4), new Color(120, 100, 60) * 0.8f);
            spriteBatch.Draw(px, new Rectangle(panelX, panelY, panelW, panelH), new Color(25, 22, 35, 220));

            // ── Title ──
            PixelFont.DrawCentered(spriteBatch, "INVENTORY", panelY + 8, Color.Gold, 2);

            // Separator under title
            int gridStartX = panelX + panelPad;
            int gridStartY = panelY + headerH + panelPad;
            spriteBatch.Draw(px, new Rectangle(panelX + 8, gridStartY - 8, panelW - 16, 1), Color.White * 0.2f);

            // ── Item grid ──
            for (int y = 0; y < GridHeight; y++)
            {
                for (int x = 0; x < GridWidth; x++)
                {
                    int slotX = gridStartX + x * (SlotSize + Padding);
                    int slotY = gridStartY + y * (SlotSize + Padding);
                    int index = y * GridWidth + x;

                    // Slot background
                    Color slotBg = new Color(15, 12, 20);
                    if (index < items.Count)
                    {
                        // Tint by category
                        var item = items[index];
                        if (item.Type == ItemType.Resource)
                            slotBg = new Color(20, 25, 15);
                        else if (item.Type == ItemType.KeyItem)
                            slotBg = new Color(25, 20, 10);
                    }
                    spriteBatch.Draw(px, new Rectangle(slotX, slotY, SlotSize, SlotSize), slotBg);

                    // Slot border
                    Color borderColor = Color.White * 0.15f;
                    spriteBatch.Draw(px, new Rectangle(slotX, slotY, SlotSize, 1), borderColor);
                    spriteBatch.Draw(px, new Rectangle(slotX, slotY + SlotSize - 1, SlotSize, 1), borderColor);
                    spriteBatch.Draw(px, new Rectangle(slotX, slotY, 1, SlotSize), borderColor);
                    spriteBatch.Draw(px, new Rectangle(slotX + SlotSize - 1, slotY, 1, SlotSize), borderColor);

                    // Equipped indicator (blue corner)
                    if (index == GameRef.SharedPlayer.Inventory.EquippedIndex)
                    {
                        spriteBatch.Draw(px, new Rectangle(slotX, slotY, SlotSize, 2), Color.Cyan * 0.8f);
                        spriteBatch.Draw(px, new Rectangle(slotX, slotY, 2, SlotSize), Color.Cyan * 0.8f);
                        spriteBatch.Draw(px, new Rectangle(slotX, slotY + SlotSize - 2, SlotSize, 2), Color.Cyan * 0.8f);
                        spriteBatch.Draw(px, new Rectangle(slotX + SlotSize - 2, slotY, 2, SlotSize), Color.Cyan * 0.8f);
                        // "E" badge
                        PixelFont.DrawString(spriteBatch, "E", slotX + 2, slotY + 2, Color.Cyan, 1);
                    }

                    // Selection cursor (animated gold)
                    if (x == _cursorX && y == _cursorY)
                    {
                        float pulse = 0.7f + 0.3f * MathF.Sin(_animTimer * 4f);
                        Color selColor = Color.Gold * pulse;
                        int t = 2;
                        spriteBatch.Draw(px, new Rectangle(slotX - t, slotY - t, SlotSize + t * 2, t), selColor);
                        spriteBatch.Draw(px, new Rectangle(slotX - t, slotY + SlotSize, SlotSize + t * 2, t), selColor);
                        spriteBatch.Draw(px, new Rectangle(slotX - t, slotY, t, SlotSize), selColor);
                        spriteBatch.Draw(px, new Rectangle(slotX + SlotSize, slotY, t, SlotSize), selColor);
                    }

                    // Item icon
                    if (index < items.Count)
                    {
                        var item = items[index];
                        var sprite = GameRef.SharedPlayer.Inventory.GetSprite(item.Id);
                        if (sprite != null)
                        {
                            var srcRect = GameRef.SharedPlayer.Inventory.GetSourceRect(item.Id);
                            var destRect = new Rectangle(slotX + 6, slotY + 6, SlotSize - 12, SlotSize - 12);
                            if (srcRect.HasValue)
                                spriteBatch.Draw(sprite, destRect, srcRect.Value, Color.White);
                            else
                                spriteBatch.Draw(sprite, destRect, Color.White);
                        }

                        // Quantity badge (bottom-right)
                        int qty = GameRef.SharedPlayer.Inventory.GetQuantity(item);
                        if (qty > 1)
                        {
                            string qtyStr = qty.ToString();
                            int badgeW = qtyStr.Length * 6 + 4;
                            spriteBatch.Draw(px, new Rectangle(slotX + SlotSize - badgeW - 2, slotY + SlotSize - 12, badgeW, 10), Color.Black * 0.7f);
                            PixelFont.DrawString(spriteBatch, qtyStr, slotX + SlotSize - badgeW, slotY + SlotSize - 11, Color.White, 1);
                        }
                    }
                }
            }

            // ── Description area ──
            int descY = gridStartY + totalGridH + 12;
            spriteBatch.Draw(px, new Rectangle(panelX + 8, descY - 4, panelW - 16, 1), Color.White * 0.2f);

            int selectedIndex = _cursorY * GridWidth + _cursorX;
            if (selectedIndex < items.Count)
            {
                var sel = items[selectedIndex];
                bool isEquipped = selectedIndex == GameRef.SharedPlayer.Inventory.EquippedIndex;

                // Item name
                Color nameColor = isEquipped ? Color.Cyan : Color.Gold;
                string nameStr = sel.Name;
                if (isEquipped) nameStr += " [EQUIPPED]";
                PixelFont.DrawString(spriteBatch, nameStr, gridStartX, descY + 2, nameColor, 2);

                // Type tag
                string typeTag = sel.Type switch
                {
                    ItemType.Weapon => "WEAPON",
                    ItemType.Resource => "RESOURCE",
                    ItemType.KeyItem => "KEY ITEM",
                    ItemType.Consumable => "CONSUMABLE",
                    _ => ""
                };
                Color tagColor = sel.Type switch
                {
                    ItemType.Weapon => new Color(200, 80, 80),
                    ItemType.Resource => new Color(80, 180, 80),
                    ItemType.KeyItem => new Color(200, 180, 60),
                    _ => Color.Gray
                };
                int tagX = gridStartX + totalGridW - typeTag.Length * 6 - 4;
                spriteBatch.Draw(px, new Rectangle(tagX - 2, descY + 2, typeTag.Length * 6 + 4, 10), tagColor * 0.3f);
                PixelFont.DrawString(spriteBatch, typeTag, tagX, descY + 3, tagColor, 1);

                // Description
                PixelFont.DrawWrapped(spriteBatch, sel.Description,
                    gridStartX, descY + 20, totalGridW, Color.White * 0.8f, 2);

                // Stats line for weapons
                if (sel.Type == ItemType.Weapon || sel.EffectType == EffectType.Melee ||
                    sel.EffectType == EffectType.Projectile || sel.EffectType == EffectType.ProjectileReturn ||
                    sel.EffectType == EffectType.AOE)
                {
                    string stats = $"DMG:{sel.Damage}  RNG:{(int)sel.Range}  SPD:{sel.Cooldown:0.#}s";
                    PixelFont.DrawString(spriteBatch, stats, gridStartX, descY + 52, new Color(180, 160, 120), 1);
                }
            }
            else
            {
                PixelFont.DrawString(spriteBatch, "Empty slot", gridStartX, descY + 2, Color.Gray * 0.5f, 2);
            }

            // ── Coin display ──
            int coinY2 = panelY + 10;
            int coinX2 = panelX + panelW - 70;
            // Procedural coin icon
            spriteBatch.Draw(px, new Rectangle(coinX2, coinY2, 12, 12), new Color(220, 180, 40));
            spriteBatch.Draw(px, new Rectangle(coinX2 + 1, coinY2 + 1, 10, 10), new Color(255, 215, 50));
            spriteBatch.Draw(px, new Rectangle(coinX2 + 4, coinY2 + 2, 4, 8), new Color(200, 160, 30));
            PixelFont.DrawString(spriteBatch, GameRef.SharedPlayer.Inventory.Coins.ToString(),
                coinX2 + 16, coinY2 + 2, Color.Gold, 2);

            // ── Controls hint ──
            int ctrlY = panelY + panelH - controlsH - 4;
            spriteBatch.Draw(px, new Rectangle(panelX + 8, ctrlY - 2, panelW - 16, 1), Color.White * 0.1f);
            PixelFont.DrawCentered(spriteBatch, "WASD:Move  SPACE:Equip  I:Close", ctrlY + 4, Color.White * 0.4f, 1);
        }
    }
}
