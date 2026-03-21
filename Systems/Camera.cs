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
            -(int)Position.X, -(int)Position.Y, 0f);

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

            // ── Check each edge ──
            if (playerHitbox.Right >= roomRight && RoomX < WorldRoomsX - 1)
            {
                // Exiting right
                newRoomX = RoomX + 1;
                adjustedPos.X = newRoomX * RoomWidth + PlayerInset;
            }
            else if (playerHitbox.Left <= roomLeft && RoomX > 0)
            {
                // Exiting left
                newRoomX = RoomX - 1;
                adjustedPos.X = (newRoomX + 1) * RoomWidth - playerHitbox.Width - PlayerInset;
            }
            else if (playerHitbox.Bottom >= roomBottom && RoomY < WorldRoomsY - 1)
            {
                // Exiting bottom
                newRoomY = RoomY + 1;
                adjustedPos.Y = newRoomY * RoomHeight + PlayerInset;
            }
            else if (playerHitbox.Top <= roomTop && RoomY > 0)
            {
                // Exiting top
                newRoomY = RoomY - 1;
                adjustedPos.Y = (newRoomY + 1) * RoomHeight - playerHitbox.Height - PlayerInset;
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
    }
}
