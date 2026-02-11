namespace Player.StateMachine
{
    using UnityEngine;
    using System;
    using Player.StateMachine.States;

    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(PlayerInputHandler))]
    [RequireComponent(typeof(CharacterMotor))]
    public partial class PlayerStateMachine : MonoBehaviour
    {
        [Header("Attack Movement")]
        [SerializeField] private float attackForwardDistance = 0.50f;
        [SerializeField, Min(0.01f)] private float attackPushDuration = 0.12f;
        [SerializeField, Range(0.05f, 0.95f)] private float attackPushEndSpeedFraction = 0.2f;

        public IState CurrentState { get; private set; }
        public string CurrentStateName => CurrentState?.StateName ?? "None";

        public event Action<IState, IState> OnStateChanged;
        public event Action<bool> OnWeaponStateChanged;
        public event Action<AttackStep> OnAttackStepChanged;

        public Animator Animator { get; private set; }
        public PlayerInputHandler Input { get; private set; }
        public CharacterMotor Motor { get; private set; }
        public CameraController CameraController { get; private set; }
        public AttackStep? CurrentAttackStep { get; private set; }

        public float AttackForwardDistance => attackForwardDistance;
        public float AttackPushDuration => attackPushDuration;
        public float AttackPushEndSpeedFraction => attackPushEndSpeedFraction;

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
            UpdateTimeScale();

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

            UpdateAnimatorParameters();
        }

        private void FixedUpdate()
        {
            if (IsTransitioningWeapon)
            {
                Motor.Move(Vector2.zero, useSprint: false);
                return;
            }

            CurrentState?.OnFixedUpdate();
        }

        public void ChangeState<T>() where T : PlayerStateBase, new()
        {
            T newState = GetState<T>();
            ChangeState(newState);
        }

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

            ClearCurrentAttack();

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

        public T GetState<T>() where T : PlayerStateBase, new()
        {
            T newState = new T();
            newState.Initialize(this, Animator, Input, Motor);
            return newState;
        }

        public void SetCurrentAttack(AttackStep step)
        {
            CurrentAttackStep = step;
            OnAttackStepChanged?.Invoke(step);
        }

        public void ClearCurrentAttack()
        {
            CurrentAttackStep = null;
        }

        public void NotifyAttackPhase(AttackPhase phase)
        {
            if (CurrentState is IAttackPhaseListener listener)
            {
                listener.OnAttackPhase(phase);
            }
        }

        private void InitializeComponents()
        {
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

            CameraController = FindAnyObjectByType<CameraController>();
            if (CameraController == null)
            {
                Debug.LogWarning("[PlayerStateMachine] CameraController not found in scene. Lock-on weapon behavior will not work.");
            }
        }
    }
}
