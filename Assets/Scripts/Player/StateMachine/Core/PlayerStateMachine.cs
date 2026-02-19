namespace Player.StateMachine
{
    using System.Collections.Generic;
    using global::StateMachine.Core;
    using UnityEngine;
    using System;
    using Player.StateMachine.States;

    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(PlayerInputHandler))]
    [RequireComponent(typeof(CharacterMotor))]
    public partial class PlayerStateMachine : MonoBehaviour
    {
        [Header("Combat")]
        [SerializeField] private AttackComboAsset attackCombo;

        public IState CurrentState => runtime?.CurrentState;
        public string CurrentStateName => runtime?.CurrentStateName ?? "None";

        public event Action<IState, IState> OnStateChanged;
        public event Action<bool> OnWeaponStateChanged;
        public event Action<AttackStep> OnAttackStepChanged;

        public Animator Animator { get; private set; }
        public PlayerInputHandler Input { get; private set; }
        public CharacterMotor Motor { get; private set; }
        public CameraController CameraController { get; private set; }
        public PlayerCombatStateContext CombatContext { get; private set; }
        public AttackStep? CurrentAttackStep { get; private set; }
        public AttackComboAsset AttackCombo => attackCombo;
        public int AttackStepCount => attackCombo != null ? attackCombo.Count : 0;

        private readonly List<global::Combat.ICombatAttackFeedbackHook> attackFeedbackHooks = new List<global::Combat.ICombatAttackFeedbackHook>(4);
        private readonly Dictionary<Type, PlayerStateBase> stateCache = new Dictionary<Type, PlayerStateBase>(16);
        private StateMachineRuntime runtime;

        private void Awake()
        {
            InitializeComponents();
            InitializeRuntime();
            ValidateAttackComboConfiguration();
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

            runtime?.Tick();

            UpdateAnimatorParameters();
        }

        private void FixedUpdate()
        {
            if (IsTransitioningWeapon)
            {
                Motor.Move(Vector2.zero, useSprint: false);
                return;
            }

            runtime?.FixedTick();
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

            runtime?.ChangeState(newState);
        }

        public T GetState<T>() where T : PlayerStateBase, new()
        {
            Type stateType = typeof(T);
            if (stateCache.TryGetValue(stateType, out PlayerStateBase cachedState))
            {
                return (T)cachedState;
            }

            T newState = new T();
            newState.Initialize(this, CombatContext);
            stateCache[stateType] = newState;
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
            if (CurrentState is not IAttackPhaseListener listener)
            {
                return;
            }

            if (listener.OnAttackPhase(phase))
            {
                DispatchAttackFeedback(phase);
            }
        }

        public bool TryGetAttackStep(int index, out AttackStep step)
        {
            if (attackCombo == null)
            {
                step = default;
                return false;
            }

            return attackCombo.TryGetStep(index, out step);
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

            CombatContext = new PlayerCombatStateContext(this, Animator, Input, Motor);
        }

        private void InitializeRuntime()
        {
            runtime = new StateMachineRuntime();
            runtime.StateChanging += HandleStateChanging;
            runtime.StateChanged += HandleStateChanged;
        }

        private void HandleStateChanging(IState previous, IState next)
        {
            ClearCurrentAttack();
        }

        private void HandleStateChanged(IState previous, IState current)
        {
            OnStateChanged?.Invoke(previous, current);

            if (showDebugInfo)
            {
                Debug.Log($"[PlayerStateMachine] State changed: {previous?.StateName ?? "None"} -> {current?.StateName ?? "None"}");
            }
        }

        private void ValidateAttackComboConfiguration()
        {
            if (attackCombo != null)
            {
                return;
            }

            Debug.LogWarning("[PlayerStateMachine] No AttackComboAsset assigned. Attack state will return to Idle.", this);
        }

        private void DispatchAttackFeedback(AttackPhase phase)
        {
            attackFeedbackHooks.Clear();
            GetComponents(attackFeedbackHooks);
            if (attackFeedbackHooks.Count == 0)
            {
                return;
            }

            global::Combat.AttackData? attack = null;
            if (CurrentAttackStep.HasValue)
            {
                AttackStep current = CurrentAttackStep.Value;
                attack = global::Player.Combat.AttackDataMapper.ToAttackData(current);
            }

            var context = new global::Combat.CombatAttackFeedbackContext
            {
                Phase = MapAttackPhase(phase),
                Attack = attack,
                Attacker = GetComponent<global::Combat.ICombatant>(),
                AttackDirection = ResolveAttackDirection()
            };

            for (int i = 0; i < attackFeedbackHooks.Count; i++)
            {
                attackFeedbackHooks[i]?.OnCombatAttackPhase(context);
            }
        }

        private static global::Combat.CombatAttackPhase MapAttackPhase(AttackPhase phase)
        {
            switch (phase)
            {
                case AttackPhase.Windup:
                    return global::Combat.CombatAttackPhase.Windup;
                case AttackPhase.Slash:
                    return global::Combat.CombatAttackPhase.Slash;
                case AttackPhase.Recovery:
                    return global::Combat.CombatAttackPhase.Recovery;
                default:
                    return global::Combat.CombatAttackPhase.Recovery;
            }
        }

        private Vector3 ResolveAttackDirection()
        {
            Vector3 forward = transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude <= 0.0001f)
            {
                return Vector3.forward;
            }

            return forward.normalized;
        }
    }

    public sealed class PlayerCombatStateContext : IActorStateContext
    {
        private readonly Dictionary<int, float> timersByKey = new Dictionary<int, float>();

        public PlayerCombatStateContext(
            PlayerStateMachine owner,
            Animator animator,
            PlayerInputHandler input,
            CharacterMotor motor)
        {
            Owner = owner;
            Animator = animator;
            Input = input;
            Motor = motor;
        }

        public PlayerStateMachine Owner { get; }
        public Animator Animator { get; }
        public PlayerInputHandler Input { get; }
        public CharacterMotor Motor { get; }
        public IIntentSource IntentSource => Input;

        public Transform ActorTransform => Owner != null ? Owner.transform : null;
        public bool IsGrounded => Motor != null && Motor.IsGrounded;
        public bool IsLockedOn => Motor != null && Motor.IsLockedOn;
        public Transform LockOnTarget => Motor != null ? Motor.GetLockOnTarget() : null;

        public void SetTimer(int key, float remainingSeconds)
        {
            timersByKey[key] = Mathf.Max(0f, remainingSeconds);
        }

        public bool TryGetTimer(int key, out float remainingSeconds)
        {
            return timersByKey.TryGetValue(key, out remainingSeconds);
        }

        public void ClearTimer(int key)
        {
            timersByKey.Remove(key);
        }
    }
}
