namespace Player.StateMachine
{
    using UnityEngine;
    using System;
    using System.Collections.Generic;
    using Player.StateMachine.States;

    /// <summary>
    /// Main state machine controller for the player character.
    /// Manages state transitions, weapon state, and coordinates all player behavior.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(PlayerInputHandler))]
    [RequireComponent(typeof(CharacterMotor))]
    public class PlayerStateMachine : MonoBehaviour
    {
        [Header("Initial State")]
        [SerializeField] private bool startEquipped = false;

        [Header("Weapon Settings")]
        [SerializeField] private float unequipDelay = 3f;


        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;

        // Weapon state
        /// <summary>
        /// Is the weapon currently equipped?
        /// </summary>
        public bool IsEquipped { get; private set; }

        /// <summary>
        /// Is the weapon currently transitioning between equipped/unequipped states?
        /// </summary>
        public bool IsTransitioningWeapon { get; private set; }

        // Current state
        /// <summary>
        /// The currently active state.
        /// </summary>
        public IState CurrentState { get; private set; }

        /// <summary>
        /// The name of the current state for debugging.
        /// </summary>
        public string CurrentStateName => CurrentState?.StateName ?? "None";


        // Events
        /// <summary>
        /// Fired when the state changes. Parameters: (oldState, newState)
        /// </summary>
        public event Action<IState, IState> OnStateChanged;

        /// <summary>
        /// Fired when the weapon state changes. Parameter: isEquipped
        /// </summary>
        public event Action<bool> OnWeaponStateChanged;

        // Components
        /// <summary>
        /// Reference to the Animator component.
        /// </summary>
        public Animator Animator { get; private set; }

        /// <summary>
        /// Reference to the PlayerInputHandler component.
        /// </summary>
        public PlayerInputHandler Input { get; private set; }

        /// <summary>
        /// Reference to the CharacterMotor component.
        /// </summary>
        public CharacterMotor Motor { get; private set; }

        /// <summary>
        /// Reference to the CameraController (found at runtime).
        /// </summary>
        public CameraController CameraController { get; private set; }

        // Internal state management
        private float unequipTimer = -1f;
        private bool hasPendingUnequipRequest = false;
        private bool requestedEquipWhilePending = false;
        private bool pendingEquipRequest = false;
        private bool pendingUnequipRequest = false;
        private WeaponTransitionType currentWeaponTransition = WeaponTransitionType.None;
        private float weaponTransitionStartTime = -1f;

        [SerializeField]
        [Tooltip("Optional fallback timeout (seconds) if transition animation never completes. Set to 0 to disable.")]
        private float weaponTransitionTimeout = 0f;


        private static readonly int EquipToUnequip01Hash = Animator.StringToHash("Equip To Unequip 01");
        private static readonly int EquipToUnequip02Hash = Animator.StringToHash("Equip To Unequip 02");
        private static readonly int EquipToUnequip03Hash = Animator.StringToHash("Equip To Unequip 03");
        private static readonly int EquipToUnequip04Hash = Animator.StringToHash("Equip To Unequip 04");
        private static readonly int EquipToUnequip05Hash = Animator.StringToHash("Equip To Unequip 05");
        private static readonly int UnequipToEquipQuickHash = Animator.StringToHash("Unequip To Equip Quick");
        private static readonly int VelocityXHash = Animator.StringToHash("VelocityX");
        private static readonly int VelocityZHash = Animator.StringToHash("VelocityZ");
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
        private static readonly int IsSprintingHash = Animator.StringToHash("IsSprinting");
        private static readonly int IsTransitioningWeaponHash = Animator.StringToHash("IsTransitioningWeapon");
        private static readonly int UnequipVariantHash = Animator.StringToHash("UnequipVariant");

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeComponents();
        }

        private void Start()
        {
            IsEquipped = startEquipped;
            UpdateAnimatorParameters();
            ChangeState<IdleState>();
        }

        private void Update()
        {
            // Update weapon state timer
            UpdateWeaponState();

            if (IsTransitioningWeapon)
            {
                UpdateAnimatorParameters();
                return;
            }

            // Check for state transitions
            IState nextState = CurrentState?.CheckTransitions();
            if (nextState != null && nextState != CurrentState)
            {
                ChangeState(nextState);
            }

            // Update current state
            CurrentState?.OnUpdate();

            // Update animator parameters every frame
            UpdateAnimatorParameters();
        }

        private void FixedUpdate()
        {
            if (IsTransitioningWeapon)
            {
                Motor.Move(Vector2.zero, useSprint: false);
                return;
            }

            // Fixed update for physics-based state logic
            CurrentState?.OnFixedUpdate();
        }

        private void OnDestroy()
        {
        }

        #endregion

        #region State Management

        /// <summary>
        /// Change to a new state. Call from state's CheckTransitions or externally.
        /// </summary>
        public void ChangeState<T>() where T : PlayerStateBase, new()
        {
            T newState = GetState<T>();
            ChangeState(newState);
        }

        /// <summary>
        /// Change to a specific state instance.
        /// </summary>
        public void ChangeState(IState newState)
        {
            if (newState == null)
            {
                Debug.LogError("[PlayerStateMachine] Attempted to change to null state!");
                return;
            }

            // Don't change if already in this state
            if (CurrentState == newState)
            {
                return;
            }

            IState oldState = CurrentState;

            // Exit current state
            CurrentState?.OnExit();

            // Change state
            CurrentState = newState;

            // Enter new state
            CurrentState?.OnEnter();

            // Fire event
            OnStateChanged?.Invoke(oldState, newState);

            if (showDebugInfo)
            {
                Debug.Log($"[PlayerStateMachine] State changed: {oldState?.StateName ?? "None"} -> {CurrentState.StateName}");
            }
        }

        /// <summary>
        /// Create a new state instance of the specified type.
        /// </summary>
        public T GetState<T>() where T : PlayerStateBase, new()
        {
            T newState = new T();
            newState.Initialize(this, Animator, Input, Motor);
            return newState;
        }

        #endregion

        #region Weapon State Management

        /// <summary>
        /// Request weapon equip. Plays equip animation.
        /// </summary>
        public void RequestEquip()
        {
            // Cancel any pending unequip
            CancelUnequipRequest();

            // Already equipped or transitioning?
            if (IsEquipped || IsTransitioningWeapon)
            {
                if (IsTransitioningWeapon)
                {
                    pendingEquipRequest = true;
                }
                return;
            }

            BeginEquipTransition();
        }

        /// <summary>
        /// Request weapon unequip. Plays unequip animation after delay.
        /// </summary>
        public void RequestUnequip()
        {
            // Already unequipped or transitioning?
            if (!IsEquipped || IsTransitioningWeapon)
            {
                if (IsTransitioningWeapon)
                {
                    pendingUnequipRequest = true;
                }
                return;
            }

            // Start the unequip timer
            hasPendingUnequipRequest = true;
            unequipTimer = unequipDelay;
            requestedEquipWhilePending = false;

            if (showDebugInfo)
            {
                Debug.Log($"[PlayerStateMachine] Unequip requested. Will unequip in {unequipDelay}s");
            }
        }

        /// <summary>
        /// Cancel pending unequip request.
        /// </summary>
        public void CancelUnequipRequest()
        {
            if (hasPendingUnequipRequest)
            {
                hasPendingUnequipRequest = false;
                unequipTimer = -1f;
                requestedEquipWhilePending = false;

                if (showDebugInfo)
                {
                    Debug.Log("[PlayerStateMachine] Unequip request cancelled");
                }
            }
        }

        public void NotifyEquipAnimationComplete()
        {
            if (!IsTransitioningWeapon)
            {
                return;
            }

            IsEquipped = true;
            Animator?.SetBool("IsEquipped", true);
            OnWeaponStateChanged?.Invoke(true);

            IsTransitioningWeapon = false;
            currentWeaponTransition = WeaponTransitionType.None;
            Animator?.SetBool(IsTransitioningWeaponHash, false);

            if (Input != null && Input.HasMovementInput)
            {
                ChangeState(Input.IsSprinting ? GetState<SprintState>() : GetState<WalkingState>());
            }
            else
            {
                ChangeState(GetState<IdleState>());
            }

            if (pendingUnequipRequest)
            {
                pendingUnequipRequest = false;
                RequestUnequip();
            }
        }

        public void NotifyUnequipAnimationComplete()
        {
            if (!IsTransitioningWeapon)
            {
                return;
            }

            IsEquipped = false;
            Animator?.SetBool("IsEquipped", false);
            OnWeaponStateChanged?.Invoke(false);

            IsTransitioningWeapon = false;
            currentWeaponTransition = WeaponTransitionType.None;
            Animator?.SetBool(IsTransitioningWeaponHash, false);

            if (Input != null && Input.HasMovementInput)
            {
                ChangeState(Input.IsSprinting ? GetState<SprintState>() : GetState<WalkingState>());
            }
            else
            {
                ChangeState(GetState<IdleState>());
            }

            if (pendingEquipRequest)
            {
                pendingEquipRequest = false;
                RequestEquip();
            }
        }

        private void UpdateWeaponState()
        {
            if (CameraController != null)
            {
                bool isLockedOn = CameraController.IsLockedOn;

                if (isLockedOn && !IsEquipped && !IsTransitioningWeapon)
                {
                    RequestEquip();
                }
                else if (!isLockedOn && IsEquipped && !hasPendingUnequipRequest)
                {
                    RequestUnequip();
                }

                if (hasPendingUnequipRequest)
                {
                    if (isLockedOn)
                    {
                        requestedEquipWhilePending = true;
                        unequipTimer = unequipDelay;
                    }
                    else
                    {
                        requestedEquipWhilePending = false;
                    }
                }
            }

            if (hasPendingUnequipRequest && unequipTimer > 0f &&
                (CameraController == null || !CameraController.IsLockedOn))
            {
                unequipTimer -= Time.deltaTime;

                if (unequipTimer <= 0f)
                {
                    if (!requestedEquipWhilePending && (CameraController == null || !CameraController.IsLockedOn))
                    {
                        BeginUnequipTransition();
                    }
                    hasPendingUnequipRequest = false;
                    requestedEquipWhilePending = false;
                }
            }

            CheckWeaponTransitionCompletion();
        }

        private void BeginEquipTransition()
        {
            if (showDebugInfo)
            {
                Debug.Log("[PlayerStateMachine] Equipping weapon (transition)");
            }

            IsTransitioningWeapon = true;
            currentWeaponTransition = WeaponTransitionType.Equipping;
            weaponTransitionStartTime = Time.time;
            Animator?.SetTrigger("Equip");
            Animator?.SetBool(IsTransitioningWeaponHash, true);
        }

        private void BeginUnequipTransition()
        {
            if (showDebugInfo)
            {
                Debug.Log("[PlayerStateMachine] Unequipping weapon (transition)");
            }

            IsTransitioningWeapon = true;
            currentWeaponTransition = WeaponTransitionType.Unequipping;
            weaponTransitionStartTime = Time.time;
            SetUnequipVariant();
            Animator?.SetTrigger("Unequip");
            Animator?.SetBool(IsTransitioningWeaponHash, true);
        }

        private void SetUnequipVariant()
        {
            if (Animator == null)
            {
                return;
            }

            int variant = UnityEngine.Random.Range(0, 5);
            Animator.SetInteger(UnequipVariantHash, variant);
        }

        private void CheckWeaponTransitionCompletion()
        {
            if (!IsTransitioningWeapon || Animator == null)
            {
                return;
            }

            AnimatorStateInfo stateInfo = Animator.GetCurrentAnimatorStateInfo(0);
            int shortHash = stateInfo.shortNameHash;

            bool isEquipTransitionState = shortHash == UnequipToEquipQuickHash;
            bool isUnequipTransitionState = shortHash == EquipToUnequip01Hash || shortHash == EquipToUnequip02Hash ||
                                            shortHash == EquipToUnequip03Hash || shortHash == EquipToUnequip04Hash ||
                                            shortHash == EquipToUnequip05Hash;

            if (currentWeaponTransition == WeaponTransitionType.Equipping && isEquipTransitionState &&
                stateInfo.normalizedTime >= 0.95f)
            {
                NotifyEquipAnimationComplete();
                return;
            }

            if (currentWeaponTransition == WeaponTransitionType.Unequipping && isUnequipTransitionState &&
                stateInfo.normalizedTime >= 0.95f)
            {
                NotifyUnequipAnimationComplete();
                return;
            }

            if (weaponTransitionTimeout > 0f && weaponTransitionStartTime > 0f &&
                Time.time - weaponTransitionStartTime >= weaponTransitionTimeout)
            {
                if (currentWeaponTransition == WeaponTransitionType.Equipping)
                {
                    NotifyEquipAnimationComplete();
                }
                else if (currentWeaponTransition == WeaponTransitionType.Unequipping)
                {
                    NotifyUnequipAnimationComplete();
                }
            }
        }

        private enum WeaponTransitionType
        {
            None,
            Equipping,
            Unequipping
        }

        #endregion

        #region Component Initialization

        private void InitializeComponents()
        {
            // Get required components
            Animator = GetComponent<Animator>();
            if (Animator == null)
            {
                Debug.LogError("[PlayerStateMachine] Animator component not found!");
            }

            Input = GetComponent<PlayerInputHandler>();
            if (Input == null)
            {
                Debug.LogError("[PlayerStateMachine] PlayerInputHandler component not found!");
            }

            Motor = GetComponent<CharacterMotor>();
            if (Motor == null)
            {
                Debug.LogError("[PlayerStateMachine] CharacterMotor component not found!");
            }

            // Find CameraController in scene
            CameraController = FindAnyObjectByType<CameraController>();
            if (CameraController == null)
            {
                Debug.LogWarning("[PlayerStateMachine] CameraController not found in scene. Lock-on weapon behavior will not work.");
            }
        }

        #endregion

        #region Animator Updates

        private void UpdateAnimatorParameters()
        {
            if (Animator == null)
            {
                return;
            }

            Animator.SetBool("IsEquipped", IsEquipped);

            Animator.SetBool(IsTransitioningWeaponHash, IsTransitioningWeapon);

            bool isLockedOn = CameraController != null && CameraController.IsLockedOn;
            Animator.SetBool("IsLockedOn", isLockedOn);

            if (IsTransitioningWeapon)
            {
                Animator.SetBool(IsMovingHash, false);
                Animator.SetBool(IsSprintingHash, false);
                Animator.SetFloat(VelocityXHash, 0f);
                Animator.SetFloat(VelocityZHash, 0f);
                Animator.SetFloat(SpeedHash, 0f);
            }
        }

        #endregion

        #region Debug

        private void OnGUI()
        {
            if (!showDebugInfo)
            {
                return;
            }

            // Create debug display in top-left corner
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.BeginVertical("box");

            GUILayout.Label("<b>Player State Machine</b>");
            GUILayout.Space(5);

            GUILayout.Label($"State: <b>{CurrentStateName}</b>");
            GUILayout.Label($"Equipped: <b>{IsEquipped}</b>");
            GUILayout.Label($"Transitioning: <b>{IsTransitioningWeapon}</b>");

            bool isLockedOn = CameraController != null && CameraController.IsLockedOn;

            if (hasPendingUnequipRequest)
            {
                if (isLockedOn || requestedEquipWhilePending)
                {
                    GUILayout.Label("Unequip: <b>paused</b>");
                }
                else
                {
                    GUILayout.Label($"Unequip in: <b>{unequipTimer:F1}s</b>");
                }
            }
            GUILayout.Label($"Locked On: <b>{isLockedOn}</b>");

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        #endregion
    }
}
