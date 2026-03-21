// ============================================================================
// Enemy.cs — Base Enemy Class (StateMachine-driven)
// Author: Mehdi Lakhouane
// Description: Abstract base for all enemy types. Uses a StateMachine<Enemy>
//              with Idle/Chase/Attack/Hurt/Dead states. Exposes public
//              properties and helpers so state classes can drive behaviour.
// ============================================================================

using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheGame.Entities.EnemyStates;
using TheGame.Systems;
using TheGame.Utils;

namespace TheGame.Entities
{
    public abstract class Enemy : Entity
    {
        // ── State Machine ──
        protected StateMachine<Enemy> SM;

        /// <summary>
        /// The name of the currently active state.
        /// </summary>
        public string CurrentStateName => SM.CurrentStateName;

        // ── AI parameters (exposed for states to read) ──
        public float DetectionRangeValue { get; protected set; } = 150f;
        public float AttackRangeValue { get; protected set; } = 30f;
        public int ContactDamage { get; protected set; } = 1;

        // ── Target tracking (set by the game state each frame) ──
        /// <summary>
        /// The position the AI should chase / face. Usually the player's position.
        /// Set this each frame before calling Update.
        /// </summary>
        public Vector2 TargetPosition;

        // ── Knockback (written by HurtState) ──
        public Vector2 KnockbackVelocity;

        // ── Death ──
        public const float DeathFadeTime = 0.5f;
        public bool IsMarkedForRemoval;

        // ── Animations ──
        protected Dictionary<string, SpriteAnimation> Animations;
        private string _currentAnimKey;

        // ── Constructor ──

        protected Enemy(Vector2 startPosition, int maxHealth)
            : base(startPosition, maxHealth)
        {
            Speed = 60f;
            Animations = new Dictionary<string, SpriteAnimation>();

            // Build the state machine with the standard enemy states
            SM = new StateMachine<Enemy>(this);
            SM.AddState("idle",   new EnemyIdleState());
            SM.AddState("chase",  new EnemyChaseState());
            SM.AddState("attack", new EnemyAttackState());
            SM.AddState("hurt",   new EnemyHurtState());
            SM.AddState("dead",   new EnemyDeadState());
        }

        /// <summary>
        /// Override in subclasses to load monster-specific sprites,
        /// then call InitStateMachine() when done.
        /// </summary>
        public abstract void LoadContent();

        /// <summary>
        /// Call after LoadContent to start the state machine.
        /// </summary>
        protected void InitStateMachine()
        {
            SM.ForceState("idle");
        }

        // ── Update ──

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);  // ticks invincibility

            if (!IsAlive && !SM.IsInState("dead"))
            {
                SM.SetState("dead");
                return;
            }

            SM.Update(gameTime);
        }

        /// <summary>
        /// Convenience method: set the player position before updating.
        /// Called from the game state's Update loop.
        /// </summary>
        public void UpdateAI(GameTime gameTime, Vector2 playerPosition)
        {
            TargetPosition = playerPosition;
            // State machine is ticked via Update() which is called separately
        }

        // ── Damage ──

        public override void TakeDamage(int amount)
        {
            if (IsInvincible || !IsAlive) return;
            base.TakeDamage(amount);

            if (!IsAlive)
                SM.SetState("dead");
            else
                SM.SetState("hurt");
        }

        // ── Animation helpers (called by states) ──

        /// <summary>
        /// Switch to a named animation (e.g. "idle", "move", "attack").
        /// </summary>
        public void PlayAnimation(string animKey)
        {
            if (Animations.ContainsKey(animKey))
            {
                _currentAnimKey = animKey;
                Animations[animKey].Reset();
            }
        }

        /// <summary>
        /// Advance the current animation by dt seconds.
        /// </summary>
        public void TickAnimation(float dt)
        {
            if (_currentAnimKey != null && Animations.ContainsKey(_currentAnimKey))
            {
                Animations[_currentAnimKey].Update(dt);
                Sprite = Animations[_currentAnimKey].CurrentFrame;
            }
        }

        // ── Drawing ──

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!IsAlive)
            {
                // Fade-out during dead state
                if (SM.IsInState("dead") && SM.StateTimer < DeathFadeTime && Sprite != null)
                {
                    float alpha = 1f - (SM.StateTimer / DeathFadeTime);
                    spriteBatch.Draw(Sprite, Position, Color.White * alpha);
                }
                return;
            }

            base.Draw(spriteBatch);
        }

        /// <summary>
        /// True when the enemy can be safely removed from the entity list.
        /// </summary>
        public bool CanRemove => IsMarkedForRemoval;
    }
}
