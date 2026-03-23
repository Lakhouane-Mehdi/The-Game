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

        public void UpgradeMaxHealth(int amount)
        {
            MaxHealth += amount;
            CurrentHealth = MaxHealth;
        }
        private const int AttackReach = 36;
        private const int AttackWidth = 28;

        // ── Input (exposed for states) ──
        public KeyboardState PrevKeyboard { get; private set; }

        // ── Inventory & Party ──
        public Inventory Inventory { get; } = new Inventory();
        public PartyManager Party { get; } = new PartyManager();

        // ── Projectiles (managed by the overworld, spawned by player) ──
        /// <summary>
        /// Callback set by the game state to handle spawning projectiles
        /// into the world's projectile list.
        /// </summary>
        public Action<Projectile> OnSpawnProjectile { get; set; }

        // ── Constructor ──

        public Player(Vector2 startPosition)
            : base(startPosition, maxHealth: 6)
        {
            Speed = 160f;
            DrawScale = 2f;
            HitboxWidth = 28;
            HitboxHeight = 28;
            HitboxOffset = new Vector2(18, 36); // adjusted explicitly for 64x64 bounds
            PrevKeyboard = Keyboard.GetState();

            _sm = new StateMachine<Player>(this);
            _sm.AddState("idle",     new PlayerIdleState());
            _sm.AddState("walk",     new PlayerWalkState());
            _sm.AddState("attack",   new PlayerAttackState());
            _sm.AddState("cooldown", new PlayerCooldownState());
            _sm.AddState("item",     new PlayerItemState());
            _sm.AddState("capture",  new PlayerCaptureState());
        }

        // Layer sets
        private Dictionary<FacingDirection, SpriteAnimation[]> _walkAnimsLayers;
        private Dictionary<FacingDirection, SpriteAnimation[]> _idleAnimsLayers;
        private Dictionary<FacingDirection, SpriteAnimation[]> _attackAnimsLayers;

        public void LoadContent()
        {
            // Base Layers
            Texture2D walkBody = AssetLoader.LoadTexture("Sprites/Player/CuteFantasy/Player_Base_Running.png");
            Texture2D idleBody = AssetLoader.LoadTexture("Sprites/Player/CuteFantasy/Player_Base_Idle.png");
            Texture2D attackBody = AssetLoader.LoadTexture("Sprites/Player/CuteFantasy/Player_Base_Attack.png");
            
            // Accessories Options
            Texture2D walkHair = AssetLoader.LoadTexture("Sprites/Player/CuteFantasy/Hair/Medium_Hair_Brown/Medium_Hair_Brown_Running.png");
            Texture2D idleHair = AssetLoader.LoadTexture("Sprites/Player/CuteFantasy/Hair/Medium_Hair_Brown/Medium_Hair_Brown_Idle.png");
            Texture2D attackHair = AssetLoader.LoadTexture("Sprites/Player/CuteFantasy/Hair/Medium_Hair_Brown/Medium_Hair_Brown_Attack.png");
            
            Texture2D walkShirt = AssetLoader.LoadTexture("Sprites/Player/CuteFantasy/Clothes/Shirt_Green/Shirt_Green_Running.png");
            Texture2D idleShirt = AssetLoader.LoadTexture("Sprites/Player/CuteFantasy/Clothes/Shirt_Green/Shirt_Green_Idle.png");
            Texture2D attackShirt = AssetLoader.LoadTexture("Sprites/Player/CuteFantasy/Clothes/Shirt_Green/Shirt_Green_Attack.png");

            Texture2D walkPants = AssetLoader.LoadTexture("Sprites/Player/CuteFantasy/Clothes/Pants_Blue/Pants_Blue_Running.png");
            Texture2D idlePants = AssetLoader.LoadTexture("Sprites/Player/CuteFantasy/Clothes/Pants_Blue/Pants_Blue_Idle.png");
            Texture2D attackPants = AssetLoader.LoadTexture("Sprites/Player/CuteFantasy/Clothes/Pants_Blue/Pants_Blue_Attack.png");

            Texture2D walkShoes = AssetLoader.LoadTexture("Sprites/Player/CuteFantasy/Clothes/Shoes_Brown/Shoes_Brown_Running.png");
            Texture2D idleShoes = AssetLoader.LoadTexture("Sprites/Player/CuteFantasy/Clothes/Shoes_Brown/Shoes_Brown_Idle.png");
            Texture2D attackShoes = AssetLoader.LoadTexture("Sprites/Player/CuteFantasy/Clothes/Shoes_Brown/Shoes_Brown_Attack.png");

            // Combine into layers: 0=Body, 1=Pants, 2=Shoes, 3=Shirt, 4=Hair
            _walkAnimsLayers = new Dictionary<FacingDirection, SpriteAnimation[]>
            {
                [FacingDirection.Down]  = SetupLayer(walkBody, walkPants, walkShoes, walkShirt, walkHair, 6, 0, 0.1f),
                [FacingDirection.Right] = SetupLayer(walkBody, walkPants, walkShoes, walkShirt, walkHair, 6, 6, 0.1f),
                [FacingDirection.Up]    = SetupLayer(walkBody, walkPants, walkShoes, walkShirt, walkHair, 6, 12, 0.1f),
                [FacingDirection.Left]  = SetupLayer(walkBody, walkPants, walkShoes, walkShirt, walkHair, 6, 6, 0.1f),
            };

            _idleAnimsLayers = new Dictionary<FacingDirection, SpriteAnimation[]>
            {
                [FacingDirection.Down]  = SetupLayer(idleBody, idlePants, idleShoes, idleShirt, idleHair, 6, 0, 0.15f),
                [FacingDirection.Right] = SetupLayer(idleBody, idlePants, idleShoes, idleShirt, idleHair, 6, 6, 0.15f),
                [FacingDirection.Up]    = SetupLayer(idleBody, idlePants, idleShoes, idleShirt, idleHair, 6, 12, 0.15f),
                [FacingDirection.Left]  = SetupLayer(idleBody, idlePants, idleShoes, idleShirt, idleHair, 6, 6, 0.15f),
            };

            // Attack sheet is 4 cols × 9 rows: 3 rows per direction (12 frames each)
            // Down = rows 0-2 (index 0), Right = rows 3-5 (index 12), Up = rows 6-8 (index 24)
            _attackAnimsLayers = new Dictionary<FacingDirection, SpriteAnimation[]>
            {
                [FacingDirection.Down]  = SetupLayer(attackBody, attackPants, attackShoes, attackShirt, attackHair, 4, 0, 0.08f, false),
                [FacingDirection.Right] = SetupLayer(attackBody, attackPants, attackShoes, attackShirt, attackHair, 4, 12, 0.08f, false),
                [FacingDirection.Up]    = SetupLayer(attackBody, attackPants, attackShoes, attackShirt, attackHair, 4, 24, 0.08f, false),
                [FacingDirection.Left]  = SetupLayer(attackBody, attackPants, attackShoes, attackShirt, attackHair, 4, 12, 0.08f, false),
            };

            _sm.ForceState("idle");
        }
        
        private SpriteAnimation[] SetupLayer(Texture2D t1, Texture2D t2, Texture2D t3, Texture2D t4, Texture2D t5, int framesCount, int startIndex, float dur, bool loop = true)
        {
            return new SpriteAnimation[] {
                new SpriteAnimation(t1, 32, 32, framesCount, startIndex, dur, loop),
                new SpriteAnimation(t2, 32, 32, framesCount, startIndex, dur, loop),
                new SpriteAnimation(t3, 32, 32, framesCount, startIndex, dur, loop),
                new SpriteAnimation(t4, 32, 32, framesCount, startIndex, dur, loop),
                new SpriteAnimation(t5, 32, 32, framesCount, startIndex, dur, loop)
            };
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            
            // Play idle animation implicitly if not moving/attacking
            if (_sm.CurrentStateName == "idle")
            {
                UpdateAnimationMap(_idleAnimsLayers, (float)gameTime.ElapsedGameTime.TotalSeconds);
            }
            else if (_sm.CurrentStateName == "attack")
            {
                UpdateAnimationMap(_attackAnimsLayers, (float)gameTime.ElapsedGameTime.TotalSeconds);
            }
            
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
            UpdateAnimationMap(_walkAnimsLayers, dt);
        }
        
        private void UpdateAnimationMap(Dictionary<FacingDirection, SpriteAnimation[]> map, float dt)
        {
            if (map != null && map.ContainsKey(Facing))
            {
                var layers = map[Facing];
                for (int i=0; i<layers.Length; i++) layers[i].Update(dt);
                
                Sprite = layers[0].CurrentTexture;
                SpriteSourceRect = layers[0].CurrentSourceRect;
                
                ExtraLayerSprites.Clear();
                for (int i=1; i<layers.Length; i++) ExtraLayerSprites.Add(layers[i].CurrentTexture);

                foreach (var kv in map)
                    if (kv.Key != Facing) 
                        foreach (var l in kv.Value) l.Reset();
            }
        }

        public void ResetWalkAnimations()
        {
            if (_walkAnimsLayers != null)
                foreach (var kv in _walkAnimsLayers) 
                    foreach (var l in kv.Value) l.Reset();
        }

        public void SetIdleSprite()
        {
            UpdateAnimationMap(_idleAnimsLayers, 0f);
        }

        public void SetAttackSprite()
        {
            UpdateAnimationMap(_attackAnimsLayers, 0f);
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
            if (!Inventory.ConsumeItem(item)) return;

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

            bool isBoomerang = item.EffectType == EffectType.ProjectileReturn;

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

        /// <summary>
        /// Set health directly (for save/load).
        /// </summary>
        public void SetHealth(int hp)
        {
            CurrentHealth = Math.Clamp(hp, 0, MaxHealth);
        }

        // ── Drawing ──

        public override void Draw(SpriteBatch spriteBatch)
        {
            // Use the base Entity.Draw which handles shadow, scale, and invincibility blink
            base.Draw(spriteBatch);
        }
    }
}
