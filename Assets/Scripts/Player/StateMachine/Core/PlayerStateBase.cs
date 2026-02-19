namespace Player.StateMachine
{
    using global::StateMachine.Core;
    using UnityEngine;

    /// <summary>
    /// Abstract base class for all player states in the state machine.
    /// Provides common functionality, animation helpers, and references to core components.
    /// This is the "Move" base class from the Combat Specification Document.
    /// </summary>
    public abstract class PlayerStateBase : IState
    {
        #region Core References

        /// <summary>
        /// Reference to the state machine that owns this state.
        /// </summary>
        protected PlayerStateMachine Owner { get; private set; }

        /// <summary>
        /// Shared combat runtime context for this state instance.
        /// </summary>
        protected PlayerCombatStateContext Context { get; private set; }

        /// <summary>
        /// Shared actor context view used by reusable state-machine logic.
        /// </summary>
        protected IActorStateContext ActorContext => Context;

        /// <summary>
        /// Intent surface (player input or AI planner) for state logic.
        /// </summary>
        protected IIntentSource Intent => Context?.IntentSource;

        /// <summary>
        /// Reference to the Animator component for animation control.
        /// </summary>
        protected Animator Animator { get; private set; }

        /// <summary>
        /// Reference to the PlayerInputHandler for reading player input.
        /// </summary>
        protected PlayerInputHandler Input { get; private set; }

        /// <summary>
        /// Reference to the CharacterMotor for physics and movement control.
        /// </summary>
        protected CharacterMotor Motor { get; private set; }

        #endregion

        #region Cached Animator Parameter Hashes

        /// <summary>
        /// Cached hash for the "VelocityX" animator parameter (horizontal velocity).
        /// </summary>
        protected static readonly int VelocityXHash = Animator.StringToHash("VelocityX");

        /// <summary>
        /// Cached hash for the "VelocityZ" animator parameter (forward velocity).
        /// </summary>
        protected static readonly int VelocityZHash = Animator.StringToHash("VelocityZ");

        /// <summary>
        /// Cached hash for the "Speed" animator parameter (overall speed magnitude).
        /// </summary>
        protected static readonly int SpeedHash = Animator.StringToHash("Speed");

        /// <summary>
        /// Cached hash for the "IsGrounded" animator parameter.
        /// </summary>
        protected static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");

        /// <summary>
        /// Cached hash for the "IsSprinting" animator parameter.
        /// </summary>
        protected static readonly int IsSprintingHash = Animator.StringToHash("IsSprinting");

        /// <summary>
        /// Cached hash for the "IsMoving" animator parameter.
        /// </summary>
        protected static readonly int IsMovingHash = Animator.StringToHash("IsMoving");

        /// <summary>
        /// Cached hash for the "IsEquipped" animator parameter (weapon equipped state).
        /// </summary>
        protected static readonly int IsEquippedHash = Animator.StringToHash("IsEquipped");

        /// <summary>
        /// Cached hash for the "IsLockedOn" animator parameter (target lock state).
        /// </summary>
        protected static readonly int IsLockedOnHash = Animator.StringToHash("IsLockedOn");

        /// <summary>
        /// Cached hash for the "Jump" animator trigger.
        /// </summary>
        protected static readonly int JumpHash = Animator.StringToHash("Jump");

        /// <summary>
        /// Cached hash for the "Land" animator trigger.
        /// </summary>
        protected static readonly int LandHash = Animator.StringToHash("Land");

        #endregion

        #region Properties

        /// <summary>
        /// Gets the name of this state for debugging purposes.
        /// Defaults to the class name but can be overridden.
        /// </summary>
        public virtual string StateName => GetType().Name;

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the state with references to core components.
        /// Called by the state machine when the state is created.
        /// </summary>
        /// <param name="stateMachine">The state machine that owns this state.</param>
        /// <param name="context">Shared combat runtime context.</param>
        public void Initialize(PlayerStateMachine stateMachine, PlayerCombatStateContext context)
        {
            Owner = stateMachine;
            Context = context;
            Animator = context?.Animator;
            Input = context?.Input;
            Motor = context?.Motor;
        }

        #endregion

        #region IState Implementation

        /// <summary>
        /// Called when this state is entered.
        /// Override to implement state-specific entry behavior.
        /// </summary>
        public virtual void OnEnter() { }

        /// <summary>
        /// Called every frame while this state is active.
        /// Override to implement state-specific update logic.
        /// </summary>
        public virtual void OnUpdate() { }

        /// <summary>
        /// Called every fixed timestep while this state is active.
        /// Override to implement state-specific physics logic.
        /// </summary>
        public virtual void OnFixedUpdate() { }

        /// <summary>
        /// Called when this state is exited.
        /// Override to implement state-specific cleanup behavior.
        /// </summary>
        public virtual void OnExit() { }

        /// <summary>
        /// Checks for state transitions and returns the next state if a transition should occur.
        /// Must be implemented by concrete state classes.
        /// </summary>
        /// <returns>The transition decision for this frame.</returns>
        public abstract TransitionDecision EvaluateTransition();

        #endregion

        #region Animation Helpers

        /// <summary>
        /// Gets the weapon-appropriate animation name by replacing the {W} placeholder.
        /// </summary>
        /// <param name="pattern">Animation name pattern with {W} placeholder (e.g., "Stand_{W}_Idle").</param>
        /// <returns>Animation name with "Equip" or "Unequip" based on weapon state.</returns>
        /// <example>
        /// GetWeaponVariant("Stand_{W}_Idle") returns "Stand_Equip_Idle" if weapon is equipped,
        /// or "Stand_Unequip_Idle" if weapon is not equipped.
        /// </example>
        protected string GetWeaponVariant(string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
            {
                Debug.LogWarning($"[{StateName}] GetWeaponVariant called with null or empty pattern.");
                return pattern;
            }

            if (Animator == null)
            {
                Debug.LogWarning($"[{StateName}] Animator is null in GetWeaponVariant.");
                return pattern;
            }

            bool isEquipped = Animator.GetBool(IsEquippedHash);
            string weaponState = isEquipped ? "Equip" : "Unequip";
            return pattern.Replace("{W}", weaponState);
        }

        /// <summary>
        /// Checks if the animator is currently in a specific state.
        /// </summary>
        /// <param name="stateName">The name of the animator state to check.</param>
        /// <param name="layer">The animator layer to check (default is 0).</param>
        /// <returns>True if the animator is in the specified state, false otherwise.</returns>
        protected bool IsAnimatorInState(string stateName, int layer = 0)
        {
            if (Animator == null)
            {
                Debug.LogWarning($"[{StateName}] Animator is null in IsAnimatorInState.");
                return false;
            }

            if (string.IsNullOrEmpty(stateName))
            {
                Debug.LogWarning($"[{StateName}] IsAnimatorInState called with null or empty state name.");
                return false;
            }

            AnimatorStateInfo stateInfo = Animator.GetCurrentAnimatorStateInfo(layer);
            return stateInfo.IsName(stateName);
        }

        /// <summary>
        /// Gets the normalized time (0-1) of the current animator state.
        /// </summary>
        /// <param name="layer">The animator layer to check (default is 0).</param>
        /// <returns>Normalized time of the current state, or 0 if animator is null.</returns>
        protected float GetAnimatorNormalizedTime(int layer = 0)
        {
            if (Animator == null)
            {
                Debug.LogWarning($"[{StateName}] Animator is null in GetAnimatorNormalizedTime.");
                return 0f;
            }

            AnimatorStateInfo stateInfo = Animator.GetCurrentAnimatorStateInfo(layer);
            return stateInfo.normalizedTime;
        }

        /// <summary>
        /// Checks if the current animation has completed based on normalized time.
        /// </summary>
        /// <param name="threshold">The normalized time threshold to consider complete (default is 0.95).</param>
        /// <param name="layer">The animator layer to check (default is 0).</param>
        /// <returns>True if the animation has reached or exceeded the threshold, false otherwise.</returns>
        protected bool IsAnimationComplete(float threshold = 0.95f, int layer = 0)
        {
            if (Animator == null)
            {
                Debug.LogWarning($"[{StateName}] Animator is null in IsAnimationComplete.");
                return false;
            }

            AnimatorStateInfo stateInfo = Animator.GetCurrentAnimatorStateInfo(layer);
            return stateInfo.normalizedTime >= threshold;
        }

        private static readonly System.Collections.Generic.HashSet<int> _loggedMissingStates = new();

        protected bool CrossFade(string stateName, float duration = 0.1f, int layer = 0)
        {
            if (Animator == null || Animator.runtimeAnimatorController == null)
            {
                Debug.LogWarning($"[{StateName}] Animator is null in CrossFade.");
                return false;
            }

            if (string.IsNullOrEmpty(stateName))
            {
                Debug.LogWarning($"[{StateName}] CrossFade called with null or empty state name.");
                return false;
            }

            if (layer < 0) layer = 0;
            if (layer >= Animator.layerCount) layer = 0;

            if (TryCrossFadeWithSubStatePaths(stateName, duration, layer))
            {
                return true;
            }

            int stateHash = Animator.StringToHash(stateName);
            if (Animator.HasState(layer, stateHash))
            {
                Animator.CrossFadeInFixedTime(stateHash, duration, layer);
                return true;
            }

            string layerName = Animator.GetLayerName(layer);
            string fullPath = $"{layerName}.{stateName}";
            int fullPathHash = Animator.StringToHash(fullPath);
            if (Animator.HasState(layer, fullPathHash))
            {
                Animator.CrossFadeInFixedTime(fullPathHash, duration, layer);
                return true;
            }

            int logKey = (Animator.GetInstanceID() * 397) ^ stateName.GetHashCode();
            if (!_loggedMissingStates.Contains(logKey))
            {
                _loggedMissingStates.Add(logKey);
                Debug.LogWarning($"[{StateName}] State '{stateName}' not found in animator. Run Tools → Setup Player Animator Controller.", Animator);
            }
            return false;
        }

        private bool TryCrossFadeWithSubStatePaths(string stateName, float duration, int layer)
        {
            string layerName = Animator.GetLayerName(layer);
            bool isEquipped = Animator.GetBool(IsEquippedHash);

            string[] preferredPaths = isEquipped
                ? new[]
                {
                    $"{layerName}.Grounded.Equip Locomotion.{stateName}",
                    $"{layerName}.Airborne.Equip Jump.{stateName}",
                    $"{layerName}.Grounded.Unequip Locomotion.{stateName}",
                    $"{layerName}.Airborne.Unequip Jump.{stateName}",
                }
                : new[]
                {
                    $"{layerName}.Grounded.Unequip Locomotion.{stateName}",
                    $"{layerName}.Airborne.Unequip Jump.{stateName}",
                    $"{layerName}.Grounded.Equip Locomotion.{stateName}",
                    $"{layerName}.Airborne.Equip Jump.{stateName}",
                };

            foreach (string path in preferredPaths)
            {
                int pathHash = Animator.StringToHash(path);
                if (Animator.HasState(layer, pathHash))
                {
                    Animator.CrossFadeInFixedTime(pathHash, duration, layer);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Updates common locomotion animator parameters.
        /// Convenience method to set multiple animator parameters at once.
        /// </summary>
        /// <param name="velocity">The local velocity (X and Z components).</param>
        /// <param name="speed">The overall speed magnitude.</param>
        /// <param name="isGrounded">Whether the character is grounded.</param>
        /// <param name="isSprinting">Whether the character is sprinting.</param>
        protected void UpdateAnimatorLocomotion(Vector2 velocity, float speed, bool isGrounded, bool isSprinting)
        {
            if (Animator == null)
            {
                Debug.LogWarning($"[{StateName}] Animator is null in UpdateAnimatorLocomotion.");
                return;
            }

            Animator.SetFloat(VelocityXHash, velocity.x);
            Animator.SetFloat(VelocityZHash, velocity.y);
            Animator.SetFloat(SpeedHash, speed);
            Animator.SetBool(IsGroundedHash, isGrounded);
            Animator.SetBool(IsSprintingHash, isSprinting);
            Animator.SetBool(IsMovingHash, speed > 0.01f);
        }

        /// <summary>
        /// Updates blend-tree parameters with smoothed velocity.
        /// </summary>
        /// <param name="currentVelocity">Current smoothed velocity (x = strafe, y = forward)</param>
        /// <param name="velocityRef">Reference velocity for SmoothDamp</param>
        /// <param name="smoothTime">Smoothing time</param>
        /// <param name="lockOn">Whether lock-on movement should use raw input</param>
        /// <returns>Updated smoothed velocity</returns>
        protected Vector2 UpdateBlendTreeParameters(Vector2 currentVelocity, ref Vector2 velocityRef, float smoothTime, bool lockOn)
        {
            Vector2 targetVelocity = lockOn
                ? Input.MoveInput
                : new Vector2(0f, Input.MoveInput.magnitude);

            Vector2 smoothed = Vector2.SmoothDamp(currentVelocity, targetVelocity, ref velocityRef, smoothTime);

            Animator.SetFloat(VelocityXHash, smoothed.x);
            Animator.SetFloat(VelocityZHash, smoothed.y);
            Animator.SetFloat(SpeedHash, smoothed.magnitude);

            return smoothed;
        }

        /// <summary>
        /// Rotate towards lock-on target or movement direction based on input and lock state.
        /// </summary>
        /// <param name="requireMovementInput">If true, only rotate towards movement when input exists.</param>
        protected void RotateWithContext(bool requireMovementInput = false)
        {
            if (Motor.IsLockedOn)
            {
                Motor.RotateTowardsLockOnTarget();
                return;
            }

            if (requireMovementInput && !Input.HasMovementInput)
            {
                return;
            }

            Motor.RotateTowardsMovement(Input.MoveInput);
        }

        #endregion
    }
}
