// ============================================================================
// Camera.cs — Zelda-Style Screen-Scrolling Camera
// Author: Mehdi Lakhouane
// Description: Divides the world into discrete rooms (screen-sized tiles).
//              When the player touches a screen edge, the camera lerps to
//              the next room and repositions the player inside the boundary.
//              Exposes room coordinates so other systems can activate/deactivate
//              entities per room.
//
// How it works:
//   The world is a grid of rooms, each exactly ScreenWidth x ScreenHeight.
//   Room (0,0) covers pixels (0,0)→(ScreenW, ScreenH).
//   Room (1,0) covers (ScreenW, 0)→(2*ScreenW, ScreenH), etc.
//
//   The camera's Position is the top-left corner of the visible area.
//   During a transition, Position lerps from the old room origin to the new
//   one over TransitionDuration seconds. While transitioning, gameplay is
//   paused (the player and enemies freeze).
// ============================================================================

using System;
using Microsoft.Xna.Framework;

namespace TheGame.Systems
{
    public class Camera
    {
        // ── Room dimensions (each room = one screen) ──
        public int RoomWidth { get; }
        public int RoomHeight { get; }

        // ── Current room index ──
        public int RoomX { get; private set; }
        public int RoomY { get; private set; }

        // ── Previous room (for detecting room changes) ──
        public int PrevRoomX { get; private set; }
        public int PrevRoomY { get; private set; }

        // ── Screen Shake ──
        private float _shakeIntensity;
        private float _shakeDuration;
        private float _shakeTimer;
        private Vector2 _shakeOffset;
        private Random _shakeRng = new Random();

        /// <summary>
        /// True when the camera changed rooms on the last CheckTransition call.
        /// Use this to trigger enemy activation/deactivation.
        /// </summary>
        public bool RoomChanged { get; private set; }

        // ── Camera position (top-left of visible area) ──
        public Vector2 Position { get; private set; }

        // ── Transition state ──
        public bool IsTransitioning { get; private set; }
        private Vector2 _transitionStart;
        private Vector2 _transitionEnd;
        private float _transitionTimer;
        private const float TransitionDuration = 0.6f; // seconds for the slide

        // ── Player repositioning after transition ──
        private const float PlayerInset = 14f; // pixels inside the new room edge

        // ── World bounds (in rooms) ──
        public int WorldRoomsX { get; }
        public int WorldRoomsY { get; }

        public Camera(int roomWidth, int roomHeight, int worldRoomsX, int worldRoomsY)
        {
            RoomWidth = roomWidth;
            RoomHeight = roomHeight;
            WorldRoomsX = worldRoomsX;
            WorldRoomsY = worldRoomsY;
            RoomX = 0;
            RoomY = 0;
            Position = Vector2.Zero;
        }

        /// <summary>
        /// Returns the top-left pixel of the current room.
        /// </summary>
        public Vector2 RoomOrigin => new Vector2(RoomX * RoomWidth, RoomY * RoomHeight);

        /// <summary>
        /// The transformation matrix to pass to SpriteBatch.Begin().
        /// Offsets everything by -Camera.Position so the current room
        /// appears at (0,0) on screen.
        /// </summary>
        public Matrix TransformMatrix => Matrix.CreateTranslation(
            -(int)Position.X + (int)_shakeOffset.X,
            -(int)Position.Y + (int)_shakeOffset.Y, 0f);

        // ──────────────────────────────────────────────
        //  Transition Check — call every frame
        // ──────────────────────────────────────────────

        /// <summary>
        /// Checks if the player has crossed a room boundary.
        /// If so, starts a camera slide transition and returns the
        /// new position the player should be warped to.
        /// </summary>
        /// <param name="playerPos">Player's current world position.</param>
        /// <param name="playerHitbox">Player's bounding box (world coords).</param>
        /// <param name="newPlayerPos">Output: adjusted player position, or unchanged if no transition.</param>
        /// <returns>True if a transition was started.</returns>
        public bool CheckTransition(Vector2 playerPos, Rectangle playerHitbox, out Vector2 newPlayerPos)
        {
            RoomChanged = false;
            newPlayerPos = playerPos;

            if (IsTransitioning) return false;

            int newRoomX = RoomX;
            int newRoomY = RoomY;
            Vector2 adjustedPos = playerPos;

            float roomLeft = RoomX * RoomWidth;
            float roomRight = roomLeft + RoomWidth;
            float roomTop = RoomY * RoomHeight;
            float roomBottom = roomTop + RoomHeight;

            // Extract the offset between the graphic position and the actual hitbox
            float offsetX = playerHitbox.Left - playerPos.X;
            float offsetY = playerHitbox.Top - playerPos.Y;

            // ── Check each edge ──
            if (playerHitbox.Right >= roomRight && RoomX < WorldRoomsX - 1)
            {
                // Exiting right
                newRoomX = RoomX + 1;
                float newRoomLeft = newRoomX * RoomWidth;
                adjustedPos.X = newRoomLeft + PlayerInset - offsetX;
            }
            else if (playerHitbox.Left <= roomLeft && RoomX > 0)
            {
                // Exiting left
                newRoomX = RoomX - 1;
                float newRoomRight = (newRoomX + 1) * RoomWidth;
                adjustedPos.X = newRoomRight - PlayerInset - playerHitbox.Width - offsetX;
            }
            else if (playerHitbox.Bottom >= roomBottom && RoomY < WorldRoomsY - 1)
            {
                // Exiting bottom
                newRoomY = RoomY + 1;
                float newRoomTop = newRoomY * RoomHeight;
                adjustedPos.Y = newRoomTop + PlayerInset - offsetY;
            }
            else if (playerHitbox.Top <= roomTop && RoomY > 0)
            {
                // Exiting top
                newRoomY = RoomY - 1;
                float newRoomBottom = (newRoomY + 1) * RoomHeight;
                adjustedPos.Y = newRoomBottom - PlayerInset - playerHitbox.Height - offsetY;
            }

            // Did we actually change rooms?
            if (newRoomX != RoomX || newRoomY != RoomY)
            {
                PrevRoomX = RoomX;
                PrevRoomY = RoomY;
                RoomX = newRoomX;
                RoomY = newRoomY;

                // Start the camera slide
                _transitionStart = Position;
                _transitionEnd = new Vector2(RoomX * RoomWidth, RoomY * RoomHeight);
                _transitionTimer = 0f;
                IsTransitioning = true;
                RoomChanged = true;

                newPlayerPos = adjustedPos;
                return true;
            }

            return false;
        }

        // ──────────────────────────────────────────────
        //  Update — lerp during transition
        // ──────────────────────────────────────────────

        /// <summary>
        /// Call every frame. Advances the transition if one is active.
        /// </summary>
        public void Update(GameTime gameTime)
        {
            if (!IsTransitioning) return;

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _transitionTimer += dt;

            float t = MathHelper.Clamp(_transitionTimer / TransitionDuration, 0f, 1f);

            // Smooth-step for a polished slide feel
            t = t * t * (3f - 2f * t);

            Position = Vector2.Lerp(_transitionStart, _transitionEnd, t);

            if (_transitionTimer >= TransitionDuration)
            {
                Position = _transitionEnd;
                IsTransitioning = false;
            }
        }

        /// <summary>
        /// Checks if a world-space rectangle is inside the current room.
        /// </summary>
        public bool IsInCurrentRoom(Rectangle worldBounds)
        {
            Rectangle roomRect = new Rectangle(
                RoomX * RoomWidth, RoomY * RoomHeight,
                RoomWidth, RoomHeight);
            return roomRect.Intersects(worldBounds);
        }

        /// <summary>
        /// Checks if a world-space position is inside a specific room.
        /// </summary>
        public static bool IsInRoom(Vector2 worldPos, int roomX, int roomY, int roomW, int roomH)
        {
            Rectangle roomRect = new Rectangle(roomX * roomW, roomY * roomH, roomW, roomH);
            return roomRect.Contains((int)worldPos.X, (int)worldPos.Y);
        }

        /// <summary>
        /// Snap camera to current room (no transition). Used at game start.
        /// </summary>
        public void SnapToRoom(int rx, int ry)
        {
            RoomX = rx;
            RoomY = ry;
            Position = new Vector2(rx * RoomWidth, ry * RoomHeight);
        }

        // ──────────────────────────────────────────────
        //  Screen Shake
        // ──────────────────────────────────────────────

        /// <summary>
        /// Triggers a screen shake effect. Call on enemy death, boss hits, etc.
        /// </summary>
        /// <param name="intensity">Max pixel displacement (3-6 for hits, 8-12 for boss).</param>
        /// <param name="duration">Shake duration in seconds (0.15-0.3 typical).</param>
        public void Shake(float intensity = 4f, float duration = 0.2f)
        {
            // Allow stacking: only override if new shake is stronger
            if (intensity >= _shakeIntensity)
            {
                _shakeIntensity = intensity;
                _shakeDuration = duration;
                _shakeTimer = 0f;
            }
        }

        /// <summary>
        /// Ticks the shake timer. Called from the main Update.
        /// </summary>
        public void UpdateShake(float dt)
        {
            if (_shakeIntensity <= 0f)
            {
                _shakeOffset = Vector2.Zero;
                return;
            }

            _shakeTimer += dt;
            if (_shakeTimer >= _shakeDuration)
            {
                _shakeIntensity = 0f;
                _shakeOffset = Vector2.Zero;
                return;
            }

            // Decay intensity over time for a natural feel
            float decay = 1f - (_shakeTimer / _shakeDuration);
            float currentIntensity = _shakeIntensity * decay;

            // Random offset each frame
            _shakeOffset = new Vector2(
                (_shakeRng.NextSingle() * 2f - 1f) * currentIntensity,
                (_shakeRng.NextSingle() * 2f - 1f) * currentIntensity);
        }
    }
}
