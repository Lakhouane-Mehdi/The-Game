// ============================================================================
// GameStateBase.cs — Abstract State Handler
// Author: Mehdi Lakhouane
// Description: Interface contract for all game states (Menu, Overworld, Dungeon).
//              Each state owns its own Update and Draw logic.
// ============================================================================

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TheGame.Core;

namespace TheGame.States
{
    /// <summary>
    /// Base class for all game states. Each state receives a reference
    /// back to Game1 so it can trigger state transitions.
    /// </summary>
    public abstract class GameStateBase
    {
        protected Game1 GameRef { get; }
        protected ContentManager Content { get; }

        protected GameStateBase(Game1 game, ContentManager content)
        {
            GameRef = game;
            Content = content;
        }

        public abstract void Update(GameTime gameTime);
        public abstract void Draw(SpriteBatch spriteBatch);
    }
}
