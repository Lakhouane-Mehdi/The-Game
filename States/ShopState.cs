// ============================================================================
// ShopState.cs — Shop Buy/Sell UI
// Author: Mehdi Lakhouane
// Description: Overlay UI for buying and selling items at the shop.
// ============================================================================

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TheGame.Core;
using TheGame.Entities;
using TheGame.Entities.Items;
using TheGame.Utils;

namespace TheGame.States
{
    public class ShopState : GameStateBase
    {
        private Player _player;
        private KeyboardState _prevKb;
        private int _selectedIndex;
        private int _tab; // 0 = BUY, 1 = SELL
        private string _message;
        private float _messageTimer;

        // Shop inventory
        private static readonly (string itemId, string name, int buyPrice, int sellPrice)[] ShopItems =
        {
            ("health_potion", "Health Potion", 25, 8),
            ("bomb", "Bomb (x3)", 40, 12),
            ("bow", "Bow", 60, 20),
            ("boomerang", "Boomerang", 50, 15),
            ("axe", "Axe", 80, 25),
        };

        // Sellable resource items
        private static readonly (string itemId, string name, int sellPrice)[] SellableItems =
        {
            ("fruit", "Wild Fruit", 3),
            ("stone", "Stone", 4),
            ("flower", "Wildflower", 5),
            ("mushroom", "Mushroom", 6),
            ("wood", "Wood", 3),
        };

        public ShopState(Game1 game, ContentManager content) : base(game, content)
        {
            _prevKb = Keyboard.GetState();
        }

        public void Open(Player player)
        {
            _player = player;
            _selectedIndex = 0;
            _tab = 0;
            _message = null;
            _messageTimer = 0;
            _prevKb = Keyboard.GetState();
        }

        public override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            KeyboardState kb = Keyboard.GetState();

            if (_messageTimer > 0) _messageTimer -= dt;

            // Close shop
            if (kb.IsKeyDown(Keys.Escape) && _prevKb.IsKeyUp(Keys.Escape))
            {
                GameRef.ResumeFromPause();
                _prevKb = kb;
                return;
            }

            // Switch tabs
            if (kb.IsKeyDown(Keys.Tab) && _prevKb.IsKeyUp(Keys.Tab))
            {
                _tab = 1 - _tab;
                _selectedIndex = 0;
            }

            int maxItems = _tab == 0 ? ShopItems.Length : SellableItems.Length;

            // Navigate
            if (kb.IsKeyDown(Keys.Up) && _prevKb.IsKeyUp(Keys.Up))
                _selectedIndex = (_selectedIndex - 1 + maxItems) % maxItems;
            if (kb.IsKeyDown(Keys.Down) && _prevKb.IsKeyUp(Keys.Down))
                _selectedIndex = (_selectedIndex + 1) % maxItems;

            // Confirm purchase/sell
            if ((kb.IsKeyDown(Keys.E) && _prevKb.IsKeyUp(Keys.E)) ||
                (kb.IsKeyDown(Keys.Enter) && _prevKb.IsKeyUp(Keys.Enter)))
            {
                if (_tab == 0)
                    TryBuy();
                else
                    TrySell();
            }

            _prevKb = kb;
        }

        private void TryBuy()
        {
            var item = ShopItems[_selectedIndex];
            if (_player.Inventory.Coins >= item.buyPrice)
            {
                _player.Inventory.Coins -= item.buyPrice;
                int amount = item.itemId == "bomb" ? 3 : 1;
                _player.Inventory.AddItem(item.itemId, amount);
                _message = $"BOUGHT {item.name}!";
                _messageTimer = 1.5f;
            }
            else
            {
                _message = "NOT ENOUGH COINS!";
                _messageTimer = 1.5f;
            }
        }

        private void TrySell()
        {
            var item = SellableItems[_selectedIndex];
            int qty = _player.Inventory.GetQuantity(item.itemId);
            if (qty > 0)
            {
                _player.Inventory.RemoveItem(item.itemId, 1);
                _player.Inventory.Coins += item.sellPrice;
                _message = $"SOLD {item.name} FOR {item.sellPrice} COINS!";
                _messageTimer = 1.5f;
            }
            else
            {
                _message = "YOU DON'T HAVE ANY!";
                _messageTimer = 1.5f;
            }
        }

        public override void Draw(SpriteBatch sb)
        {
            Texture2D px = Game1.PixelTexture;
            int sw = Game1.ScreenWidth;
            int sh = Game1.ScreenHeight;

            // Dim background
            sb.Draw(px, new Rectangle(0, 0, sw, sh), Color.Black * 0.6f);

            // Panel
            int panelW = 400;
            int panelH = 320;
            int panelX = sw / 2 - panelW / 2;
            int panelY = sh / 2 - panelH / 2;
            sb.Draw(px, new Rectangle(panelX - 2, panelY - 2, panelW + 4, panelH + 4), Color.White * 0.8f);
            sb.Draw(px, new Rectangle(panelX, panelY, panelW, panelH), new Color(30, 25, 40));

            // Title
            string title = "GENERAL STORE";
            int titleW = PixelFont.MeasureWidth(title, 2);
            PixelFont.DrawString(sb, title, sw / 2 - titleW / 2, panelY + 10, Color.Gold, 2);

            // Tabs
            Color buyTabColor = _tab == 0 ? Color.Gold : Color.Gray;
            Color sellTabColor = _tab == 1 ? Color.Gold : Color.Gray;
            PixelFont.DrawString(sb, "BUY", panelX + 20, panelY + 40, buyTabColor, 2);
            PixelFont.DrawString(sb, "SELL", panelX + 120, panelY + 40, sellTabColor, 2);
            PixelFont.DrawString(sb, "[TAB]", panelX + 220, panelY + 42, Color.Gray * 0.6f, 1);

            // Coins display
            string coinStr = $"COINS: {_player.Inventory.Coins}";
            PixelFont.DrawString(sb, coinStr, panelX + panelW - PixelFont.MeasureWidth(coinStr, 1) - 10,
                panelY + 42, Color.Gold, 1);

            // Items list
            int listY = panelY + 70;

            if (_tab == 0)
            {
                for (int i = 0; i < ShopItems.Length; i++)
                {
                    var item = ShopItems[i];
                    bool selected = i == _selectedIndex;
                    Color c = selected ? Color.White : Color.Gray;
                    if (selected)
                        sb.Draw(px, new Rectangle(panelX + 10, listY + i * 30, panelW - 20, 26),
                            Color.White * 0.1f);

                    string arrow = selected ? "> " : "  ";
                    PixelFont.DrawString(sb, $"{arrow}{item.name}", panelX + 20, listY + i * 30 + 4, c, 2);

                    bool canAfford = _player.Inventory.Coins >= item.buyPrice;
                    Color priceColor = canAfford ? Color.Green : Color.Red;
                    string priceStr = $"{item.buyPrice}G";
                    PixelFont.DrawString(sb, priceStr,
                        panelX + panelW - PixelFont.MeasureWidth(priceStr, 2) - 20,
                        listY + i * 30 + 4, priceColor, 2);
                }
            }
            else
            {
                for (int i = 0; i < SellableItems.Length; i++)
                {
                    var item = SellableItems[i];
                    bool selected = i == _selectedIndex;
                    int qty = _player.Inventory.GetQuantity(item.itemId);
                    Color c = selected ? Color.White : Color.Gray;
                    if (qty == 0) c = Color.DarkGray * 0.5f;
                    if (selected)
                        sb.Draw(px, new Rectangle(panelX + 10, listY + i * 30, panelW - 20, 26),
                            Color.White * 0.1f);

                    string arrow = selected ? "> " : "  ";
                    PixelFont.DrawString(sb, $"{arrow}{item.name} (x{qty})",
                        panelX + 20, listY + i * 30 + 4, c, 2);

                    string priceStr = $"+{item.sellPrice}G";
                    PixelFont.DrawString(sb, priceStr,
                        panelX + panelW - PixelFont.MeasureWidth(priceStr, 2) - 20,
                        listY + i * 30 + 4, qty > 0 ? Color.Green : Color.DarkGray, 2);
                }
            }

            // Message
            if (_messageTimer > 0 && _message != null)
            {
                int msgW = PixelFont.MeasureWidth(_message, 2);
                PixelFont.DrawString(sb, _message, sw / 2 - msgW / 2,
                    panelY + panelH - 40, Color.Yellow, 2);
            }

            // Controls hint
            PixelFont.DrawString(sb, "[E] SELECT   [ESC] CLOSE",
                panelX + 20, panelY + panelH - 18, Color.Gray * 0.6f, 1);
        }
    }
}
