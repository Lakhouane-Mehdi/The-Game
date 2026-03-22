// ============================================================================
// TileMap.cs — Tile Map Renderer
// Author: Mehdi Lakhouane
// Description: Reads a 2D integer array and renders tiles from a tileset
//              spritesheet. Each tile ID maps to a specific 16x16 region
//              in the tileset texture via source rectangle math.
// ============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TheGame.Systems
{
    public class TileMap
    {
        // ── Tile dimensions (pixels in the source tileset) ──
        public int TileWidth { get; }
        public int TileHeight { get; }

        // ── The tileset spritesheet texture ──
        public Texture2D Tileset { get; set; }

        // ── How many tiles fit across one row of the tileset ──
        public int TilesetColumns { get; private set; }

        // ── The map data: mapData[row, col] = tile ID ──
        private int[,] _mapData;

        // ── Map dimensions in tiles ──
        public int MapWidth { get; private set; }
        public int MapHeight { get; private set; }

        // ── Render scale (1 = native 16px, 2 = 32px, etc.) ──
        public int RenderScale { get; set; } = 1;

        /// <summary>
        /// Creates a new TileMap with the given tile size.
        /// Default tile size is 16x16 (standard for Ninja Adventure).
        /// </summary>
        public TileMap(int tileWidth = 16, int tileHeight = 16)
        {
            TileWidth = tileWidth;
            TileHeight = tileHeight;
        }

        /// <summary>
        /// Sets the tileset texture and calculates how many columns it has.
        /// </summary>
        public void SetTileset(Texture2D tileset)
        {
            Tileset = tileset;
            TilesetColumns = tileset.Width / TileWidth;
        }

        /// <summary>
        /// Loads map data from a 2D integer array.
        /// Each value is a tile ID: -1 means empty/transparent.
        /// Tile IDs start at 0, reading left-to-right, top-to-bottom
        /// across the tileset image.
        /// </summary>
        public void LoadFromArray(int[,] data)
        {
            _mapData = data;
            MapHeight = data.GetLength(0);
            MapWidth = data.GetLength(1);
        }

        /// <summary>
        /// Loads map data from a CSV string.
        /// Each line is a row, values separated by commas.
        /// Example: "0,1,2\n3,4,5"
        /// </summary>
        public void LoadFromCSV(string csv)
        {
            string[] lines = csv.Trim().Split('\n');
            int rows = lines.Length;
            // Determine column count from first row
            string[] firstRow = lines[0].Trim().Split(',');
            int cols = firstRow.Length;

            int[,] data = new int[rows, cols];
            for (int r = 0; r < rows; r++)
            {
                string[] values = lines[r].Trim().Split(',');
                for (int c = 0; c < cols; c++)
                {
                    if (c < values.Length && int.TryParse(values[c].Trim(), out int id))
                        data[r, c] = id;
                    else
                        data[r, c] = -1; // empty
                }
            }
            LoadFromArray(data);
        }

        /// <summary>
        /// Loads map data from a CSV file on disk.
        /// </summary>
        public void LoadFromCSVFile(string filePath)
        {
            if (File.Exists(filePath))
                LoadFromCSV(File.ReadAllText(filePath));
        }

        /// <summary>
        /// Gets the source rectangle for a given tile ID.
        ///
        /// SOURCE RECTANGLE MATH:
        /// ─────────────────────
        /// Given a tileset image that is W pixels wide with 16x16 tiles,
        /// there are (W / 16) tiles per row.
        ///
        /// For tile ID = n:
        ///   column = n % columns
        ///   row    = n / columns
        ///   sourceX = column * TileWidth
        ///   sourceY = row    * TileHeight
        ///
        /// This gives us a Rectangle(sourceX, sourceY, TileWidth, TileHeight)
        /// that we pass as the sourceRectangle to SpriteBatch.Draw().
        /// </summary>
        public Rectangle GetSourceRect(int tileId)
        {
            if (TilesetColumns <= 0) return Rectangle.Empty;

            int col = tileId % TilesetColumns;
            int row = tileId / TilesetColumns;

            return new Rectangle(
                col * TileWidth,
                row * TileHeight,
                TileWidth,
                TileHeight);
        }

        /// <summary>
        /// Draws the entire tile map.
        /// Each tile is rendered at (col * TileWidth * RenderScale, row * TileHeight * RenderScale)
        /// with an optional offset for camera/scrolling.
        /// </summary>
        public void Draw(SpriteBatch spriteBatch, Vector2 offset = default, Color? tint = null)
        {
            if (Tileset == null || _mapData == null) return;

            Color color = tint ?? Color.White;
            int scaledW = TileWidth * RenderScale;
            int scaledH = TileHeight * RenderScale;

            for (int row = 0; row < MapHeight; row++)
            {
                for (int col = 0; col < MapWidth; col++)
                {
                    int tileId = _mapData[row, col];
                    if (tileId < 0) continue; // skip empty tiles

                    Rectangle source = GetSourceRect(tileId);
                    Rectangle dest = new Rectangle(
                        (int)(col * scaledW + offset.X),
                        (int)(row * scaledH + offset.Y),
                        scaledW,
                        scaledH);

                    spriteBatch.Draw(Tileset, dest, source, color);
                }
            }
        }

        /// <summary>
        /// Draws only the tiles visible within a given viewport rectangle.
        /// More efficient for large maps — skips off-screen tiles.
        /// </summary>
        public void DrawVisible(SpriteBatch spriteBatch, Rectangle viewport,
            Vector2 offset = default, Color? tint = null)
        {
            if (Tileset == null || _mapData == null) return;

            Color color = tint ?? Color.White;
            int scaledW = TileWidth * RenderScale;
            int scaledH = TileHeight * RenderScale;

            // Calculate visible tile range
            int startCol = Math.Max(0, (int)((viewport.Left - offset.X) / scaledW));
            int endCol = Math.Min(MapWidth, (int)((viewport.Right - offset.X) / scaledW) + 1);
            int startRow = Math.Max(0, (int)((viewport.Top - offset.Y) / scaledH));
            int endRow = Math.Min(MapHeight, (int)((viewport.Bottom - offset.Y) / scaledH) + 1);

            for (int row = startRow; row < endRow; row++)
            {
                for (int col = startCol; col < endCol; col++)
                {
                    int tileId = _mapData[row, col];
                    if (tileId < 0) continue;

                    Rectangle source = GetSourceRect(tileId);
                    Rectangle dest = new Rectangle(
                        (int)(col * scaledW + offset.X),
                        (int)(row * scaledH + offset.Y),
                        scaledW,
                        scaledH);

                    spriteBatch.Draw(Tileset, dest, source, color);
                }
            }
        }

        /// <summary>
        /// Gets the tile ID at a world position (accounting for render scale and offset).
        /// Returns -1 if out of bounds.
        /// </summary>
        public int GetTileAtWorld(float worldX, float worldY, Vector2 offset = default)
        {
            int scaledW = TileWidth * RenderScale;
            int scaledH = TileHeight * RenderScale;

            int col = (int)((worldX - offset.X) / scaledW);
            int row = (int)((worldY - offset.Y) / scaledH);

            if (col < 0 || col >= MapWidth || row < 0 || row >= MapHeight)
                return -1;

            return _mapData[row, col];
        }

        /// <summary>
        /// Sets a tile ID at a specific map position.
        /// </summary>
        public void SetTile(int row, int col, int tileId)
        {
            if (row >= 0 && row < MapHeight && col >= 0 && col < MapWidth)
                _mapData[row, col] = tileId;
        }
    }
}
