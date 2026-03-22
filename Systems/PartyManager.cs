using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TheGame.Entities;

namespace TheGame.Systems
{
    /// <summary>
    /// Manages the player's captured Echoes. Limits the active party to 3.
    /// </summary>
    public class PartyManager
    {
        private const int MAX_PARTY_SIZE = 3;
        
        // Active group of captured Echoes.
        private readonly List<EchoEntity> _party = new List<EchoEntity>();

        // Provides access to the currently 'equipped' lead Echo for passive abilities.
        public EchoEntity LeadEcho => _party.Count > 0 ? _party[0] : null;

        public IReadOnlyList<EchoEntity> CurrentParty => _party.AsReadOnly();

        /// <summary>
        /// Attempts to capture an Echo and add it to the party. 
        /// Returns false if party is full.
        /// </summary>
        public bool CaptureEcho(EchoEntity echo, Player player)
        {
            if (_party.Count >= MAX_PARTY_SIZE)
                return false; // Party is full, maybe send to "PCBox" equivalent later

            _party.Add(echo);
            
            // If it's the first one, immediately apply passive ability
            if (_party.Count == 1)
            {
                echo.Ability?.ApplyPassive(player);
            }

            return true;
        }

        /// <summary>
        /// Cycles the party so a new Echo takes the lead. Updates passive abilities.
        /// </summary>
        public void CycleLead(Player player)
        {
            if (_party.Count <= 1) return;

            // Remove passive of old lead
            LeadEcho?.Ability?.RemovePassive(player);

            // Shift index 0 to back
            var oldLead = _party[0];
            _party.RemoveAt(0);
            _party.Add(oldLead);

            // Apply passive of new lead
            LeadEcho?.Ability?.ApplyPassive(player);
        }

        /// <summary>
        /// Updates the following logic for the lead Echo to trail behind the player.
        /// </summary>
        public void Update(Vector2 playerPos, GameTime gameTime)
        {
            if (LeadEcho != null)
            {
                LeadEcho.UpdateFollowerLogic(playerPos, gameTime);
            }
        }

        /// <summary>
        /// Draws the lead Echo directly, bypassing normal RoomManager rendering since it follows across rooms.
        /// </summary>
        public void Draw(Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch)
        {
            // The lead echo floats around the player in all rooms.
            // Other party members remain inside their capture crystals (hidden).
            if (LeadEcho != null)
            {
                LeadEcho.Draw(spriteBatch);
            }
        }
    }
}
