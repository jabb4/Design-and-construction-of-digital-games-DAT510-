namespace Player.StateMachine
{
    using global::StateMachine.Core;
    using UnityEngine;
    using UnityEngine.InputSystem;
    using System;

    /// <summary>
    /// Handles player input and exposes it via a clean API for the state machine.
    /// Attach this component to the player GameObject.
    /// This class wraps Unity's Input System and provides a consistent interface
    /// for the PlayerStateMachine to query input state.
    /// </summary>
    public class PlayerInputHandler : MonoBehaviour, IIntentSource
    {
        #region Input Properties

        /// <summary>
        /// When true, all input is suppressed and zero/false values are returned.
        /// Use this to freeze the player cleanly (e.g. pause menu) without
        /// disabling the state machine mid-state.
        /// </summary>
        public bool IsBlocked { get; set; }

        /// <summary>
        /// The current movement input from the player.
        /// X = horizontal (A/D or Left/Right), Y = vertical (W/S or Up/Down).
        /// </summary>
        public Vector2 MoveInput => IsBlocked ? Vector2.zero : rawMoveInput;

        /// <summary>
        /// Whether the player is currently holding the sprint button.
        /// </summary>
        public bool IsSprinting => !IsBlocked && rawIsSprinting;

        /// <summary>
        /// Whether the player is currently holding the block button.
        /// </summary>
        public bool IsBlocking => !IsBlocked && rawIsBlocking;

        /// <summary>
        /// True only on the frame when block was pressed.
        /// </summary>
        public bool IsBlockPressed => !IsBlocked && blockIntent.IsPressedThisFrame;

        /// <summary>
        /// True only on the frame when jump was pressed.
        /// This is reset to false in LateUpdate.
        /// </summary>
        public bool IsJumpPressed => !IsBlocked && jumpIntent.IsPressedThisFrame;

        /// <summary>
        /// True only on the frame when attack was pressed.
        /// This is reset to false in LateUpdate.
        /// </summary>
        public bool IsAttackPressed => !IsBlocked && attackIntent.IsPressedThisFrame;

        /// <summary>
        /// True while jump input is buffered.
        /// </summary>
        public bool IsJumpBuffered => !IsBlocked && jumpIntent.IsBuffered;

        /// <summary>
        /// True while attack input is buffered.
        /// </summary>
        public bool IsAttackBuffered => !IsBlocked && attackIntent.IsBuffered;

        /// <summary>
        /// Returns true if the player has significant movement input (above threshold).
        /// </summary>
        public bool HasMovementInput => MoveInput.magnitude > MovementThreshold;

        // Shared intent contract aliases (player + AI compatibility surface).
        public Vector2 MoveIntent => MoveInput;
        public bool HasMoveIntent => HasMovementInput;
        public bool SprintHeld => IsSprinting;
        public bool BlockHeld => IsBlocking;
        public bool BlockPressed => IsBlockPressed;
        public bool JumpPressed => IsJumpPressed;
        public bool JumpBuffered => IsJumpBuffered;
        public bool AttackPressed => IsAttackPressed;
        public bool AttackBuffered => IsAttackBuffered;

        #endregion

        #region Configuration

        /// <summary>
        /// Minimum input magnitude to be considered as movement.
        /// Helps filter out controller stick drift.
        /// </summary>
        [SerializeField]
        [Tooltip("Minimum input magnitude to be considered as movement (filters stick drift)")]
        private float movementThreshold = 0.1f;

        [SerializeField]
        [Tooltip("Time window to buffer jump input (seconds)")]
        private float jumpBufferDuration = 0.2f;

        [SerializeField]
        [Tooltip("Time window to buffer attack input (seconds)")]
        private float attackBufferDuration = 0.2f;

        /// <summary>
        /// Public accessor for the movement threshold.
        /// </summary>
        public float MovementThreshold => movementThreshold;

        #endregion

        #region Events

        /// <summary>
        /// Invoked when the jump button is pressed.
        /// </summary>
        public event Action OnJumpPressed;

        /// <summary>
        /// Invoked when the sprint button is pressed down.
        /// </summary>
        public event Action OnSprintStarted;

        /// <summary>
        /// Invoked when the sprint button is released.
        /// </summary>
        public event Action OnSprintEnded;

        /// <summary>
        /// Invoked when the attack button is pressed.
        /// </summary>
        public event Action OnAttackPressed;

        #endregion

        #region Input System Callbacks

        /// <summary>
        /// Called by Unity Input System when movement input changes.
        /// Receives WASD/Arrow keys or gamepad stick input.
        /// </summary>
        /// <param name="value">The input value from the Input System</param>
        private void OnMove(InputValue value)
        {
            rawMoveInput = value.Get<Vector2>();
        }

        /// <summary>
        /// Called by Unity Input System when sprint input changes.
        /// Handles both button press and release.
        /// </summary>
        /// <param name="value">The input value from the Input System</param>
        private void OnSprint(InputValue value)
        {
            bool wasSprintingBefore = rawIsSprinting;
            rawIsSprinting = value.isPressed;

            // Fire events based on state change
            if (IsSprinting && !wasSprintingBefore)
            {
                OnSprintStarted?.Invoke();
            }
            else if (!IsSprinting && wasSprintingBefore)
            {
                OnSprintEnded?.Invoke();
            }
        }

        /// <summary>
        /// Called by Unity Input System when jump input is received.
        /// Sets a one-frame flag that gets reset in LateUpdate.
        /// </summary>
        /// <param name="value">The input value from the Input System</param>
        private void OnJump(InputValue value)
        {
            if (value.isPressed)
            {
                jumpIntent.RecordPress(jumpBufferDuration);
                OnJumpPressed?.Invoke();
            }
        }

        /// <summary>
        /// Called by Unity Input System when block input changes.
        /// </summary>
        /// <param name="value">The input value from the Input System</param>
        private void OnBlock(InputValue value)
        {
            bool wasBlocking = rawIsBlocking;
            rawIsBlocking = value.isPressed;
            if (rawIsBlocking && !wasBlocking)
            {
                blockIntent.RecordPress();
            }
        }

        /// <summary>
        /// Called by Unity Input System when attack input is received.
        /// Sets a one-frame flag that gets reset in LateUpdate.
        /// </summary>
        /// <param name="value">The input value from the Input System</param>
        private void OnAttack(InputValue value)
        {
            if (value.isPressed)
            {
                attackIntent.RecordPress(attackBufferDuration);
                OnAttackPressed?.Invoke();
            }
        }

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// Called after all Update methods.
        /// Resets one-frame input flags like IsJumpPressed.
        /// </summary>
        private void LateUpdate()
        {
            // Reset one-frame flags
            jumpIntent.ResetFrameState();
            blockIntent.ResetFrameState();
            attackIntent.ResetFrameState();

            jumpIntent.Tick(Time.deltaTime);
            attackIntent.Tick(Time.deltaTime);
        }

        #endregion

        #region Private Fields

        private Vector2 rawMoveInput;
        private bool rawIsSprinting;
        private bool rawIsBlocking;

        private readonly IntentBuffer jumpIntent = new IntentBuffer();
        private readonly IntentBuffer blockIntent = new IntentBuffer();
        private readonly IntentBuffer attackIntent = new IntentBuffer();

        #endregion

        #region Public API

        /// <summary>
        /// Gets the normalized movement direction (0-1 range).
        /// Returns Vector2.zero if input is below threshold.
        /// </summary>
        /// <returns>Normalized movement direction or zero</returns>
        public Vector2 GetNormalizedMovement()
        {
            if (!HasMovementInput)
            {
                return Vector2.zero;
            }

            return MoveInput.normalized;
        }

        /// <summary>
        /// Gets the raw movement magnitude (useful for analog stick input).
        /// Returns 0 if input is below threshold.
        /// </summary>
        /// <returns>Movement magnitude or zero</returns>
        public float GetMovementMagnitude()
        {
            if (!HasMovementInput)
            {
                return 0f;
            }

            return MoveInput.magnitude;
        }

        public bool ConsumeJumpBuffer()
        {
            return jumpIntent.ConsumeBuffered();
        }

        public bool ConsumeAttackBuffer()
        {
            return attackIntent.ConsumeBuffered();
        }

        #endregion
    }
}
