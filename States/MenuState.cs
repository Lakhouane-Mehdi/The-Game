// ============================================================================
// MenuState.cs — Title / Main Menu
// Author: Mehdi Lakhouane
// Description: Displays the game title and waits for the player to press
//              Enter to begin. Transitions to the Overworld state.
// ============================================================================

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TheGame.Core;
using TheGame.Utils;

namespace TheGame.States
{
    public class MenuState : GameStateBase
    {
        private Texture2D _titleBg;
        private float _blinkTimer;
        private bool _showPrompt = true;
        private KeyboardState _prevKb;

        public MenuState(Game1 game, ContentManager content)
            : base(game, content)
        {
            // Use a tileset image as the title background
            if (AssetLoader.Exists("Tiles/ground.png"))
                _titleBg = AssetLoader.LoadTexture("Tiles/ground.png");
        }

        public override void Update(GameTime gameTime)
        {
            KeyboardState kb = Keyboard.GetState();

            // Blink the prompt
            _blinkTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_blinkTimer >= 0.5f)
            {
                _blinkTimer = 0f;
                _showPrompt = !_showPrompt;
            }

            // Transition on Enter (edge-triggered)
            if (kb.IsKeyDown(Keys.Enter) && _prevKb.IsKeyUp(Keys.Enter))
                GameRef.ChangeState(GameState.Overworld);

            _prevKb = kb;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            // Draw tiled background
            if (_titleBg != null)
            {
                for (int x = 0; x < Game1.ScreenWidth; x += _titleBg.Width)
                    for (int y = 0; y < Game1.ScreenHeight; y += _titleBg.Height)
                        spriteBatch.Draw(_titleBg, new Vector2(x, y), Color.White * 0.3f);
            }

            // Draw title text using pixel rectangles (no SpriteFont needed)
            Texture2D px = Game1.PixelTexture;

            // "THE GAME" as a large centered box
            int boxW = 300, boxH = 60;
            int boxX = Game1.ScreenWidth / 2 - boxW / 2;
            int boxY = 140;
            spriteBatch.Draw(px, new Rectangle(boxX, boxY, boxW, boxH), Color.DarkGreen * 0.8f);
            spriteBatch.Draw(px, new Rectangle(boxX + 2, boxY + 2, boxW - 4, boxH - 4), Color.Black * 0.9f);

            // Title indicator bar
            spriteBatch.Draw(px, new Rectangle(boxX + 20, boxY + 20, boxW - 40, 4), Color.Gold);
            spriteBatch.Draw(px, new Rectangle(boxX + 20, boxY + 36, boxW - 40, 4), Color.Gold);

            // "Press ENTER" blinking prompt
            if (_showPrompt)
            {
                int promptW = 180, promptH = 20;
                int promptX = Game1.ScreenWidth / 2 - promptW / 2;
                spriteBatch.Draw(px, new Rectangle(promptX, 320, promptW, promptH), Color.White * 0.6f);
            }

            // Controls hint
            int hintY = 400;
            spriteBatch.Draw(px, new Rectangle(250, hintY, 300, 3), Color.Gray * 0.4f);
            spriteBatch.Draw(px, new Rectangle(250, hintY + 20, 300, 3), Color.Gray * 0.4f);
        }
    }
}
