// ============================================================================
// BlacksmithState.cs — Blacksmith Upgrade UI
// Author: Mehdi Lakhouane
// Description: Overlay UI for upgrading weapons and equipment at the forge.
// ============================================================================

using System;
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
    public class BlacksmithState : GameStateBase
    {
        private Player _player;
        private KeyboardState _prevKb;
        private int _selectedIndex;
        private string _message;
        private float _messageTimer;

        private struct UpgradeOption
        {
            public string Name;
            public string Description;
            public int CoinCost;
            public string MaterialId;
            public int MaterialCost;
            public Func<Player, bool> CanApply;
            public Action<Player> Apply;
        }

        private UpgradeOption[] _options;

        public BlacksmithState(Game1 game, ContentManager content) : base(game, content)
        {
            _prevKb = Keyboard.GetState();
        }

        public void Open(Player player)
        {
            _player = player;
            _selectedIndex = 0;
            _message = null;
            _messageTimer = 0;
            _prevKb = Keyboard.GetState();

            _options = new UpgradeOption[]
            {
                new UpgradeOption
                {
                    Name = "SHARPEN SWORD",
                    Description = "Increase sword damage by 1",
                    CoinCost = 50,
                    MaterialId = "stone",
                    MaterialCost = 5,
                    CanApply = p => p.AttackDamage < 5,
                    Apply = p =>
                    {
                        p.AttackDamage++;
                        GameRef.QuestFlags.Add("sword_upgraded");
                    }
                },
                new UpgradeOption
                {
                    Name = "REINFORCE ARMOR",
                    Description = "Gain +1 max heart",
                    CoinCost = 80,
                    MaterialId = "stone",
                    MaterialCost = 8,
                    CanApply = _ => true,
                    Apply = p => p.UpgradeMaxHealth(2)
                },
                new UpgradeOption
                {
                    Name = "FORGE AXE",
                    Description = "Craft an Axe (2 damage, slow)",
                    CoinCost = 40,
                    MaterialId = "wood",
                    MaterialCost = 5,
                    CanApply = p => !p.Inventory.HasItem("axe"),
                    Apply = p => p.Inventory.AddItem("axe")
                },
                new UpgradeOption
                {
                    Name = "HEALING SALVE",
                    Description = "Craft a Health Potion",
                    CoinCost = 15,
                    MaterialId = "flower",
                    MaterialCost = 3,
                    CanApply = _ => true,
                    Apply = p => p.Inventory.AddItem("health_potion")
                },
            };
        }

        public override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            KeyboardState kb = Keyboard.GetState();

            if (_messageTimer > 0) _messageTimer -= dt;

            if (kb.IsKeyDown(Keys.Escape) && _prevKb.IsKeyUp(Keys.Escape))
            {
                GameRef.ResumeFromPause();
                _prevKb = kb;
                return;
            }

            if (kb.IsKeyDown(Keys.Up) && _prevKb.IsKeyUp(Keys.Up))
                _selectedIndex = (_selectedIndex - 1 + _options.Length) % _options.Length;
            if (kb.IsKeyDown(Keys.Down) && _prevKb.IsKeyUp(Keys.Down))
                _selectedIndex = (_selectedIndex + 1) % _options.Length;

            if ((kb.IsKeyDown(Keys.E) && _prevKb.IsKeyUp(Keys.E)) ||
                (kb.IsKeyDown(Keys.Enter) && _prevKb.IsKeyUp(Keys.Enter)))
            {
                TryUpgrade();
            }

            _prevKb = kb;
        }

        private void TryUpgrade()
        {
            var opt = _options[_selectedIndex];

            if (!opt.CanApply(_player))
            {
                _message = "ALREADY AT MAX!";
                _messageTimer = 1.5f;
                return;
            }

            if (_player.Inventory.Coins < opt.CoinCost)
            {
                _message = "NOT ENOUGH COINS!";
                _messageTimer = 1.5f;
                return;
            }

            int matQty = _player.Inventory.GetQuantity(opt.MaterialId);
            if (matQty < opt.MaterialCost)
            {
                _message = $"NEED {opt.MaterialCost} {opt.MaterialId.ToUpper()}! (HAVE {matQty})";
                _messageTimer = 2f;
                return;
            }

            // Apply upgrade
            _player.Inventory.Coins -= opt.CoinCost;
            _player.Inventory.RemoveItem(opt.MaterialId, opt.MaterialCost);
            opt.Apply(_player);
            _message = $"{opt.Name} COMPLETE!";
            _messageTimer = 2f;
        }

        public override void Draw(SpriteBatch sb)
        {
            Texture2D px = Game1.PixelTexture;
            int sw = Game1.ScreenWidth;
            int sh = Game1.ScreenHeight;

            // Dim background
            sb.Draw(px, new Rectangle(0, 0, sw, sh), Color.Black * 0.6f);

            // Panel
            int panelW = 440;
            int panelH = 340;
            int panelX = sw / 2 - panelW / 2;
            int panelY = sh / 2 - panelH / 2;
            sb.Draw(px, new Rectangle(panelX - 2, panelY - 2, panelW + 4, panelH + 4),
                new Color(180, 100, 40));
            sb.Draw(px, new Rectangle(panelX, panelY, panelW, panelH), new Color(35, 25, 20));

            // Title
            string title = "THE FORGE";
            int titleW = PixelFont.MeasureWidth(title, 2);
            PixelFont.DrawString(sb, title, sw / 2 - titleW / 2, panelY + 10, new Color(255, 160, 50), 2);

            // Coins
            string coinStr = $"COINS: {_player.Inventory.Coins}";
            PixelFont.DrawString(sb, coinStr,
                panelX + panelW - PixelFont.MeasureWidth(coinStr, 1) - 10,
                panelY + 14, Color.Gold, 1);

            // Options list
            int listY = panelY + 50;
            for (int i = 0; i < _options.Length; i++)
            {
                var opt = _options[i];
                bool selected = i == _selectedIndex;
                bool canDo = opt.CanApply(_player);
                Color nameColor = !canDo ? Color.DarkGray * 0.5f :
                    selected ? Color.White : Color.Gray;

                if (selected)
                    sb.Draw(px, new Rectangle(panelX + 8, listY + i * 60, panelW - 16, 54),
                        Color.White * 0.08f);

                string arrow = selected ? "> " : "  ";
                PixelFont.DrawString(sb, $"{arrow}{opt.Name}", panelX + 16, listY + i * 60 + 4, nameColor, 2);
                PixelFont.DrawString(sb, opt.Description, panelX + 40, listY + i * 60 + 22,
                    Color.Gray * 0.7f, 1);

                // Cost display
                int matQty = _player.Inventory.GetQuantity(opt.MaterialId);
                bool hasCoins = _player.Inventory.Coins >= opt.CoinCost;
                bool hasMats = matQty >= opt.MaterialCost;

                string costStr = $"{opt.CoinCost}G + {opt.MaterialCost} {opt.MaterialId.ToUpper()}";
                Color costColor = (hasCoins && hasMats && canDo) ? Color.Green : Color.Red;
                PixelFont.DrawString(sb, costStr, panelX + 40, listY + i * 60 + 36, costColor, 1);
            }

            // Message
            if (_messageTimer > 0 && _message != null)
            {
                int msgW = PixelFont.MeasureWidth(_message, 2);
                PixelFont.DrawString(sb, _message, sw / 2 - msgW / 2,
                    panelY + panelH - 40, Color.Yellow, 2);
            }

            // Controls
            PixelFont.DrawString(sb, "[E] FORGE   [ESC] CLOSE",
                panelX + 16, panelY + panelH - 18, Color.Gray * 0.6f, 1);
        }
    }
}
