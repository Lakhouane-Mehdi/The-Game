// ============================================================================
// AssetLoader.cs — Raw Asset Loading (bypasses MGCB pipeline)
// Author: Mehdi Lakhouane
// Description: Loads PNG textures, WAV/OGG audio, and fonts directly from
//              the Content folder without requiring the MGCB content pipeline.
// ============================================================================

using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;

namespace TheGame.Utils
{
    /// <summary>
    /// Helper for loading raw asset files directly from disk.
    /// All paths are relative to the Content/ folder.
    /// </summary>
    public static class AssetLoader
    {
        private static string _contentRoot;
        private static GraphicsDevice _device;

        /// <summary>
        /// Must be called once at startup with the GraphicsDevice.
        /// </summary>
        public static void Initialize(GraphicsDevice device, string contentRoot)
        {
            _device = device;
            _contentRoot = contentRoot;
        }

        /// <summary>
        /// Loads a PNG file as a Texture2D.
        /// Usage: AssetLoader.LoadTexture("Sprites/Player/idle_down.png")
        /// </summary>
        public static Texture2D LoadTexture(string relativePath)
        {
            string fullPath = Path.Combine(_contentRoot, relativePath);
            using FileStream stream = new FileStream(fullPath, FileMode.Open);
            Texture2D texture = Texture2D.FromStream(_device, stream);
            
            // Pre-multiply alpha for proper BlendState.AlphaBlend rendering
            Color[] data = new Color[texture.Width * texture.Height];
            texture.GetData(data);
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = new Color(
                    (byte)(data[i].R * data[i].A / 255),
                    (byte)(data[i].G * data[i].A / 255),
                    (byte)(data[i].B * data[i].A / 255),
                    data[i].A);
            }
            texture.SetData(data);
            
            return texture;
        }

        /// <summary>
        /// Loads a WAV file as a SoundEffect.
        /// Usage: AssetLoader.LoadSound("Audio/SFX/sword.wav")
        /// </summary>
        public static SoundEffect LoadSound(string relativePath)
        {
            string fullPath = Path.Combine(_contentRoot, relativePath);
            using FileStream stream = new FileStream(fullPath, FileMode.Open);
            SoundEffect sfx = SoundEffect.FromStream(stream);
            return sfx;
        }

        /// <summary>
        /// Creates a solid 1x1 pixel texture (for debug drawing rectangles).
        /// </summary>
        public static Texture2D CreatePixel(Color color)
        {
            Texture2D pixel = new Texture2D(_device, 1, 1);
            pixel.SetData(new[] { color });
            return pixel;
        }

        /// <summary>
        /// Returns the full file system path for a content-relative path.
        /// Useful for checking file existence.
        /// </summary>
        public static string GetFullPath(string relativePath)
        {
            return Path.Combine(_contentRoot, relativePath);
        }

        /// <summary>
        /// Checks if a content file exists on disk.
        /// </summary>
        public static bool Exists(string relativePath)
        {
            return File.Exists(Path.Combine(_contentRoot, relativePath));
        }
    }
}
