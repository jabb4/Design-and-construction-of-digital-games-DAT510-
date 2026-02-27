namespace Enemies.AI
{
    using global::Combat;
    using UnityEngine;
    using UnityEngine.AI;

    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class EnemyNavAgentBridge : MonoBehaviour
    {
        private enum MoveMode
        {
            Stopped,
            Pursue,
            Orbit
        }

        [Header("Optional References")]
        [SerializeField] private NavMeshAgent navMeshAgent;
        [SerializeField] private CombatHorizontalImpulseDriver impulseDriver;

        [Header("Orbit Tuning")]
        [SerializeField, Min(5f)] private float orbitAngularSpeedDegrees = 90f;
        [SerializeField, Min(0.1f)] private float orbitDestinationTolerance = 0.25f;

        private MoveMode mode = MoveMode.Stopped;
        private Transform target;
        private float stoppingDistance = 1.8f;
        private float orbitRadius = 2.75f;
        private float targetOrbitRadius = 2.75f;
        private float orbitAngleDegrees;
        private float orbitAngleOffset;
        private float angularSpeedJitter;
        private float orbitDirectionSign;
        private float orbitPathRequestTimer;
        private bool impulsePauseActive;
        private bool restoreUpdatePositionAfterImpulse = true;

        private const float OrbitPathRequestInterval = 0.15f;
        private const float OrbitRadiusSmoothSpeed = 4f;
        private const float OffMeshRecoveryMaxDistance = 3f;

        public Vector3 CurrentVelocity => navMeshAgent != null ? navMeshAgent.velocity : Vector3.zero;
        public Vector3 DesiredVelocity => navMeshAgent != null ? navMeshAgent.desiredVelocity : Vector3.zero;
        public bool IsStopped => mode == MoveMode.Stopped;

        private void Awake()
        {
            ResolveReferences();
            ConfigureAgent();
        }

        private void OnValidate()
        {
            orbitAngularSpeedDegrees = Mathf.Max(5f, orbitAngularSpeedDegrees);
            orbitDestinationTolerance = Mathf.Max(0.1f, orbitDestinationTolerance);
            ResolveReferences();
        }

        private void OnEnable()
        {
            orbitAngleOffset = Random.Range(0f, 360f);
            angularSpeedJitter = Random.Range(0.8f, 1.2f);
            orbitDirectionSign = Random.value > 0.5f ? 1f : -1f;
        }

        private void OnDisable()
        {
            Stop();
            if (navMeshAgent != null && restoreUpdatePositionAfterImpulse)
            {
                navMeshAgent.updatePosition = true;
            }

            impulsePauseActive = false;
        }

        private void Update()
        {
            if (navMeshAgent == null || !navMeshAgent.isActiveAndEnabled || !navMeshAgent.enabled)
            {
                return;
            }

            if (!navMeshAgent.isOnNavMesh)
            {
                TryRecoverOffMeshAgent();
                return;
            }

            if (impulseDriver != null && impulseDriver.IsImpulseActive)
            {
                PauseAgentForImpulse();
                return;
            }

            if (target == null || mode == MoveMode.Stopped)
            {
                PauseAgent();
                return;
            }

            ResumeAgent();

            switch (mode)
            {
                case MoveMode.Pursue:
                    UpdatePursue();
                    break;
                case MoveMode.Orbit:
                    UpdateOrbit();
                    break;
            }
        }

        public void SetPursue(Transform pursueTarget, float stopDistance)
        {
            target = pursueTarget;
            mode = target == null ? MoveMode.Stopped : MoveMode.Pursue;
            stoppingDistance = Mathf.Max(0f, stopDistance);
        }

        public void SetOrbit(Transform orbitTarget, float desiredRadius)
        {
            target = orbitTarget;
            mode = target == null ? MoveMode.Stopped : MoveMode.Orbit;
            targetOrbitRadius = Mathf.Max(0.1f, desiredRadius);
        }

        public void Stop()
        {
            mode = MoveMode.Stopped;
            target = null;
            PauseAgent();
        }

        private void UpdatePursue()
        {
            if (!IsAgentReadyForCommands())
            {
                return;
            }

            navMeshAgent.stoppingDistance = stoppingDistance;
            navMeshAgent.SetDestination(target.position);
        }

        private void UpdateOrbit()
        {
            if (!IsAgentReadyForCommands())
            {
                return;
            }

            orbitRadius = Mathf.MoveTowards(orbitRadius, targetOrbitRadius, OrbitRadiusSmoothSpeed * Time.deltaTime);

            Vector3 center = target.position;
            orbitAngleDegrees += orbitDirectionSign * orbitAngularSpeedDegrees * angularSpeedJitter * Time.deltaTime;
            float effectiveAngle = orbitAngleDegrees + orbitAngleOffset;
            float radians = effectiveAngle * Mathf.Deg2Rad;
            Vector3 candidate = center + new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians)) * orbitRadius;

            float distanceToCandidate = Vector3.Distance(transform.position, candidate);
            if (distanceToCandidate <= orbitDestinationTolerance && navMeshAgent.hasPath)
            {
                return;
            }

            orbitPathRequestTimer -= Time.deltaTime;
            if (orbitPathRequestTimer > 0f)
            {
                return;
            }
            orbitPathRequestTimer = OrbitPathRequestInterval;

            navMeshAgent.stoppingDistance = 0.1f;
            navMeshAgent.SetDestination(candidate);
        }

        private void PauseAgent()
        {
            if (!IsAgentReadyForCommands())
            {
                return;
            }

            if (impulseDriver != null && impulseDriver.IsImpulseActive)
            {
                PauseAgentForImpulse();
                return;
            }

            RestoreAgentPositionSyncIfNeeded();
            PauseAgentCore();
        }

        private void PauseAgentForImpulse()
        {
            if (!IsAgentReadyForCommands())
            {
                return;
            }

            if (!impulsePauseActive)
            {
                restoreUpdatePositionAfterImpulse = navMeshAgent.updatePosition;
                if (restoreUpdatePositionAfterImpulse)
                {
                    // Let rigidbody-driven impulse own transform position during lunge/pushback.
                    navMeshAgent.updatePosition = false;
                }

                impulsePauseActive = true;
            }

            PauseAgentCore();
        }

        private void PauseAgentCore()
        {
            navMeshAgent.isStopped = true;
            if (navMeshAgent.hasPath)
            {
                navMeshAgent.ResetPath();
            }
        }

        private void ResumeAgent()
        {
            if (!IsAgentReadyForCommands())
            {
                return;
            }

            RestoreAgentPositionSyncIfNeeded();
            navMeshAgent.isStopped = false;
        }

        private void ConfigureAgent()
        {
            if (navMeshAgent == null)
            {
                return;
            }

            navMeshAgent.updateRotation = false;
        }

        private void ResolveReferences()
        {
            if (navMeshAgent == null)
            {
                navMeshAgent = GetComponent<NavMeshAgent>();
            }

            if (impulseDriver == null)
            {
                impulseDriver = GetComponent<CombatHorizontalImpulseDriver>();
            }
        }

        private bool IsAgentReadyForCommands()
        {
            return navMeshAgent != null &&
                   navMeshAgent.isActiveAndEnabled &&
                   navMeshAgent.enabled &&
                   navMeshAgent.isOnNavMesh;
        }

        private void TryRecoverOffMeshAgent()
        {
            if (navMeshAgent == null || !navMeshAgent.isActiveAndEnabled)
            {
                return;
            }

            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, OffMeshRecoveryMaxDistance, NavMesh.AllAreas))
            {
                navMeshAgent.Warp(hit.position);
            }
        }

        private void RestoreAgentPositionSyncIfNeeded()
        {
            if (!impulsePauseActive || navMeshAgent == null || !navMeshAgent.enabled)
            {
                return;
            }

            if (restoreUpdatePositionAfterImpulse)
            {
                if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, OffMeshRecoveryMaxDistance, NavMesh.AllAreas))
                {
                    navMeshAgent.Warp(hit.position);
                }
                else
                {
                    navMeshAgent.nextPosition = transform.position;
                }

                navMeshAgent.updatePosition = true;
            }

            impulsePauseActive = false;
        }
    }
}
