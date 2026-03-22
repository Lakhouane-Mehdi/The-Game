// ============================================================================
// DialogueBox.cs — Typewriter Dialogue Renderer
// Author: Mehdi Lakhouane
// Description: Displays dialogue text with a typewriter effect using the
//              custom PixelFont. Supports multi-page conversations,
//              speaker names, and input-driven page advancement.
// ============================================================================

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TheGame.Core;
using TheGame.Utils;

namespace TheGame.UI
{
    public class DialogueBox
    {
        // ── Layout ──
        private const int BoxMargin = 16;
        private const int BoxHeight = 100;
        private const int TextPadding = 12;
        private const int TextScale = 2;
        private const int NameScale = 2;
        private const int LineSpacing = 18;

        // ── Typewriter ──
        // At 60 FPS, 0.03s per char = ~2 chars/frame = readable speed
        private const float CharsPerSecond = 35f;

        // ── State ──
        private List<DialoguePage> _pages;
        private int _currentPage;
        private float _charTimer;
        private int _visibleChars;
        private bool _pageComplete;
        private KeyboardState _prevKb = Keyboard.GetState();

        public bool IsActive { get; private set; }
        public bool JustClosed { get; private set; }

        /// <summary>
        /// A single page of dialogue with a speaker name and lines.
        /// </summary>
        public class DialoguePage
        {
            public string Speaker { get; set; }
            public string Text { get; set; }
        }

        /// <summary>
        /// Opens the dialogue box with the given pages.
        /// </summary>
        public void Open(List<DialoguePage> pages)
        {
            if (pages == null || pages.Count == 0) return;
            _pages = pages;
            _currentPage = 0;
            _charTimer = 0f;
            _visibleChars = 0;
            _pageComplete = false;
            IsActive = true;
            JustClosed = false;
        }

        /// <summary>
        /// Opens with a simple single-page message (no speaker name).
        /// </summary>
        public void Open(string text, string speaker = null)
        {
            Open(new List<DialoguePage>
            {
                new DialoguePage { Speaker = speaker, Text = text }
            });
        }

        public void Close()
        {
            IsActive = false;
            JustClosed = true;
        }

        public void Update(GameTime gameTime)
        {
            JustClosed = false;
            if (!IsActive) return;

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            KeyboardState kb = Keyboard.GetState();

            var page = _pages[_currentPage];

            if (!_pageComplete)
            {
                // Typewriter: advance visible characters
                _charTimer += dt * CharsPerSecond;
                _visibleChars = (int)_charTimer;
                if (_visibleChars >= page.Text.Length)
                {
                    _visibleChars = page.Text.Length;
                    _pageComplete = true;
                }

                // Space/Enter: skip to full text
                if ((kb.IsKeyDown(Keys.Space) && _prevKb.IsKeyUp(Keys.Space)) ||
                    (kb.IsKeyDown(Keys.Enter) && _prevKb.IsKeyUp(Keys.Enter)))
                {
                    _visibleChars = page.Text.Length;
                    _pageComplete = true;
                }
            }
            else
            {
                // Page is fully shown — advance or close
                if ((kb.IsKeyDown(Keys.Space) && _prevKb.IsKeyUp(Keys.Space)) ||
                    (kb.IsKeyDown(Keys.Enter) && _prevKb.IsKeyUp(Keys.Enter)))
                {
                    _currentPage++;
                    if (_currentPage >= _pages.Count)
                    {
                        Close();
                    }
                    else
                    {
                        _charTimer = 0f;
                        _visibleChars = 0;
                        _pageComplete = false;
                    }
                }
            }

            _prevKb = kb;
        }

        public void Draw(SpriteBatch sb)
        {
            if (!IsActive) return;

            Texture2D px = Game1.PixelTexture;
            var page = _pages[_currentPage];

            // ── Box position (bottom of screen) ──
            int boxX = BoxMargin;
            int boxY = Game1.ScreenHeight - BoxHeight - BoxMargin;
            int boxW = Game1.ScreenWidth - BoxMargin * 2;

            // ── Draw box background ──
            // Outer border
            sb.Draw(px, new Rectangle(boxX - 2, boxY - 2, boxW + 4, BoxHeight + 4),
                Color.Gold * 0.7f);
            // Inner fill
            sb.Draw(px, new Rectangle(boxX, boxY, boxW, BoxHeight),
                new Color(8, 12, 8) * 0.95f);

            // ── Speaker name tag ──
            int textX = boxX + TextPadding;
            int textY = boxY + TextPadding;

            if (!string.IsNullOrEmpty(page.Speaker))
            {
                // Name background tab
                int nameW = PixelFont.MeasureWidth(page.Speaker, NameScale) + 16;
                int nameH = PixelFont.MeasureHeight(NameScale) + 8;
                sb.Draw(px, new Rectangle(boxX + 8, boxY - nameH - 2, nameW + 4, nameH + 4),
                    Color.Gold * 0.7f);
                sb.Draw(px, new Rectangle(boxX + 10, boxY - nameH, nameW, nameH),
                    new Color(8, 12, 8) * 0.95f);

                PixelFont.DrawString(sb, page.Speaker, boxX + 18, boxY - nameH + 4,
                    Color.Gold, NameScale);
            }

            // ── Typewriter text (word-wrapped) ──
            string visibleText = page.Text.Substring(0, _visibleChars);
            DrawWrappedText(sb, visibleText, textX, textY, boxW - TextPadding * 2,
                new Color(220, 230, 220), TextScale);

            // ── Page indicator ──
            if (_pages.Count > 1)
            {
                string indicator = $"{_currentPage + 1}/{_pages.Count}";
                int indW = PixelFont.MeasureWidth(indicator, 1);
                PixelFont.DrawString(sb, indicator,
                    boxX + boxW - indW - 8, boxY + BoxHeight - 14,
                    Color.Gray * 0.6f, 1);
            }

            // ── Advance prompt (blinking triangle) ──
            if (_pageComplete)
            {
                float blink = MathF.Sin((float)DateTime.Now.TimeOfDay.TotalSeconds * 5f);
                if (blink > 0f)
                {
                    int triX = boxX + boxW - 20;
                    int triY = boxY + BoxHeight - 20;
                    sb.Draw(px, new Rectangle(triX, triY, 6, 2), Color.White * 0.8f);
                    sb.Draw(px, new Rectangle(triX + 1, triY + 2, 4, 2), Color.White * 0.8f);
                    sb.Draw(px, new Rectangle(triX + 2, triY + 4, 2, 2), Color.White * 0.8f);
                }
            }
        }

        /// <summary>
        /// Draws text with word wrapping within the given width.
        /// </summary>
        private void DrawWrappedText(SpriteBatch sb, string text, int x, int y,
            int maxWidth, Color color, int scale)
        {
            int charW = 5 * scale + 1 * scale; // char width + spacing
            int charsPerLine = maxWidth / charW;
            if (charsPerLine <= 0) charsPerLine = 1;

            string[] words = text.Split(' ');
            int curX = x;
            int curY = y;
            int lineChars = 0;

            foreach (string word in words)
            {
                int wordLen = word.Length;
                // Check if word fits on current line
                if (lineChars > 0 && lineChars + 1 + wordLen > charsPerLine)
                {
                    // Wrap to next line
                    curX = x;
                    curY += LineSpacing;
                    lineChars = 0;
                }

                if (lineChars > 0)
                {
                    // Draw space
                    curX += charW;
                    lineChars++;
                }

                PixelFont.DrawString(sb, word, curX, curY, color, scale);
                curX += wordLen * charW;
                lineChars += wordLen;
            }
        }
    }
}
