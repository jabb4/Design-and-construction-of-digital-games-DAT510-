namespace Enemies.StateMachine
{
    using System;
    using System.Collections.Generic;
    using global::Combat;
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
        private static readonly int VelocityXHash = Animator.StringToHash("VelocityX");
        private static readonly int VelocityZHash = Animator.StringToHash("VelocityZ");
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
        private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
        private static readonly int IsSprintingHash = Animator.StringToHash("IsSprinting");
        private static readonly int IsInAirHash = Animator.StringToHash("IsInAir");
        private static readonly int IsBlockingHash = Animator.StringToHash("IsBlocking");
        private static readonly List<EnemyStateMachine> RegisteredEnemies = new List<EnemyStateMachine>(32);
        private const float PlayerTargetResolveRetrySeconds = 0.5f;
        private static Transform cachedPlayerTarget;
        private static ICombatant cachedPlayerCombatant;
        private static float nextPlayerTargetResolveAt = float.NegativeInfinity;

        [Header("Combat")]
        [SerializeField] private EnemyCombatProfile combatProfile;
        [SerializeField, Min(0.05f)] private float targetRefreshIntervalSeconds = 0.2f;
        [Header("Animation")]
        [SerializeField, Min(0f)] private float locomotionSmoothingSeconds = 0.08f;
        [SerializeField, Min(0f)] private float movingSpeedThreshold = 0.05f;

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
        private Enemies.Combat.EnemyDefenseReactionAnimationDriver defenseReactionDriver;
        private float nextTargetRefreshAt = float.NegativeInfinity;
        private int cachedNearbyEnemyCount = 1;
        private float cachedNearbyEnemyRadius = -1f;
        private float nextNearbyEnemyRefreshAt = float.NegativeInfinity;
        private const float nearbyEnemyRefreshInterval = 0.2f;
        private Vector2 smoothedLocalVelocity;
        private Vector2 smoothedLocalVelocityRef;

        private void Awake()
        {
            Enemy = GetComponent<Enemy>();
            Animator = GetComponent<Animator>();
            IntentSource = GetComponent<EnemyIntentSource>();
            NavBridge = GetComponent<EnemyNavAgentBridge>();
            defenseReactionDriver = GetComponent<Enemies.Combat.EnemyDefenseReactionAnimationDriver>();

            runtime = new StateMachineRuntime();
            runtime.StateChanging += HandleStateChanging;
            runtime.StateChanged += HandleStateChanged;
        }

        private void OnEnable()
        {
            RegisterEnemy(this);
        }

        private void OnValidate()
        {
            ValidateConfiguration();
        }

        private void Start()
        {
            ApplyEnemyAnimatorDefaults();
            ChangeState<States.EnemyIdleState>();
        }

        private void OnDisable()
        {
            UnregisterEnemy(this);
            CleanupRuntimeState();
        }

        private void OnDestroy()
        {
            UnregisterEnemy(this);
            CleanupRuntimeState();

            if (runtime != null)
            {
                runtime.StateChanging -= HandleStateChanging;
                runtime.StateChanged -= HandleStateChanged;
            }
        }

        private void Update()
        {
            ApplyEnemyAnimatorDefaults();
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

        public float SampleDefenseDurationSeconds()
        {
            float min = 0.9f;
            float max = 1.8f;
            if (combatProfile != null)
            {
                min = Mathf.Max(0.05f, combatProfile.MinDefenseDuration);
                max = Mathf.Max(min, combatProfile.MaxDefenseDuration);
            }

            return UnityEngine.Random.Range(min, max);
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

        public int SampleAttackChainLengthForCurrentGroup()
        {
            return SampleAttackChainLength();
        }

        public float ComputeSupportOrbitRadiusForCurrentGroup()
        {
            float baseRadius = combatProfile != null ? combatProfile.OrbitRadius : 2.75f;
            if (combatProfile == null)
            {
                return baseRadius;
            }

            int extraEnemies = Mathf.Max(0, GetNearbyEnemyCount(combatProfile.GroupAwarenessRadius) - 1);
            float dynamicExtra = combatProfile.SupportOrbitExtraRadiusPerEnemy * extraEnemies;
            return baseRadius + combatProfile.SupportOrbitExtraRadius + dynamicExtra;
        }

        public float ComputeAttackTokenReleaseCooldownForCurrentGroup()
        {
            if (combatProfile == null)
            {
                return 0f;
            }

            int extraEnemies = Mathf.Max(0, GetNearbyEnemyCount(combatProfile.GroupAwarenessRadius) - 1);
            return combatProfile.AttackTokenCooldownBase +
                   (combatProfile.AttackTokenCooldownPerExtraEnemy * extraEnemies);
        }

        public bool IsClosestEnemyToCurrentTarget(float distanceBias = 0.15f, bool requireTokenEligibility = false)
        {
            if (!HasTarget || currentTarget == null)
            {
                return false;
            }

            Vector3 targetPosition = currentTarget.position;
            float biasSq = Mathf.Max(0f, distanceBias) * Mathf.Max(0f, distanceBias);
            int ownId = GetInstanceID();
            float bestDistanceSq = float.PositiveInfinity;
            int bestId = int.MaxValue;

            PruneRegisteredEnemies();
            for (int i = 0; i < RegisteredEnemies.Count; i++)
            {
                EnemyStateMachine enemy = RegisteredEnemies[i];
                if (enemy == null || !enemy.IsAlive || enemy.currentTarget != currentTarget)
                {
                    continue;
                }

                if (requireTokenEligibility && !EnemyAttackTokenService.CanAcquire(enemy))
                {
                    continue;
                }

                Vector3 delta = enemy.transform.position - targetPosition;
                delta.y = 0f;
                float distanceSq = delta.sqrMagnitude;
                int enemyId = enemy.GetInstanceID();

                bool isClearlyCloser = distanceSq + biasSq < bestDistanceSq;
                bool isTieButLowerId = Mathf.Abs(distanceSq - bestDistanceSq) <= biasSq && enemyId < bestId;
                if (!isClearlyCloser && !isTieButLowerId)
                {
                    continue;
                }

                bestDistanceSq = distanceSq;
                bestId = enemyId;
            }

            if (bestId == int.MaxValue)
            {
                return false;
            }

            return bestId == ownId;
        }

        public int GetNearbyEnemyCount(float radius)
        {
            if (!HasTarget)
            {
                cachedNearbyEnemyCount = 1;
                cachedNearbyEnemyRadius = Mathf.Max(0.1f, radius);
                return cachedNearbyEnemyCount;
            }

            float clampedRadius = Mathf.Max(0.1f, radius);
            if (Mathf.Abs(cachedNearbyEnemyRadius - clampedRadius) > 0.01f
                || Time.time >= nextNearbyEnemyRefreshAt)
            {
                RefreshNearbyEnemyCount(clampedRadius);
                nextNearbyEnemyRefreshAt = Time.time + nearbyEnemyRefreshInterval;
            }

            return Mathf.Max(1, cachedNearbyEnemyCount);
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
            if (!TryResolvePlayerTarget(out currentTarget, out currentTargetCombatant) ||
                !currentTargetCombatant.IsVulnerable)
            {
                currentTarget = null;
                currentTargetCombatant = null;
                return false;
            }

            RefreshNearbyEnemyCount(combatProfile != null ? combatProfile.GroupAwarenessRadius : 8f);
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

            return false;
        }

        public bool TryCrossFadeStateIfNotActive(string stateName, float duration = 0.08f, int layer = 0)
        {
            if (IsInOrTransitioningToState(stateName, layer))
            {
                return false;
            }

            return TryCrossFadeState(stateName, duration, layer);
        }

        private bool TryCrossFadeWithSubStatePaths(string stateName, float duration, int layer)
        {
            string layerName = Animator.GetLayerName(layer);
            string[] preferredPaths =
            {
                $"{layerName}.Grounded.Equip Locomotion.{stateName}",
                $"{layerName}.Grounded.Unequip Locomotion.{stateName}",
                $"{layerName}.Airborne.Equip Jump.{stateName}",
                $"{layerName}.Airborne.Unequip Jump.{stateName}"
            };

            for (int i = 0; i < preferredPaths.Length; i++)
            {
                int pathHash = Animator.StringToHash(preferredPaths[i]);
                if (!Animator.HasState(layer, pathHash))
                {
                    continue;
                }

                Animator.CrossFadeInFixedTime(pathHash, duration, layer);
                return true;
            }

            return false;
        }

        private bool IsInOrTransitioningToState(string stateName, int layer)
        {
            if (Animator == null || string.IsNullOrWhiteSpace(stateName))
            {
                return false;
            }

            if (layer < 0 || layer >= Animator.layerCount)
            {
                layer = 0;
            }

            int shortNameHash = Animator.StringToHash(stateName);
            AnimatorStateInfo current = Animator.GetCurrentAnimatorStateInfo(layer);
            if (current.shortNameHash == shortNameHash || current.IsName(stateName))
            {
                return true;
            }

            if (!Animator.IsInTransition(layer))
            {
                return false;
            }

            AnimatorStateInfo next = Animator.GetNextAnimatorStateInfo(layer);
            return next.shortNameHash == shortNameHash || next.IsName(stateName);
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

        private void ApplyEnemyAnimatorDefaults()
        {
            if (Animator == null)
            {
                return;
            }

            Animator.SetBool("IsEquipped", true);
            Animator.SetBool("IsTransitioningWeapon", false);

            bool hasParryWindow = Enemy != null && Enemy.IsParryWindowActive;
            bool hasDefenseReaction = defenseReactionDriver != null && defenseReactionDriver.IsReactionActive;
            bool isDefenseTurn = CurrentState is States.EnemyDefenseTurnState;
            bool isBlockingForAnimator = isDefenseTurn || hasParryWindow || hasDefenseReaction;

            // Animation-side guard keeps defense/parry sub-state transitions valid.
            // Combat still remains parry-only because Enemy/CombatFlags never set gameplay blocking.
            Animator.SetBool(IsBlockingHash, isBlockingForAnimator);
            Animator.SetBool("IsLockedOn", HasTarget);
            Animator.SetBool(IsGroundedHash, true);
            Animator.SetBool(IsSprintingHash, false);
            Animator.SetBool(IsInAirHash, false);

            UpdateLocomotionAnimatorParameters();
        }

        private void UpdateLocomotionAnimatorParameters()
        {
            Vector3 worldVelocity = Vector3.zero;
            if (NavBridge != null)
            {
                worldVelocity = NavBridge.CurrentVelocity;
                Vector3 desiredVelocity = NavBridge.DesiredVelocity;
                if (worldVelocity.sqrMagnitude < desiredVelocity.sqrMagnitude)
                {
                    worldVelocity = desiredVelocity;
                }
            }

            worldVelocity.y = 0f;
            Vector3 localVelocity3 = transform.InverseTransformDirection(worldVelocity);
            Vector2 targetLocalVelocity = new Vector2(localVelocity3.x, localVelocity3.z);

            if (locomotionSmoothingSeconds > 0f)
            {
                smoothedLocalVelocity = Vector2.SmoothDamp(
                    smoothedLocalVelocity,
                    targetLocalVelocity,
                    ref smoothedLocalVelocityRef,
                    locomotionSmoothingSeconds);
            }
            else
            {
                smoothedLocalVelocity = targetLocalVelocity;
                smoothedLocalVelocityRef = Vector2.zero;
            }

            float speed = smoothedLocalVelocity.magnitude;
            bool isMoving = speed > movingSpeedThreshold;

            Animator.SetFloat(VelocityXHash, smoothedLocalVelocity.x);
            Animator.SetFloat(VelocityZHash, smoothedLocalVelocity.y);
            Animator.SetFloat(SpeedHash, speed);
            Animator.SetBool(IsMovingHash, isMoving);
        }

        private void ValidateConfiguration()
        {
            if (combatProfile == null)
            {
                Debug.LogWarning("[EnemyStateMachine] Missing EnemyCombatProfile.", this);
                return;
            }

            if (combatProfile.SharedCombo == null)
            {
                Debug.LogWarning("[EnemyStateMachine] EnemyCombatProfile has no shared combo assigned.", combatProfile);
            }

            if (combatProfile.MinAttackChain < 2 || combatProfile.MaxAttackChain > 5)
            {
                Debug.LogWarning(
                    $"[EnemyStateMachine] Attack chain range should stay within [2..5]. Current: [{combatProfile.MinAttackChain}..{combatProfile.MaxAttackChain}]",
                    combatProfile);
            }

            if (combatProfile.MinParriesBeforeCounter < 1 || combatProfile.MaxParriesBeforeCounter > 5)
            {
                Debug.LogWarning(
                    $"[EnemyStateMachine] Parries-before-counter range should stay within [1..5]. Current: [{combatProfile.MinParriesBeforeCounter}..{combatProfile.MaxParriesBeforeCounter}]",
                    combatProfile);
            }

            if (combatProfile.MinDefenseDuration <= 0f || combatProfile.MaxDefenseDuration < combatProfile.MinDefenseDuration)
            {
                Debug.LogWarning(
                    $"[EnemyStateMachine] Defense duration range is invalid. Current: [{combatProfile.MinDefenseDuration}..{combatProfile.MaxDefenseDuration}]",
                    combatProfile);
            }
        }

        private void CleanupRuntimeState()
        {
            IntentSource?.ClearAllIntents();
            NavBridge?.Stop();
            Enemy?.CloseParryWindow();
            EnemyAttackTokenService.ReleaseAll(this);
            ClearCurrentAttack();

            currentTarget = null;
            currentTargetCombatant = null;
            nextTargetRefreshAt = float.NegativeInfinity;
            cachedNearbyEnemyCount = 1;
            cachedNearbyEnemyRadius = -1f;
            nextNearbyEnemyRefreshAt = float.NegativeInfinity;
            smoothedLocalVelocity = Vector2.zero;
            smoothedLocalVelocityRef = Vector2.zero;
        }

        private void RefreshNearbyEnemyCount(float radius)
        {
            cachedNearbyEnemyRadius = Mathf.Max(0.1f, radius);
            if (!HasTarget)
            {
                cachedNearbyEnemyCount = 1;
                return;
            }

            Vector3 center = currentTarget.position;
            float radiusSq = cachedNearbyEnemyRadius * cachedNearbyEnemyRadius;
            int count = 0;

            PruneRegisteredEnemies();
            for (int i = 0; i < RegisteredEnemies.Count; i++)
            {
                EnemyStateMachine enemy = RegisteredEnemies[i];
                if (enemy == null || !enemy.IsAlive)
                {
                    continue;
                }

                Vector3 delta = enemy.transform.position - center;
                delta.y = 0f;
                if (delta.sqrMagnitude > radiusSq)
                {
                    continue;
                }

                count++;
            }

            cachedNearbyEnemyCount = Mathf.Max(1, count);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticRuntimeState()
        {
            RegisteredEnemies.Clear();
            cachedPlayerTarget = null;
            cachedPlayerCombatant = null;
            nextPlayerTargetResolveAt = float.NegativeInfinity;
        }

        private static void RegisterEnemy(EnemyStateMachine enemy)
        {
            if (enemy == null || RegisteredEnemies.Contains(enemy))
            {
                return;
            }

            RegisteredEnemies.Add(enemy);
        }

        private static void UnregisterEnemy(EnemyStateMachine enemy)
        {
            if (enemy == null)
            {
                return;
            }

            RegisteredEnemies.Remove(enemy);
        }

        private static void PruneRegisteredEnemies()
        {
            for (int i = RegisteredEnemies.Count - 1; i >= 0; i--)
            {
                EnemyStateMachine enemy = RegisteredEnemies[i];
                if (enemy != null)
                {
                    continue;
                }

                RegisteredEnemies.RemoveAt(i);
            }
        }

        private static bool TryResolvePlayerTarget(out Transform target, out ICombatant combatant)
        {
            if (IsPlayerTargetValid(cachedPlayerTarget, cachedPlayerCombatant))
            {
                target = cachedPlayerTarget;
                combatant = cachedPlayerCombatant;
                return true;
            }

            if (Time.time < nextPlayerTargetResolveAt)
            {
                target = null;
                combatant = null;
                return false;
            }

            nextPlayerTargetResolveAt = Time.time + PlayerTargetResolveRetrySeconds;
            var player = FindFirstObjectByType<global::Player.Combat.Player>(FindObjectsInactive.Exclude);
            if (player == null)
            {
                cachedPlayerTarget = null;
                cachedPlayerCombatant = null;
                target = null;
                combatant = null;
                return false;
            }

            cachedPlayerTarget = player.transform;
            cachedPlayerCombatant = player;
            target = cachedPlayerTarget;
            combatant = cachedPlayerCombatant;
            return true;
        }

        private static bool IsPlayerTargetValid(Transform target, ICombatant combatant)
        {
            if (target == null || combatant == null)
            {
                return false;
            }

            if (combatant.Team != CombatTeam.Player)
            {
                return false;
            }

            if (combatant is Component component)
            {
                if (component == null || !component.gameObject.activeInHierarchy || component.transform != target)
                {
                    return false;
                }

                if (component is Behaviour behaviour)
                {
                    return behaviour.enabled;
                }

                return true;
            }

            return true;
        }

        private static global::Combat.CombatAttackPhase MapAttackPhase(AttackPhase phase)
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
