// ============================================================================
// Bamboo.cs — Bamboo Enemy
// Author: Mehdi Lakhouane
// Description: A bamboo monster that uses the Ninja Adventure bamboo sprites.
//              Inherits the StateMachine-driven Enemy base class.
// ============================================================================

using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheGame.Entities.EnemyStates;
using TheGame.Utils;

namespace TheGame.Entities
{
    public class Bamboo : Enemy
    {
        public Bamboo(Vector2 startPosition)
            : base(startPosition, maxHealth: 3)
        {
            Speed = 50f;
            DetectionRangeValue = 160f;
            AttackRangeValue = 24f;
            ContactDamage = 1;
            HitboxWidth = 22;
            HitboxHeight = 22;
            HitboxOffset = new Vector2(20, 28);

            // Bamboo wanders when not chasing (more lifelike than standing still)
            SM.AddState("idle", new EnemyWanderState());
        }

        public override void LoadContent()
        {
            Animations["idle"] = LoadAnimFromFolder("Sprites/Monsters/bamboo/idle");
            Animations["move"] = LoadAnimFromFolder("Sprites/Monsters/bamboo/move");
            Animations["attack"] = LoadAnimFromFolder("Sprites/Monsters/bamboo/attack", 0.1f);

            Sprite = Animations["idle"].CurrentFrame;
            InitStateMachine();
        }

        private SpriteAnimation LoadAnimFromFolder(string folder, float frameDuration = 0.15f)
        {
            string fullPath = AssetLoader.GetFullPath(folder);
            string[] files = Directory.GetFiles(fullPath, "*.png")
                .OrderBy(f => f)
                .ToArray();

            Texture2D[] frames = new Texture2D[files.Length];
            for (int i = 0; i < files.Length; i++)
            {
                string relative = Path.GetRelativePath(
                    AssetLoader.GetFullPath(""), files[i]);
                frames[i] = AssetLoader.LoadTexture(relative);
            }

            return new SpriteAnimation(frames, frameDuration);
        }
    }
}
