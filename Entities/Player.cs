// ============================================================================
// Player.cs — Player Controller (StateMachine-driven)
// Author: Mehdi Lakhouane
// Description: 8-directional movement, idle/walk animation, attack and item
//              use via a finite state machine. Each state is a separate class.
//              Supports inventory, boomerang, and projectile spawning.
// ============================================================================

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using TheGame.Entities.Items;
using TheGame.Entities.PlayerStates;
using TheGame.Systems;
using TheGame.Utils;

namespace TheGame.Entities
{
    public class Player : Entity
    {
        // ── State Machine ──
        private StateMachine<Player> _sm;
        public string CurrentStateName => _sm.CurrentStateName;

        // ── Attack Hitbox ──
        public Rectangle AttackHitbox { get; private set; }
        public int AttackDamage { get; set; } = 1;
        private const int AttackReach = 36;
        private const int AttackWidth = 28;

        // ── Input (exposed for states) ──
        public KeyboardState PrevKeyboard { get; private set; }

        // ── Inventory ──
        public Inventory Inventory { get; } = new Inventory();

        // ── Projectiles (managed by the overworld, spawned by player) ──
        /// <summary>
        /// Callback set by the game state to handle spawning projectiles
        /// into the world's projectile list.
        /// </summary>
        public Action<Projectile> OnSpawnProjectile { get; set; }

        // ── Animations ──
        private Dictionary<FacingDirection, SpriteAnimation> _walkAnims;
        private Dictionary<FacingDirection, Texture2D> _idleSprites;
        private Dictionary<FacingDirection, Texture2D> _attackSprites;

        // ── Constructor ──

        public Player(Vector2 startPosition)
            : base(startPosition, maxHealth: 6)
        {
            Speed = 160f;
            HitboxWidth = 24;
            HitboxHeight = 24;
            HitboxOffset = new Vector2(20, 30);

            _sm = new StateMachine<Player>(this);
            _sm.AddState("idle",     new PlayerIdleState());
            _sm.AddState("walk",     new PlayerWalkState());
            _sm.AddState("attack",   new PlayerAttackState());
            _sm.AddState("cooldown", new PlayerCooldownState());
            _sm.AddState("item",     new PlayerItemState());
        }

        public void LoadContent()
        {
            _walkAnims = new Dictionary<FacingDirection, SpriteAnimation>
            {
                [FacingDirection.Down] = new SpriteAnimation(new[]
                {
                    AssetLoader.LoadTexture("Sprites/Player/down_0.png"),
                    AssetLoader.LoadTexture("Sprites/Player/down_1.png"),
                    AssetLoader.LoadTexture("Sprites/Player/down_2.png"),
                    AssetLoader.LoadTexture("Sprites/Player/down_3.png"),
                }, 0.12f),
                [FacingDirection.Up] = new SpriteAnimation(new[]
                {
                    AssetLoader.LoadTexture("Sprites/Player/up_0.png"),
                    AssetLoader.LoadTexture("Sprites/Player/up_1.png"),
                    AssetLoader.LoadTexture("Sprites/Player/up_2.png"),
                    AssetLoader.LoadTexture("Sprites/Player/up_3.png"),
                }, 0.12f),
                [FacingDirection.Left] = new SpriteAnimation(new[]
                {
                    AssetLoader.LoadTexture("Sprites/Player/left_0.png"),
                    AssetLoader.LoadTexture("Sprites/Player/left_1.png"),
                    AssetLoader.LoadTexture("Sprites/Player/left_2.png"),
                    AssetLoader.LoadTexture("Sprites/Player/left_3.png"),
                }, 0.12f),
                [FacingDirection.Right] = new SpriteAnimation(new[]
                {
                    AssetLoader.LoadTexture("Sprites/Player/right_0.png"),
                    AssetLoader.LoadTexture("Sprites/Player/right_1.png"),
                    AssetLoader.LoadTexture("Sprites/Player/right_2.png"),
                    AssetLoader.LoadTexture("Sprites/Player/right_3.png"),
                }, 0.12f),
            };

            _idleSprites = new Dictionary<FacingDirection, Texture2D>
            {
                [FacingDirection.Down] = AssetLoader.LoadTexture("Sprites/Player/idle_down.png"),
                [FacingDirection.Up] = AssetLoader.LoadTexture("Sprites/Player/idle_up.png"),
                [FacingDirection.Left] = AssetLoader.LoadTexture("Sprites/Player/idle_left.png"),
                [FacingDirection.Right] = AssetLoader.LoadTexture("Sprites/Player/idle_right.png"),
            };

            _attackSprites = new Dictionary<FacingDirection, Texture2D>
            {
                [FacingDirection.Down] = AssetLoader.LoadTexture("Sprites/Player/attack_down.png"),
                [FacingDirection.Up] = AssetLoader.LoadTexture("Sprites/Player/attack_up.png"),
                [FacingDirection.Left] = AssetLoader.LoadTexture("Sprites/Player/attack_left.png"),
                [FacingDirection.Right] = AssetLoader.LoadTexture("Sprites/Player/attack_right.png"),
            };

            Sprite = _idleSprites[FacingDirection.Down];
            _sm.ForceState("idle");
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            _sm.Update(gameTime);
            PrevKeyboard = Keyboard.GetState();
        }

        // ── Input helpers ──

        public static Vector2 ReadMovementInput(KeyboardState kb)
        {
            Vector2 input = Vector2.Zero;
            if (kb.IsKeyDown(Keys.W) || kb.IsKeyDown(Keys.Up))    input.Y -= 1;
            if (kb.IsKeyDown(Keys.S) || kb.IsKeyDown(Keys.Down))  input.Y += 1;
            if (kb.IsKeyDown(Keys.A) || kb.IsKeyDown(Keys.Left))  input.X -= 1;
            if (kb.IsKeyDown(Keys.D) || kb.IsKeyDown(Keys.Right)) input.X += 1;
            return input;
        }

        // ── Animation helpers (called by states) ──

        public void UpdateWalkAnimation(float dt)
        {
            if (_walkAnims != null && _walkAnims.ContainsKey(Facing))
            {
                _walkAnims[Facing].Update(dt);
                Sprite = _walkAnims[Facing].CurrentFrame;
                foreach (var kv in _walkAnims)
                    if (kv.Key != Facing) kv.Value.Reset();
            }
        }

        public void ResetWalkAnimations()
        {
            if (_walkAnims != null)
                foreach (var kv in _walkAnims) kv.Value.Reset();
        }

        public void SetIdleSprite()
        {
            if (_idleSprites != null && _idleSprites.ContainsKey(Facing))
                Sprite = _idleSprites[Facing];
        }

        public void SetAttackSprite()
        {
            if (_attackSprites != null && _attackSprites.ContainsKey(Facing))
                Sprite = _attackSprites[Facing];
        }

        // ── Attack hitbox (called by states) ──

        public void ComputeAttackHitbox()
        {
            Rectangle bb = BoundingBox;
            AttackHitbox = Facing switch
            {
                FacingDirection.Up    => new Rectangle(bb.Center.X - AttackWidth / 2, bb.Top - AttackReach, AttackWidth, AttackReach),
                FacingDirection.Down  => new Rectangle(bb.Center.X - AttackWidth / 2, bb.Bottom, AttackWidth, AttackReach),
                FacingDirection.Left  => new Rectangle(bb.Left - AttackReach, bb.Center.Y - AttackWidth / 2, AttackReach, AttackWidth),
                FacingDirection.Right => new Rectangle(bb.Right, bb.Center.Y - AttackWidth / 2, AttackReach, AttackWidth),
                _ => Rectangle.Empty
            };
        }

        public void ClearAttackHitbox()
        {
            AttackHitbox = Rectangle.Empty;
        }

        // ── Item use (called by PlayerItemState and PlayerIdleState) ──

        /// <summary>
        /// Returns the cooldown of the currently equipped item.
        /// </summary>
        public float GetEquippedCooldown()
        {
            return Inventory.EquippedItem?.Cooldown ?? 0.3f;
        }

        /// <summary>
        /// Spawns a projectile based on the equipped item.
        /// Called by PlayerItemState.OnEnter().
        /// </summary>
        public void SpawnItemProjectile()
        {
            var item = Inventory.EquippedItem;
            if (item == null) return;
            if (!Inventory.ConsumeAmmo(item.Id)) return;

            // Direction based on facing
            Vector2 dir = Facing switch
            {
                FacingDirection.Up    => new Vector2(0, -1),
                FacingDirection.Down  => new Vector2(0, 1),
                FacingDirection.Left  => new Vector2(-1, 0),
                FacingDirection.Right => new Vector2(1, 0),
                _ => Vector2.Zero
            };

            // Spawn position (in front of the player)
            Vector2 spawnPos = new Vector2(BoundingBox.Center.X - 16, BoundingBox.Center.Y - 16);

            bool isBoomerang = item.Effect == EffectType.ProjectileReturn;

            // Capture player reference for boomerang return
            Player self = this;
            var proj = new Projectile(
                spawnPos, dir, item.Speed, item.Range, item.Damage,
                isBoomerang,
                isBoomerang ? () => self.Position : null
            );

            proj.Sprite = Inventory.GetSprite(item.Id);

            OnSpawnProjectile?.Invoke(proj);
        }

        /// <summary>
        /// Heal the player (from heart drops).
        /// </summary>
        public void Heal(int amount)
        {
            CurrentHealth = Math.Min(CurrentHealth + amount, MaxHealth);
        }

        // ── Drawing ──

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (Sprite == null || !IsAlive) return;

            Color drawColor = Tint;
            if (IsInvincible && ((int)(InvincibilityTimer * 10) % 2 == 0))
                drawColor = Color.Transparent;

            spriteBatch.Draw(Sprite, Position, drawColor);
        }
    }
}
