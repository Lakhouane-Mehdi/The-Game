// ============================================================================
// PixelFont.cs — Bitmap Font Renderer
// Author: Mehdi Lakhouane
// Description: Renders text using a built-in 5x7 pixel font. Each character
//              is defined as a bitmask array drawn with the 1x1 pixel texture.
// ============================================================================

using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheGame.Core;

namespace TheGame.Utils
{
    public static class PixelFont
    {
        private const int CharW = 5;
        private const int CharH = 7;
        private const int DefaultScale = 3;
        private const int DefaultSpacing = 1;

        private static readonly Dictionary<char, byte[]> Glyphs = new()
        {
            ['A'] = new byte[] {
                0b01110,
                0b10001,
                0b10001,
                0b11111,
                0b10001,
                0b10001,
                0b10001 },
            ['B'] = new byte[] {
                0b11110,
                0b10001,
                0b10001,
                0b11110,
                0b10001,
                0b10001,
                0b11110 },
            ['C'] = new byte[] {
                0b01110,
                0b10001,
                0b10000,
                0b10000,
                0b10000,
                0b10001,
                0b01110 },
            ['D'] = new byte[] {
                0b11110,
                0b10001,
                0b10001,
                0b10001,
                0b10001,
                0b10001,
                0b11110 },
            ['E'] = new byte[] {
                0b11111,
                0b10000,
                0b10000,
                0b11110,
                0b10000,
                0b10000,
                0b11111 },
            ['F'] = new byte[] {
                0b11111,
                0b10000,
                0b10000,
                0b11110,
                0b10000,
                0b10000,
                0b10000 },
            ['G'] = new byte[] {
                0b01110,
                0b10001,
                0b10000,
                0b10111,
                0b10001,
                0b10001,
                0b01110 },
            ['H'] = new byte[] {
                0b10001,
                0b10001,
                0b10001,
                0b11111,
                0b10001,
                0b10001,
                0b10001 },
            ['I'] = new byte[] {
                0b11100,
                0b01000,
                0b01000,
                0b01000,
                0b01000,
                0b01000,
                0b11100 },
            ['J'] = new byte[] {
                0b00111,
                0b00010,
                0b00010,
                0b00010,
                0b00010,
                0b10010,
                0b01100 },
            ['K'] = new byte[] {
                0b10001,
                0b10010,
                0b10100,
                0b11000,
                0b10100,
                0b10010,
                0b10001 },
            ['L'] = new byte[] {
                0b10000,
                0b10000,
                0b10000,
                0b10000,
                0b10000,
                0b10000,
                0b11111 },
            ['M'] = new byte[] {
                0b10001,
                0b11011,
                0b10101,
                0b10101,
                0b10001,
                0b10001,
                0b10001 },
            ['N'] = new byte[] {
                0b10001,
                0b11001,
                0b10101,
                0b10011,
                0b10001,
                0b10001,
                0b10001 },
            ['O'] = new byte[] {
                0b01110,
                0b10001,
                0b10001,
                0b10001,
                0b10001,
                0b10001,
                0b01110 },
            ['P'] = new byte[] {
                0b11110,
                0b10001,
                0b10001,
                0b11110,
                0b10000,
                0b10000,
                0b10000 },
            ['Q'] = new byte[] {
                0b01110,
                0b10001,
                0b10001,
                0b10001,
                0b10101,
                0b10010,
                0b01101 },
            ['R'] = new byte[] {
                0b11110,
                0b10001,
                0b10001,
                0b11110,
                0b10100,
                0b10010,
                0b10001 },
            ['S'] = new byte[] {
                0b01110,
                0b10001,
                0b10000,
                0b01110,
                0b00001,
                0b10001,
                0b01110 },
            ['T'] = new byte[] {
                0b11111,
                0b00100,
                0b00100,
                0b00100,
                0b00100,
                0b00100,
                0b00100 },
            ['U'] = new byte[] {
                0b10001,
                0b10001,
                0b10001,
                0b10001,
                0b10001,
                0b10001,
                0b01110 },
            ['V'] = new byte[] {
                0b10001,
                0b10001,
                0b10001,
                0b10001,
                0b01010,
                0b01010,
                0b00100 },
            ['W'] = new byte[] {
                0b10001,
                0b10001,
                0b10001,
                0b10101,
                0b10101,
                0b11011,
                0b10001 },
            ['X'] = new byte[] {
                0b10001,
                0b10001,
                0b01010,
                0b00100,
                0b01010,
                0b10001,
                0b10001 },
            ['Y'] = new byte[] {
                0b10001,
                0b10001,
                0b01010,
                0b00100,
                0b00100,
                0b00100,
                0b00100 },
            ['Z'] = new byte[] {
                0b11111,
                0b00001,
                0b00010,
                0b00100,
                0b01000,
                0b10000,
                0b11111 },
            ['0'] = new byte[] {
                0b01110,
                0b10001,
                0b10011,
                0b10101,
                0b11001,
                0b10001,
                0b01110 },
            ['1'] = new byte[] {
                0b00100,
                0b01100,
                0b00100,
                0b00100,
                0b00100,
                0b00100,
                0b01110 },
            ['2'] = new byte[] {
                0b01110,
                0b10001,
                0b00001,
                0b00110,
                0b01000,
                0b10000,
                0b11111 },
            ['3'] = new byte[] {
                0b01110,
                0b10001,
                0b00001,
                0b00110,
                0b00001,
                0b10001,
                0b01110 },
            ['4'] = new byte[] {
                0b00010,
                0b00110,
                0b01010,
                0b10010,
                0b11111,
                0b00010,
                0b00010 },
            ['5'] = new byte[] {
                0b11111,
                0b10000,
                0b11110,
                0b00001,
                0b00001,
                0b10001,
                0b01110 },
            ['6'] = new byte[] {
                0b01110,
                0b10000,
                0b10000,
                0b11110,
                0b10001,
                0b10001,
                0b01110 },
            ['7'] = new byte[] {
                0b11111,
                0b00001,
                0b00010,
                0b00100,
                0b01000,
                0b01000,
                0b01000 },
            ['8'] = new byte[] {
                0b01110,
                0b10001,
                0b10001,
                0b01110,
                0b10001,
                0b10001,
                0b01110 },
            ['9'] = new byte[] {
                0b01110,
                0b10001,
                0b10001,
                0b01111,
                0b00001,
                0b00001,
                0b01110 },
            [' '] = new byte[] {
                0b00000,
                0b00000,
                0b00000,
                0b00000,
                0b00000,
                0b00000,
                0b00000 },
            ['-'] = new byte[] {
                0b00000,
                0b00000,
                0b00000,
                0b11111,
                0b00000,
                0b00000,
                0b00000 },
            ['+'] = new byte[] {
                0b00000,
                0b00100,
                0b00100,
                0b11111,
                0b00100,
                0b00100,
                0b00000 },
            ['/'] = new byte[] {
                0b00001,
                0b00010,
                0b00010,
                0b00100,
                0b01000,
                0b01000,
                0b10000 },
            [':'] = new byte[] {
                0b00000,
                0b00000,
                0b00100,
                0b00000,
                0b00100,
                0b00000,
                0b00000 },
            ['.'] = new byte[] {
                0b00000,
                0b00000,
                0b00000,
                0b00000,
                0b00000,
                0b00000,
                0b00100 },
            ['!'] = new byte[] {
                0b00100,
                0b00100,
                0b00100,
                0b00100,
                0b00100,
                0b00000,
                0b00100 },
            ['?'] = new byte[] {
                0b01110,
                0b10001,
                0b00001,
                0b00110,
                0b00100,
                0b00000,
                0b00100 },
            [','] = new byte[] {
                0b00000,
                0b00000,
                0b00000,
                0b00000,
                0b00000,
                0b00100,
                0b01000 },
            ['('] = new byte[] {
                0b00010,
                0b00100,
                0b01000,
                0b01000,
                0b01000,
                0b00100,
                0b00010 },
            [')'] = new byte[] {
                0b01000,
                0b00100,
                0b00010,
                0b00010,
                0b00010,
                0b00100,
                0b01000 },
        };

        public static int MeasureWidth(string text, int scale = DefaultScale)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            int spacing = DefaultSpacing * scale;
            return text.Length * (CharW * scale) + (text.Length - 1) * spacing;
        }

        public static int MeasureHeight(int scale = DefaultScale)
        {
            return CharH * scale;
        }

        public static void DrawString(SpriteBatch sb, string text, int x, int y,
            Color? color = null, int scale = DefaultScale)
        {
            Color c = color ?? Color.White;
            Texture2D px = Game1.PixelTexture;
            int spacing = DefaultSpacing * scale;
            int cursorX = x;

            foreach (char ch in text.ToUpper())
            {
                if (Glyphs.TryGetValue(ch, out byte[] glyph))
                {
                    for (int row = 0; row < CharH; row++)
                    {
                        byte bits = glyph[row];
                        for (int col = 0; col < CharW; col++)
                        {
                            if ((bits & (1 << (CharW - 1 - col))) != 0)
                            {
                                sb.Draw(px, new Rectangle(
                                    cursorX + col * scale,
                                    y + row * scale,
                                    scale, scale), c);
                            }
                        }
                    }
                }
                cursorX += CharW * scale + spacing;
            }
        }

        /// <summary>
        /// Draws text with word wrapping within a max pixel width.
        /// Returns the number of lines drawn.
        /// </summary>
        public static int DrawWrapped(SpriteBatch sb, string text, int x, int y,
            int maxWidth, Color? color = null, int scale = DefaultScale, int lineSpacing = 4)
        {
            if (string.IsNullOrEmpty(text)) return 0;

            string[] words = text.Split(' ');
            int spaceW = CharW * scale + DefaultSpacing * scale;
            int cursorX = 0;
            int line = 0;
            int lineH = CharH * scale + lineSpacing;

            foreach (string word in words)
            {
                int wordW = MeasureWidth(word, scale);
                if (cursorX > 0 && cursorX + spaceW + wordW > maxWidth)
                {
                    // Wrap to next line
                    cursorX = 0;
                    line++;
                }

                DrawString(sb, word, x + cursorX, y + line * lineH, color, scale);
                cursorX += wordW + spaceW;
            }

            return line + 1;
        }

        public static void DrawCentered(SpriteBatch sb, string text, int y,
            Color? color = null, int scale = DefaultScale)
        {
            int w = MeasureWidth(text, scale);
            int x = Game1.ScreenWidth / 2 - w / 2;
            DrawString(sb, text, x, y, color, scale);
        }

        public static void DrawCenteredWithShadow(SpriteBatch sb, string text, int y,
            Color? color = null, int scale = DefaultScale)
        {
            int offset = scale > 2 ? 2 : 1;
            Color shadow = Color.Black * 0.8f;

            DrawCentered(sb, text, y + offset, shadow, scale);
            int w = MeasureWidth(text, scale);
            int x = Game1.ScreenWidth / 2 - w / 2;
            DrawString(sb, text, x + offset, y, shadow, scale);

            DrawCentered(sb, text, y, color, scale);
        }
    }
}
