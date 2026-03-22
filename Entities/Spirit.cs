// ============================================================================
// Spirit.cs — Spirit Enemy (Ranged Attacker)
// Author: Mehdi Lakhouane
// Description: A ghostly spirit that floats and shoots projectiles at the
//              player. Uses spirit sprites. Has a ranged attack pattern:
//              hovers at distance, fires periodically, retreats if player
//              gets too close.
// ============================================================================

using System;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheGame.Entities.EnemyStates;
using TheGame.Systems;
using TheGame.Utils;

namespace TheGame.Entities
{
    public class Spirit : Enemy
    {
        // ── Ranged attack ──
        public float ShootCooldown { get; set; } = 2.0f;
        public float ShootTimer { get; set; }
        public bool WantsToShoot { get; set; }
        public Vector2 ShootDirection { get; set; }

        // ── Callback for spawning projectiles into the world ──
        public Action<Vector2, Vector2> OnShootProjectile { get; set; }

        // ── Float bob ──
        private float _bobTimer;
        public float BobOffset { get; private set; }

        public Spirit(Vector2 startPosition)
            : base(startPosition, maxHealth: 2)
        {
            Speed = 40f;
            DetectionRangeValue = 200f;
            AttackRangeValue = 120f; // preferred shooting distance
            ContactDamage = 1;
            HitboxWidth = 20;
            HitboxHeight = 20;
            HitboxOffset = new Vector2(22, 28);

            // Replace idle with wander, add custom ranged state
            SM.AddState("idle", new EnemyWanderState());
            SM.AddState("chase", new SpiritRangedState());
            SM.AddState("attack", new SpiritRangedState());
        }

        public override void LoadContent()
        {
            Animations["idle"] = LoadAnimFromFolder("Sprites/Monsters/spirit/idle");
            Animations["move"] = LoadAnimFromFolder("Sprites/Monsters/spirit/move");
            Animations["attack"] = LoadAnimFromFolder("Sprites/Monsters/spirit/attack", 0.1f);

            Sprite = Animations["idle"].CurrentTexture;
            SpriteSourceRect = Animations["idle"].CurrentSourceRect;
            InitStateMachine();
        }

        public override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _bobTimer += dt;
            BobOffset = MathF.Sin(_bobTimer * 3f) * 4f;
            base.Update(gameTime);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!IsAlive)
            {
                base.Draw(spriteBatch);
                return;
            }

            // Draw with floating bob offset
            Vector2 origPos = Position;
            Position.Y += BobOffset;
            base.Draw(spriteBatch);
            Position = origPos;
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

    /// <summary>
    /// Ranged AI state for Spirit: maintains distance, shoots periodically,
    /// retreats if player gets too close.
    /// </summary>
    public class SpiritRangedState : IState<Enemy>
    {
        private const float PreferredDist = 120f;
        private const float TooCloseDist = 60f;
        private const float ShootInterval = 2.0f;
        private float _shootTimer;

        public void OnEnter(Enemy enemy, Systems.StateMachine<Enemy> sm)
        {
            enemy.PlayAnimation("move");
            _shootTimer = ShootInterval * 0.3f; // first shot comes faster
        }

        public void OnUpdate(Enemy enemy, Systems.StateMachine<Enemy> sm, GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            var spirit = enemy as Spirit;
            if (spirit == null) return;

            Vector2 toPlayer = enemy.TargetPosition - enemy.Position;
            float dist = toPlayer.Length();

            // If player too far, go back to wander
            if (dist > enemy.DetectionRangeValue * 1.5f)
            {
                sm.SetState("idle");
                return;
            }

            Vector2 dir = dist > 0.1f ? toPlayer / dist : Vector2.Zero;

            // Movement: maintain preferred distance
            if (dist < TooCloseDist)
            {
                // Retreat
                enemy.Velocity = -dir * enemy.Speed * 1.5f;
            }
            else if (dist > PreferredDist + 20f)
            {
                // Approach
                enemy.Velocity = dir * enemy.Speed;
            }
            else
            {
                // Strafe sideways
                Vector2 perp = new Vector2(-dir.Y, dir.X);
                enemy.Velocity = perp * enemy.Speed * 0.6f;
            }

            enemy.Position += enemy.Velocity * dt;

            // Face the player
            if (MathF.Abs(toPlayer.X) > MathF.Abs(toPlayer.Y))
                enemy.Facing = toPlayer.X > 0 ? FacingDirection.Right : FacingDirection.Left;
            else
                enemy.Facing = toPlayer.Y > 0 ? FacingDirection.Down : FacingDirection.Up;

            // Shoot timer
            _shootTimer += dt;
            if (_shootTimer >= ShootInterval)
            {
                _shootTimer = 0f;
                spirit.WantsToShoot = true;
                spirit.ShootDirection = dir;
                enemy.PlayAnimation("attack");
            }
            else
            {
                enemy.TickAnimation(dt);
            }
        }

        public void OnExit(Enemy enemy, Systems.StateMachine<Enemy> sm)
        {
            enemy.Velocity = Vector2.Zero;
        }
    }
}
