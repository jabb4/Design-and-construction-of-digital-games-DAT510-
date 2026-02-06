namespace Player.StateMachine
{
    /// <summary>
    /// Interface for all player states in the hierarchical state machine.
    /// Based on the Combat Specification Document's HFSM pattern.
    /// 
    /// This interface defines the core contract that all states must implement,
    /// providing lifecycle methods for state entry/exit, update loops, and transition logic.
    /// 
    /// States implementing this interface form the nodes of the Hierarchical Finite State Machine (HFSM),
    /// enabling modular and maintainable player behavior through composition and inheritance.
    /// </summary>
    public interface IState
    {
        /// <summary>
        /// Called when entering this state.
        /// 
        /// Use this method to:
        /// - Initialize state-specific variables
        /// - Start animations
        /// - Register event listeners
        /// - Set up state-specific physics properties
        /// - Play audio cues
        /// 
        /// This is called once per state transition, before the first OnUpdate call.
        /// </summary>
        void OnEnter();
        
        /// <summary>
        /// Called every frame while this state is active.
        /// 
        /// Use this method to:
        /// - Handle input processing
        /// - Update animations based on current state
        /// - Perform visual updates
        /// - Execute frame-dependent logic
        /// 
        /// Called after CheckTransitions() each frame.
        /// Execution order: CheckTransitions() -> OnUpdate() -> (OnFixedUpdate if physics frame)
        /// </summary>
        void OnUpdate();
        
        /// <summary>
        /// Called every fixed update while this state is active.
        /// 
        /// Use this method to:
        /// - Apply physics forces
        /// - Handle Rigidbody movements
        /// - Perform collision-based logic
        /// - Execute physics-dependent calculations
        /// 
        /// This is called at a fixed time interval (typically 0.02s/50Hz) independent of framerate.
        /// Always use this for physics operations to ensure consistent behavior across different framerates.
        /// </summary>
        void OnFixedUpdate();
        
        /// <summary>
        /// Called when exiting this state.
        /// 
        /// Use this method to:
        /// - Clean up state-specific resources
        /// - Unregister event listeners
        /// - Stop animations or particle effects
        /// - Reset temporary state variables
        /// - Ensure clean transition to next state
        /// 
        /// This is called once when transitioning to a different state, after the last OnUpdate call.
        /// Always called before the next state's OnEnter().
        /// </summary>
        void OnExit();
        
        /// <summary>
        /// Checks conditions to determine if a state transition should occur.
        /// Called each tick before OnUpdate.
        /// 
        /// Use this method to:
        /// - Evaluate transition conditions (input, timers, health, etc.)
        /// - Determine the next state based on current game state
        /// - Implement state machine logic
        /// 
        /// Execution flow:
        /// 1. CheckTransitions() is called
        /// 2. If it returns a non-null state, transition occurs:
        ///    - Current state's OnExit() is called
        ///    - New state's OnEnter() is called
        /// 3. If it returns null, the state remains active and OnUpdate() proceeds
        /// 
        /// Important notes:
        /// - Return null to stay in the current state
        /// - Return a different IState instance to trigger a transition
        /// - Transitions are evaluated every frame, so keep logic efficient
        /// - Avoid creating new state instances here; reference existing state objects
        /// </summary>
        /// <returns>The next state to transition to, or null to stay in current state.</returns>
        IState CheckTransitions();
        
        /// <summary>
        /// The name of this state for debugging.
        /// 
        /// Used for:
        /// - Debug logging and visualization
        /// - State tracking in development builds
        /// - Performance profiling
        /// - Editor inspector display
        /// 
        /// Should return a clear, descriptive name (e.g., "Idle", "Running", "Attacking", "AirDash").
        /// </summary>
        string StateName { get; }
    }
}
