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
    public sealed class EnemyStateMachine : MonoBehaviour
    {
        [Header("Combat")]
        [SerializeField] private EnemyCombatProfile combatProfile;
        [SerializeField, Min(0.05f)] private float targetRefreshIntervalSeconds = 0.2f;

        public Enemy Enemy { get; private set; }
        public Animator Animator { get; private set; }
        public EnemyIntentSource IntentSource { get; private set; }
        public EnemyCombatProfile CombatProfile => combatProfile;
        public AttackStep? CurrentAttackStep { get; private set; }
        public AttackPhase CurrentAttackPhase { get; private set; } = AttackPhase.Recovery;
        public int AttackStepCount => AttackCombo != null ? AttackCombo.Count : 0;
        public AttackComboAsset AttackCombo => combatProfile != null ? combatProfile.SharedCombo : null;
        public IState CurrentState => runtime != null ? runtime.CurrentState : null;
        public string CurrentStateName => runtime != null ? runtime.CurrentStateName : "None";
        public Transform CurrentTarget => currentTarget;
        public ICombatant CurrentTargetCombatant => currentTargetCombatant;

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

            runtime = new StateMachineRuntime();
            runtime.StateChanging += HandleStateChanging;
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

        public void NotifyAttackPhase(AttackPhase phase)
        {
            CurrentAttackPhase = phase;

            if (Enemy == null)
            {
                return;
            }

            Enemy.NotifyAttackPhase(MapAttackPhase(phase));
        }

        public bool TryRefreshTarget(bool force = false)
        {
            if (!force && Time.time < nextTargetRefreshAt && currentTarget != null)
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

        private void HandleStateChanging(IState previous, IState next)
        {
            ClearCurrentAttack();
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
