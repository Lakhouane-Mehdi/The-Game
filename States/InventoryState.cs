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
        private const int GridWidth = 4;
        private const int GridHeight = 4;
        private const int SlotSize = 64;
        private const int Padding = 16;

        private Texture2D _panelTex;
        private Texture2D _selectionTex;
        private KeyboardState _prevKb = Keyboard.GetState();

        public InventoryState(Game1 game, ContentManager content) : base(game, content)
        {
            _panelTex = AssetLoader.CreatePixel(new Color(40, 40, 60, 200));
            _selectionTex = AssetLoader.CreatePixel(new Color(255, 215, 0, 150)); // Gold
        }

        public override void Update(GameTime gameTime)
        {
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
                GameRef.SharedPlayer.Inventory.SetEquippedIndex(index);
            }

            // Exit
            if ((kb.IsKeyDown(Keys.Escape) && _prevKb.IsKeyUp(Keys.Escape)) ||
                (kb.IsKeyDown(Keys.Enter) && _prevKb.IsKeyUp(Keys.Enter)) ||
                (kb.IsKeyDown(Keys.I) && _prevKb.IsKeyUp(Keys.I)))
            {
                GameRef.SharedPlayer.Inventory.SaveItems(AssetLoader.GetFullPath(""));
                GameRef.ResumeFromPause();
            }

            _prevKb = kb;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            // Dim background
            spriteBatch.Draw(Game1.PixelTexture, new Rectangle(0, 0, Game1.ScreenWidth, Game1.ScreenHeight), Color.Black * 0.5f);

            int totalGridW = GridWidth * (SlotSize + Padding) - Padding;
            int totalGridH = GridHeight * (SlotSize + Padding) - Padding;
            int panelPadding = 24;
            int descriptionHeight = 80;
            int panelW = totalGridW + panelPadding * 2;
            int panelH = totalGridH + panelPadding * 2 + descriptionHeight;
            int startX = Game1.ScreenWidth / 2 - totalGridW / 2;
            int startY = Game1.ScreenHeight / 2 - panelH / 2 + panelPadding;

            // Draw Panel (centered vertically)
            spriteBatch.Draw(_panelTex, new Rectangle(
                startX - panelPadding, startY - panelPadding,
                panelW, panelH), Color.White);

            // Draw Grid
            var items = GameRef.SharedPlayer.Inventory.Items;
            for (int y = 0; y < GridHeight; y++)
            {
                for (int x = 0; x < GridWidth; x++)
                {
                    int px = startX + x * (SlotSize + Padding);
                    int py = startY + y * (SlotSize + Padding);
                    int index = y * GridWidth + x;

                    // Slot bg
                    spriteBatch.Draw(Game1.PixelTexture, new Rectangle(px, py, SlotSize, SlotSize), Color.Black * 0.4f);

                    // Selection box
                    if (x == _cursorX && y == _cursorY)
                    {
                        spriteBatch.Draw(_selectionTex, new Rectangle(px - 2, py - 2, SlotSize + 4, SlotSize + 4), Color.White);
                    }

                    // Item icon
                    if (index < items.Count)
                    {
                        var item = items[index];
                        var sprite = GameRef.SharedPlayer.Inventory.GetSprite(item.Id);
                        if (sprite != null)
                        {
                            spriteBatch.Draw(sprite, new Rectangle(px + 4, py + 4, SlotSize - 8, SlotSize - 8), Color.White);
                        }

                        // Quantity
                        int qty = GameRef.SharedPlayer.Inventory.GetQuantity(item);
                        if (qty > 1)
                        {
                            PixelFont.DrawString(spriteBatch, qty.ToString(), px + SlotSize - 12, py + SlotSize - 15, Color.White, 1);
                        }
                    }
                }
            }

            // Description Area (below grid, inside panel)
            int selectedIndex = _cursorY * GridWidth + _cursorX;
            if (selectedIndex < items.Count)
            {
                var selectedItem = items[selectedIndex];
                int descY = startY + totalGridH + 16;
                int descMaxW = totalGridW;

                // Separator line
                spriteBatch.Draw(Game1.PixelTexture, new Rectangle(
                    startX, descY - 6, totalGridW, 2), Color.White * 0.3f);

                // Draw item name with equipped indicator
                bool isEquipped = selectedIndex == GameRef.SharedPlayer.Inventory.EquippedIndex;
                Color titleColor = isEquipped ? Color.Cyan : Color.Yellow;
                string title = isEquipped ? selectedItem.Name + " (Equipped)" : selectedItem.Name;

                PixelFont.DrawString(spriteBatch, title, startX, descY, titleColor, 2);
                // Word-wrapped description
                PixelFont.DrawWrapped(spriteBatch, selectedItem.Description,
                    startX, descY + 22, descMaxW, Color.White, 2);
            }
        }
    }
}
