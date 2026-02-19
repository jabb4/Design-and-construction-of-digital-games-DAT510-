namespace Enemies.StateMachine
{
    using System;
    using System.Collections.Generic;
    using Combat;
    using Enemies.AI;
    using global::StateMachine.Core;
    using Player.StateMachine;
    using UnityEngine;

    [RequireComponent(typeof(Enemy))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(EnemyIntentSource))]
    [RequireComponent(typeof(EnemyNavAgentBridge))]
    public sealed class EnemyStateMachine : MonoBehaviour
    {
        [Header("Combat")]
        [SerializeField] private EnemyCombatProfile combatProfile;
        [SerializeField, Min(0.05f)] private float targetRefreshIntervalSeconds = 0.2f;

        public event Action<IState, IState> OnStateChanged;

        public Enemy Enemy { get; private set; }
        public Animator Animator { get; private set; }
        public EnemyIntentSource IntentSource { get; private set; }
        public EnemyNavAgentBridge NavBridge { get; private set; }
        public EnemyCombatProfile CombatProfile => combatProfile;
        public AttackStep? CurrentAttackStep { get; private set; }
        public AttackPhase CurrentAttackPhase { get; private set; } = AttackPhase.Recovery;
        public int AttackPhaseVersion { get; private set; }
        public int AttackRecoveryVersion { get; private set; }
        public int AttackStepCount => AttackCombo != null ? AttackCombo.Count : 0;
        public AttackComboAsset AttackCombo => combatProfile != null ? combatProfile.SharedCombo : null;
        public IState CurrentState => runtime != null ? runtime.CurrentState : null;
        public string CurrentStateName => runtime != null ? runtime.CurrentStateName : "None";
        public Transform CurrentTarget => currentTarget;
        public ICombatant CurrentTargetCombatant => currentTargetCombatant;
        public bool IsAlive => Enemy != null && Enemy.IsAlive;
        public bool HasTarget => currentTarget != null && currentTargetCombatant != null && currentTargetCombatant.IsVulnerable;
        public bool IsTargetAttacking => currentTargetCombatant != null && currentTargetCombatant.IsAttacking;
        public float DistanceToTarget
        {
            get
            {
                if (currentTarget == null)
                {
                    return float.PositiveInfinity;
                }

                return Vector3.Distance(transform.position, currentTarget.position);
            }
        }

        public float AttackRange => combatProfile != null ? combatProfile.AttackRange : 2.5f;
        public float EngageRange => combatProfile != null ? combatProfile.EngageRange : 7f;

        private readonly Dictionary<Type, EnemyStateBase> stateCache = new Dictionary<Type, EnemyStateBase>(8);
        private StateMachineRuntime runtime;
        private Transform currentTarget;
        private ICombatant currentTargetCombatant;
        private float nextTargetRefreshAt = float.NegativeInfinity;

        private void Awake()
        {
            Enemy = GetComponent<Enemy>();
            Animator = GetComponent<Animator>();
            IntentSource = GetComponent<EnemyIntentSource>();
            NavBridge = GetComponent<EnemyNavAgentBridge>();

            runtime = new StateMachineRuntime();
            runtime.StateChanging += HandleStateChanging;
            runtime.StateChanged += HandleStateChanged;
        }

        private void Start()
        {
            ChangeState<States.EnemyIdleState>();
        }

        private void Update()
        {
            runtime.Tick();
        }

        public void ChangeState<TState>() where TState : EnemyStateBase, new()
        {
            ChangeState(GetState<TState>());
        }

        public void ChangeState(IState newState)
        {
            runtime.ChangeState(newState);
        }

        public TState GetState<TState>() where TState : EnemyStateBase, new()
        {
            Type key = typeof(TState);
            if (stateCache.TryGetValue(key, out EnemyStateBase state))
            {
                return (TState)state;
            }

            var created = new TState();
            created.Initialize(this);
            stateCache[key] = created;
            return created;
        }

        public void SetCurrentAttack(AttackStep step)
        {
            CurrentAttackStep = step;
        }

        public void ClearCurrentAttack()
        {
            CurrentAttackStep = null;
            CurrentAttackPhase = AttackPhase.Recovery;
            if (Enemy != null)
            {
                Enemy.IsAttacking = false;
            }
        }

        public bool TryGetAttackStep(int index, out AttackStep step)
        {
            if (AttackCombo == null)
            {
                step = default;
                return false;
            }

            return AttackCombo.TryGetStep(index, out step);
        }

        public int SampleParriesBeforeCounter()
        {
            int min = 1;
            int max = 1;
            if (combatProfile != null)
            {
                min = Mathf.Max(1, combatProfile.MinParriesBeforeCounter);
                max = Mathf.Max(min, combatProfile.MaxParriesBeforeCounter);
            }

            return UnityEngine.Random.Range(min, max + 1);
        }

        public int SampleAttackChainLength()
        {
            int min = 2;
            int max = 5;
            if (combatProfile != null)
            {
                min = Mathf.Max(1, combatProfile.MinAttackChain);
                max = Mathf.Max(min, combatProfile.MaxAttackChain);
            }

            int sampled = UnityEngine.Random.Range(min, max + 1);
            if (AttackStepCount > 0)
            {
                sampled = Mathf.Clamp(sampled, 1, AttackStepCount);
            }

            return sampled;
        }

        public bool TryPlayAttackStepByIndex(int index, float crossFadeDuration = 0.08f)
        {
            if (!TryGetAttackStep(index, out AttackStep step))
            {
                return false;
            }

            SetCurrentAttack(step);
            CurrentAttackPhase = AttackPhase.Windup;
            return TryCrossFadeState(step.AnimationStateName, crossFadeDuration);
        }

        public bool IsCurrentAttackAnimationComplete(float normalizedThreshold = 0.98f)
        {
            if (!CurrentAttackStep.HasValue || Animator == null)
            {
                return true;
            }

            AttackStep step = CurrentAttackStep.Value;
            AnimatorStateInfo info = Animator.GetCurrentAnimatorStateInfo(0);
            return info.IsName(step.AnimationStateName) && info.normalizedTime >= normalizedThreshold;
        }

        public void NotifyAttackPhase(AttackPhase phase)
        {
            CurrentAttackPhase = phase;
            AttackPhaseVersion++;
            if (phase == AttackPhase.Recovery)
            {
                AttackRecoveryVersion++;
            }

            if (Enemy == null)
            {
                return;
            }

            AttackData? attack = null;
            if (CurrentAttackStep.HasValue)
            {
                attack = global::Player.Combat.AttackDataMapper.ToAttackData(CurrentAttackStep.Value);
            }

            Enemy.NotifyAttackPhase(MapAttackPhase(phase), attack);
        }

        public bool TryRefreshTarget(bool force = false)
        {
            if (!force && Time.time < nextTargetRefreshAt && IsTargetStillValid())
            {
                return true;
            }

            nextTargetRefreshAt = Time.time + targetRefreshIntervalSeconds;

            MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            Transform bestTarget = null;
            ICombatant bestCombatant = null;
            float bestDistanceSq = float.MaxValue;

            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null)
                {
                    continue;
                }

                var combatant = behaviour as ICombatant;
                if (combatant == null || combatant.Team != CombatTeam.Player || !combatant.IsVulnerable)
                {
                    continue;
                }

                Transform target = behaviour.transform;
                Vector3 delta = target.position - transform.position;
                float distanceSq = delta.sqrMagnitude;
                if (distanceSq >= bestDistanceSq)
                {
                    continue;
                }

                bestDistanceSq = distanceSq;
                bestTarget = target;
                bestCombatant = combatant;
            }

            currentTarget = bestTarget;
            currentTargetCombatant = bestCombatant;
            return currentTarget != null;
        }

        public bool TryCrossFadeState(string stateName, float duration = 0.08f, int layer = 0)
        {
            if (Animator == null || Animator.runtimeAnimatorController == null || string.IsNullOrWhiteSpace(stateName))
            {
                return false;
            }

            if (layer < 0 || layer >= Animator.layerCount)
            {
                layer = 0;
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

            return false;
        }

        private bool IsTargetStillValid()
        {
            return currentTarget != null && currentTargetCombatant != null && currentTargetCombatant.IsVulnerable;
        }

        private void HandleStateChanging(IState previous, IState next)
        {
            ClearCurrentAttack();
        }

        private void HandleStateChanged(IState previous, IState current)
        {
            OnStateChanged?.Invoke(previous, current);
        }

        private static CombatAttackPhase MapAttackPhase(AttackPhase phase)
        {
            switch (phase)
            {
                case AttackPhase.Windup:
                    return CombatAttackPhase.Windup;
                case AttackPhase.Slash:
                    return CombatAttackPhase.Slash;
                case AttackPhase.Recovery:
                    return CombatAttackPhase.Recovery;
                default:
                    return CombatAttackPhase.Recovery;
            }
        }
    }
}
