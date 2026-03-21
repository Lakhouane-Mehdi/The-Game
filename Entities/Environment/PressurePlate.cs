// ============================================================================
// PressurePlate.cs — Pressure Plate Trigger
// Author: Mehdi Lakhouane
// Description: A floor trigger that activates when the player stands on it.
//              Can open doors, toggle walls, or trigger events.
//              Supports both "hold" (active while standing) and "toggle"
//              (flip on each step) modes.
// ============================================================================

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheGame.Core;

namespace TheGame.Entities.Environment
{
    public enum TriggerMode
    {
        Hold,   // active only while the player stands on it
        Toggle  // flips state each time the player steps on it
    }

    public class PressurePlate
    {
        // ── Transform ──
        public Vector2 Position;
        public int Width { get; set; } = 32;
        public int Height { get; set; } = 32;
        public Rectangle BoundingBox => new Rectangle(
            (int)Position.X, (int)Position.Y, Width, Height);

        // ── State ──
        public bool IsActivated { get; private set; }
        public TriggerMode Mode { get; set; }

        // ── Linked target (the thing this plate controls) ──
        /// <summary>
        /// Callback invoked when the plate's state changes.
        /// Parameter: true = activated, false = deactivated.
        /// Use this to open/close doors, toggle walls, etc.
        /// </summary>
        public Action<bool> OnStateChanged { get; set; }

        // ── Linked door (optional convenience) ──
        public Rectangle? LinkedDoor { get; set; }

        // ── Visual ──
        private bool _playerWasOn;
        private Color _baseColor = new Color(80, 80, 60);
        private Color _activeColor = new Color(60, 180, 80);

        public PressurePlate(Vector2 position, TriggerMode mode = TriggerMode.Hold)
        {
            Position = position;
            Mode = mode;
        }

        public void Update(GameTime gameTime, Rectangle playerBB)
        {
            bool playerIsOn = BoundingBox.Intersects(playerBB);

            switch (Mode)
            {
                case TriggerMode.Hold:
                    if (playerIsOn && !IsActivated)
                    {
                        IsActivated = true;
                        OnStateChanged?.Invoke(true);
                    }
                    else if (!playerIsOn && IsActivated)
                    {
                        IsActivated = false;
                        OnStateChanged?.Invoke(false);
                    }
                    break;

                case TriggerMode.Toggle:
                    // Only toggle on the rising edge (player just stepped on)
                    if (playerIsOn && !_playerWasOn)
                    {
                        IsActivated = !IsActivated;
                        OnStateChanged?.Invoke(IsActivated);
                    }
                    break;
            }

            _playerWasOn = playerIsOn;
        }

        public void Draw(SpriteBatch sb)
        {
            Texture2D px = Game1.PixelTexture;
            Color color = IsActivated ? _activeColor : _baseColor;

            // Outer plate
            sb.Draw(px, new Rectangle(
                (int)Position.X, (int)Position.Y, Width, Height), color * 0.6f);

            // Inner diamond pattern
            int inset = 6;
            sb.Draw(px, new Rectangle(
                (int)Position.X + inset, (int)Position.Y + inset,
                Width - inset * 2, Height - inset * 2),
                IsActivated ? Color.Lime * 0.4f : Color.Gray * 0.3f);
        }
    }
}
