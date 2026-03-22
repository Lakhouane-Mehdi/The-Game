// ============================================================================
// DungeonBoss.cs — The Silence Guardian (Dungeon Boss)
// Author: Mehdi Lakhouane
// Description: Multi-phase boss fight for the dungeon finale.
//   Phase 1 (HP > 50%): Circle + dash (like raccoon but faster)
//   Phase 2 (HP <= 50%): Adds spirit projectile bursts between dashes
//   Phase 3 (HP <= 25%): Enraged — faster, more projectiles, slam attack
//   Uses squid sprites (large tentacled creature fits the boss theme).
// ============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheGame.Core;
using TheGame.Systems;
using TheGame.Utils;

namespace TheGame.Entities
{
    public class DungeonBoss : Enemy
    {
        // ── Phase tracking ──
        public int Phase { get; private set; } = 1;
        public bool IsDefeated { get; private set; }

        // ── Projectile spawning ──
        public Action<Vector2, Vector2> OnShootProjectile { get; set; }

        // ── Boss-specific timers ──
        public float ActionTimer { get; set; }
        public float DashTimer { get; set; }
        public bool IsDashing { get; set; }
        public float DashClock { get; set; }
        public Vector2 DashDirection { get; set; }
        public float BurstTimer { get; set; }
        public bool IsVulnerable { get; set; } = true;

        // ── Slam attack ──
        public bool IsSlamming { get; set; }
        public float SlamTimer { get; set; }
        public Vector2 SlamTarget { get; set; }
        public bool SlamLanded { get; set; }

        // ── Health bar flash ──
        public float DamageFlash { get; set; }

        // ── Orbit ──
        public float OrbitAngle { get; set; }

        // ── Telegraph indicator ──
        public Vector2 TelegraphTarget { get; set; }
        public float TelegraphTimer { get; set; }
        public bool ShowTelegraph { get; set; }

        public void SetContactDamage(int dmg) => ContactDamage = dmg;

        public DungeonBoss(Vector2 startPosition)
            : base(startPosition, maxHealth: 20)
        {
            Speed = 65f;
            DetectionRangeValue = 400f; // always active in boss room
            AttackRangeValue = 80f;
            ContactDamage = 2;
            HitboxWidth = 40;
            HitboxHeight = 40;
            HitboxOffset = new Vector2(12, 16);

            // Squid sprites are large — scale to ~80x80
            DrawScale = 80f / 64f;

            // Boss uses custom state machine
            SM.AddState("idle", new BossIdleState());
            SM.AddState("chase", new BossChaseState());
            SM.AddState("attack", new BossAttackState());
        }

        public override void LoadContent()
        {
            Animations["idle"] = LoadAnimFromFolder("Sprites/Monsters/squid/idle");
            Animations["move"] = LoadAnimFromFolder("Sprites/Monsters/squid/idle"); // use idle for move too
            Animations["attack"] = LoadAnimFromFolder("Sprites/Monsters/squid/attack", 0.08f);

            Sprite = Animations["idle"].CurrentTexture;
            SpriteSourceRect = Animations["idle"].CurrentSourceRect;
            InitStateMachine();
        }

        public override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Update phase based on HP
            float hpPercent = (float)CurrentHealth / MaxHealth;
            if (hpPercent <= 0.25f) Phase = 3;
            else if (hpPercent <= 0.5f) Phase = 2;
            else Phase = 1;

            if (DamageFlash > 0f) DamageFlash -= dt;

            base.Update(gameTime);
        }

        public override void TakeDamage(int amount)
        {
            if (!IsVulnerable) return;
            base.TakeDamage(amount);
            DamageFlash = 0.15f;

            if (!IsAlive)
                IsDefeated = true;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!IsAlive)
            {
                base.Draw(spriteBatch);
                return;
            }

            // Draw telegraph indicator
            if (ShowTelegraph && TelegraphTimer > 0f)
            {
                Texture2D px = Game1.PixelTexture;
                float pulse = MathF.Sin(TelegraphTimer * 15f) * 0.3f + 0.5f;
                int size = IsSlamming ? 60 : 30;
                spriteBatch.Draw(px, new Rectangle(
                    (int)TelegraphTarget.X - size / 2,
                    (int)TelegraphTarget.Y - size / 2,
                    size, size), Color.Red * pulse);
            }

            // Damage flash tint
            Color origTint = Tint;
            if (DamageFlash > 0f)
                Tint = Color.Red;

            // Draw boss with phase-colored aura
            Texture2D px2 = Game1.PixelTexture;
            Color auraColor = Phase switch
            {
                1 => new Color(100, 50, 150) * 0.2f,
                2 => new Color(150, 50, 50) * 0.3f,
                3 => new Color(200, 30, 30) * 0.4f,
                _ => Color.Transparent
            };

            // Aura glow
            int aw = (int)(HitboxWidth * 2.5f);
            int ah = (int)(HitboxHeight * 2.5f);
            float auraPulse = MathF.Sin(ActionTimer * 4f) * 0.1f;
            spriteBatch.Draw(px2, new Rectangle(
                (int)(Position.X + HitboxOffset.X + HitboxWidth / 2 - aw / 2),
                (int)(Position.Y + HitboxOffset.Y + HitboxHeight / 2 - ah / 2),
                aw, ah), auraColor * (0.3f + auraPulse));

            base.Draw(spriteBatch);
            Tint = origTint;

            // Draw boss health bar
            DrawHealthBar(spriteBatch);
        }

        private void DrawHealthBar(SpriteBatch sb)
        {
            Texture2D px = Game1.PixelTexture;
            int barW = 200;
            int barH = 12;
            int barX = Game1.ScreenWidth / 2 - barW / 2;
            int barY = Game1.ScreenHeight - 40;

            // Background
            sb.Draw(px, new Rectangle(barX - 2, barY - 2, barW + 4, barH + 4), Color.Black * 0.8f);

            // HP fill
            float ratio = MathF.Max(0f, (float)CurrentHealth / MaxHealth);
            Color barColor = Phase switch
            {
                1 => Color.Green,
                2 => Color.Orange,
                3 => Color.Red,
                _ => Color.Green
            };
            sb.Draw(px, new Rectangle(barX, barY, (int)(barW * ratio), barH), barColor);

            // Border
            sb.Draw(px, new Rectangle(barX - 2, barY - 2, barW + 4, 2), Color.White * 0.5f);
            sb.Draw(px, new Rectangle(barX - 2, barY + barH, barW + 4, 2), Color.White * 0.5f);
            sb.Draw(px, new Rectangle(barX - 2, barY - 2, 2, barH + 4), Color.White * 0.5f);
            sb.Draw(px, new Rectangle(barX + barW, barY - 2, 2, barH + 4), Color.White * 0.5f);

            // Boss name
            PixelFont.DrawString(sb, "THE SILENCE GUARDIAN", barX, barY - 16, Color.White, 1);

            // Phase indicator
            string phaseText = Phase switch
            {
                1 => "PHASE I",
                2 => "PHASE II - ENRAGED",
                3 => "PHASE III - DESPERATE",
                _ => ""
            };
            PixelFont.DrawString(sb, phaseText, barX + barW - phaseText.Length * 6, barY - 16,
                Phase >= 3 ? Color.Red : Phase >= 2 ? Color.Orange : Color.Gray, 1);
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

    // ─────────────────────────────────────────────
    //  Boss States
    // ─────────────────────────────────────────────

    public class BossIdleState : IState<Enemy>
    {
        private float _waitTimer;

        public void OnEnter(Enemy enemy, StateMachine<Enemy> sm)
        {
            enemy.PlayAnimation("idle");
            enemy.Velocity = Vector2.Zero;
            _waitTimer = 0f;
        }

        public void OnUpdate(Enemy enemy, StateMachine<Enemy> sm, GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _waitTimer += dt;
            enemy.TickAnimation(dt);

            // Always engage
            if (_waitTimer > 0.5f)
            {
                sm.SetState("chase");
            }
        }

        public void OnExit(Enemy enemy, StateMachine<Enemy> sm) { }
    }

    public class BossChaseState : IState<Enemy>
    {
        private const float OrbitRadius = 90f;
        private const float AngularSpeed = 1.5f;
        private float _orbitAngle;
        private float _attackTimer;

        public void OnEnter(Enemy enemy, StateMachine<Enemy> sm)
        {
            enemy.PlayAnimation("move");
            Vector2 toEnemy = enemy.Position - enemy.TargetPosition;
            _orbitAngle = MathF.Atan2(toEnemy.Y, toEnemy.X);
            _attackTimer = 0f;
        }

        public void OnUpdate(Enemy enemy, StateMachine<Enemy> sm, GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            var boss = enemy as DungeonBoss;
            if (boss == null) return;

            boss.ActionTimer += dt;

            // Orbit the player
            float speedMult = boss.Phase >= 3 ? 2.0f : boss.Phase >= 2 ? 1.5f : 1.0f;
            _orbitAngle += AngularSpeed * speedMult * dt;

            Vector2 desiredPos = enemy.TargetPosition + new Vector2(
                MathF.Cos(_orbitAngle) * OrbitRadius,
                MathF.Sin(_orbitAngle) * OrbitRadius);

            Vector2 toDesired = desiredPos - enemy.Position;
            float dist = toDesired.Length();
            if (dist > 1f)
            {
                toDesired.Normalize();
                enemy.Velocity = toDesired * 120f * speedMult;
                enemy.Position += enemy.Velocity * dt;
            }

            // Face player
            Vector2 toPlayer = enemy.TargetPosition - enemy.Position;
            if (MathF.Abs(toPlayer.X) > MathF.Abs(toPlayer.Y))
                enemy.Facing = toPlayer.X > 0 ? FacingDirection.Right : FacingDirection.Left;
            else
                enemy.Facing = toPlayer.Y > 0 ? FacingDirection.Down : FacingDirection.Up;

            enemy.TickAnimation(dt);

            // Attack interval based on phase
            float attackInterval = boss.Phase >= 3 ? 1.5f : boss.Phase >= 2 ? 2.5f : 3.5f;
            _attackTimer += dt;
            if (_attackTimer >= attackInterval)
            {
                _attackTimer = 0f;
                sm.SetState("attack");
            }
        }

        public void OnExit(Enemy enemy, StateMachine<Enemy> sm)
        {
            enemy.Velocity = Vector2.Zero;
        }
    }

    public class BossAttackState : IState<Enemy>
    {
        private enum AttackType { Dash, Burst, Slam }
        private AttackType _currentAttack;
        private float _timer;
        private float _telegraphTime = 0.6f;
        private bool _telegraphDone;
        private Vector2 _attackDir;
        private static readonly Random _rng = new();

        public void OnEnter(Enemy enemy, StateMachine<Enemy> sm)
        {
            var boss = enemy as DungeonBoss;
            if (boss == null) return;

            enemy.PlayAnimation("attack");
            _timer = 0f;
            _telegraphDone = false;

            // Choose attack based on phase
            if (boss.Phase >= 3)
            {
                int roll = _rng.Next(3);
                _currentAttack = roll == 0 ? AttackType.Slam : roll == 1 ? AttackType.Burst : AttackType.Dash;
            }
            else if (boss.Phase >= 2)
            {
                _currentAttack = _rng.Next(2) == 0 ? AttackType.Burst : AttackType.Dash;
            }
            else
            {
                _currentAttack = AttackType.Dash;
            }

            // Telegraph
            Vector2 toPlayer = enemy.TargetPosition - enemy.Position;
            if (toPlayer != Vector2.Zero) toPlayer.Normalize();
            _attackDir = toPlayer;

            boss.ShowTelegraph = true;
            boss.TelegraphTarget = enemy.TargetPosition;
            boss.TelegraphTimer = _telegraphTime;
            boss.IsSlamming = _currentAttack == AttackType.Slam;
        }

        public void OnUpdate(Enemy enemy, StateMachine<Enemy> sm, GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            var boss = enemy as DungeonBoss;
            if (boss == null) return;

            _timer += dt;
            boss.TelegraphTimer -= dt;

            // Telegraph phase
            if (!_telegraphDone && _timer < _telegraphTime)
            {
                enemy.Velocity = Vector2.Zero;
                enemy.TickAnimation(dt);
                return;
            }

            if (!_telegraphDone)
            {
                _telegraphDone = true;
                boss.ShowTelegraph = false;
                _timer = 0f;
            }

            switch (_currentAttack)
            {
                case AttackType.Dash:
                    ExecuteDash(boss, dt, sm);
                    break;
                case AttackType.Burst:
                    ExecuteBurst(boss, dt, sm);
                    break;
                case AttackType.Slam:
                    ExecuteSlam(boss, dt, sm);
                    break;
            }

            enemy.TickAnimation(dt);
        }

        private void ExecuteDash(DungeonBoss boss, float dt, StateMachine<Enemy> sm)
        {
            float dashDuration = 0.5f;
            float dashSpeed = boss.Phase >= 3 ? 350f : 280f;

            boss.Position += _attackDir * dashSpeed * dt;
            boss.SetContactDamage(3);

            if (_timer >= dashDuration)
            {
                boss.SetContactDamage(2);
                sm.SetState("chase");
            }
        }

        private void ExecuteBurst(DungeonBoss boss, float dt, StateMachine<Enemy> sm)
        {
            // Fire projectiles in a ring pattern
            if (_timer < 0.1f && boss.OnShootProjectile != null)
            {
                int count = boss.Phase >= 3 ? 8 : 5;
                for (int i = 0; i < count; i++)
                {
                    float angle = (MathF.PI * 2f / count) * i;
                    Vector2 dir = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
                    Vector2 spawnPos = boss.Position + boss.HitboxOffset + new Vector2(boss.HitboxWidth / 2f, boss.HitboxHeight / 2f);
                    boss.OnShootProjectile(spawnPos, dir);
                }
            }

            if (_timer >= 0.8f)
            {
                sm.SetState("chase");
            }
        }

        private void ExecuteSlam(DungeonBoss boss, float dt, StateMachine<Enemy> sm)
        {
            float jumpDuration = 0.4f;
            float landDelay = 0.3f;
            float totalDuration = jumpDuration + landDelay + 0.5f;

            if (_timer < jumpDuration)
            {
                // Jump toward player position
                boss.Position += _attackDir * 300f * dt;
                boss.IsVulnerable = false;
            }
            else if (_timer < jumpDuration + landDelay)
            {
                // Hang in air (no movement)
                boss.Velocity = Vector2.Zero;
            }
            else if (!boss.SlamLanded)
            {
                // Slam down — shockwave
                boss.SlamLanded = true;
                boss.IsVulnerable = true;
                boss.SetContactDamage(4);

                // Spawn ring of projectiles on landing
                if (boss.OnShootProjectile != null)
                {
                    for (int i = 0; i < 12; i++)
                    {
                        float angle = (MathF.PI * 2f / 12) * i;
                        Vector2 dir = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
                        Vector2 spawnPos = boss.Position + boss.HitboxOffset + new Vector2(boss.HitboxWidth / 2f, boss.HitboxHeight / 2f);
                        boss.OnShootProjectile(spawnPos, dir);
                    }
                }
            }

            if (_timer >= totalDuration)
            {
                boss.SetContactDamage(2);
                boss.SlamLanded = false;
                sm.SetState("chase");
            }
        }

        public void OnExit(Enemy enemy, StateMachine<Enemy> sm)
        {
            var boss = enemy as DungeonBoss;
            if (boss != null)
            {
                boss.ShowTelegraph = false;
                boss.IsVulnerable = true;
            }
            enemy.Velocity = Vector2.Zero;
        }
    }
}
