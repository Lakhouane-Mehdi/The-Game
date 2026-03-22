// ============================================================================
// Raccoon.cs — Raccoon Enemy (boss-like, Circle/Dash AI)
// Author: Mehdi Lakhouane
// Description: A larger raccoon enemy that uses advanced steering behaviour:
//              Wanders when idle, chases until within 60px, then orbits the
//              player while occasionally dashing in for an attack.
// ============================================================================

using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheGame.Entities.EnemyStates;
using TheGame.Utils;

namespace TheGame.Entities
{
    public class Raccoon : Enemy
    {
        public Raccoon(Vector2 startPosition)
            : base(startPosition, maxHealth: 5)
        {
            Speed = 55f;
            DetectionRangeValue = 200f;
            AttackRangeValue = 60f;   // switches to circle at this range
            ContactDamage = 2;
            HitboxWidth = 30;
            HitboxHeight = 30;
            HitboxOffset = new Vector2(16, 20);

            // Raccoon sprites are 240x240 — scale down to ~72x72
            DrawScale = 72f / 240f;

            // Replace the default chase/attack states with advanced AI
            SM.AddState("idle",   new EnemyWanderState());  // wander instead of stand still
            SM.AddState("circle", new EnemyCircleState());  // orbit + dash
        }

        public override void LoadContent()
        {
            Animations["idle"] = LoadAnimFromFolder("Sprites/Monsters/raccoon/idle");
            Animations["move"] = LoadAnimFromFolder("Sprites/Monsters/raccoon/move");
            Animations["attack"] = LoadAnimFromFolder("Sprites/Monsters/raccoon/attack", 0.1f);

            Sprite = Animations["idle"].CurrentTexture;
            SpriteSourceRect = Animations["idle"].CurrentSourceRect;
            InitStateMachine();
        }

        /// <summary>
        /// Override UpdateAI to route to Circle state when close enough.
        /// The base Enemy states handle chase → circle transition, but
        /// we override the attack-range check to go to "circle" instead.
        /// </summary>
        public override void Update(GameTime gameTime)
        {
            // Before the SM ticks, check if we should switch to circle
            if (IsAlive && SM.CurrentStateName == "chase")
            {
                float dist = Vector2.Distance(Position, TargetPosition);
                if (dist <= AttackRangeValue)
                {
                    // Hijack: go to circle instead of generic attack
                    SM.SetState("circle");
                }
            }

            base.Update(gameTime);
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
