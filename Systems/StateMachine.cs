// ============================================================================
// StateMachine.cs — Generic Finite State Machine
// Author: Mehdi Lakhouane
// Description: A reusable, generic FSM that can drive any entity (Player,
//              Enemy, Boss, NPC, UI screens, etc.). Each state is a self-
//              contained class with OnEnter / OnUpdate / OnExit hooks.
//
// Architecture:
//   StateMachine<T> holds a dictionary of named IState<T> instances.
//   T is the "owner" — the entity that owns this state machine.
//   States receive a reference to the owner so they can read/write its
//   fields directly (position, velocity, animations, etc.).
//
// Usage:
//   var sm = new StateMachine<Player>(player);
//   sm.AddState("idle",      new PlayerIdleState());
//   sm.AddState("walk",      new PlayerWalkState());
//   sm.AddState("attack",    new PlayerAttackState());
//   sm.AddState("cooldown",  new PlayerCooldownState());
//   sm.SetState("idle");
//
//   // Every frame:
//   sm.Update(gameTime);
// ============================================================================

using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace TheGame.Systems
{
    /// <summary>
    /// Contract for a single state. T is the owner entity type.
    /// </summary>
    public interface IState<T>
    {
        /// <summary>
        /// Called once when transitioning INTO this state.
        /// Use for: resetting timers, playing enter-animations, setting flags.
        /// </summary>
        void OnEnter(T owner, StateMachine<T> sm);

        /// <summary>
        /// Called every frame while this state is active.
        /// Use for: reading input, moving the entity, checking transitions.
        /// Call sm.SetState("next") from here to trigger a transition.
        /// </summary>
        void OnUpdate(T owner, StateMachine<T> sm, GameTime gameTime);

        /// <summary>
        /// Called once when transitioning OUT of this state.
        /// Use for: cleanup, stopping particles/sounds, resetting hitboxes.
        /// </summary>
        void OnExit(T owner, StateMachine<T> sm);
    }

    /// <summary>
    /// Generic finite state machine. Owns a set of named states and manages
    /// transitions between them. Only one state is active at a time.
    /// </summary>
    public class StateMachine<T>
    {
        // ── Owner ──
        public T Owner { get; }

        // ── State Registry ──
        private readonly Dictionary<string, IState<T>> _states = new();

        // ── Current State ──
        private IState<T> _currentState;
        private string _currentStateName;

        /// <summary>
        /// The name of the currently active state (e.g. "idle", "attack").
        /// </summary>
        public string CurrentStateName => _currentStateName;

        /// <summary>
        /// How long (in seconds) the current state has been active.
        /// Useful for timed states like attack duration or cooldowns.
        /// </summary>
        public float StateTimer { get; private set; }

        public StateMachine(T owner)
        {
            Owner = owner;
        }

        /// <summary>
        /// Register a named state. Call this during initialization.
        /// </summary>
        public void AddState(string name, IState<T> state)
        {
            _states[name] = state;
        }

        /// <summary>
        /// Transition to a new state by name.
        /// Calls OnExit on the old state, then OnEnter on the new one.
        /// Safe to call from within OnUpdate — the transition happens immediately.
        /// </summary>
        public void SetState(string name)
        {
            // Don't transition to the same state (prevents re-entry bugs)
            if (name == _currentStateName) return;

            // Exit the old state
            _currentState?.OnExit(Owner, this);

            // Enter the new state
            _currentStateName = name;
            _currentState = _states[name];
            StateTimer = 0f;
            _currentState.OnEnter(Owner, this);
        }

        /// <summary>
        /// Force-set the initial state without calling OnExit on anything.
        /// Use this only for the very first state.
        /// </summary>
        public void ForceState(string name)
        {
            _currentStateName = name;
            _currentState = _states[name];
            StateTimer = 0f;
            _currentState.OnEnter(Owner, this);
        }

        /// <summary>
        /// Tick the active state. Call this once per frame.
        /// </summary>
        public void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            StateTimer += dt;
            _currentState?.OnUpdate(Owner, this, gameTime);
        }

        /// <summary>
        /// Check if the current state matches a given name.
        /// Handy for external queries like collision checks.
        /// </summary>
        public bool IsInState(string name) => _currentStateName == name;
    }
}
