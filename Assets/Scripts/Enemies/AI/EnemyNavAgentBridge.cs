namespace Enemies.AI
{
    using Combat;
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
        private float orbitAngleDegrees;

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

        private void OnDisable()
        {
            Stop();
        }

        private void Update()
        {
            if (navMeshAgent == null)
            {
                return;
            }

            if (impulseDriver != null && impulseDriver.IsImpulseActive)
            {
                PauseAgent();
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
            orbitRadius = Mathf.Max(0.1f, desiredRadius);
        }

        public void Stop()
        {
            mode = MoveMode.Stopped;
            target = null;
            PauseAgent();
        }

        private void UpdatePursue()
        {
            navMeshAgent.stoppingDistance = stoppingDistance;
            navMeshAgent.SetDestination(target.position);
        }

        private void UpdateOrbit()
        {
            Vector3 center = target.position;
            orbitAngleDegrees += orbitAngularSpeedDegrees * Time.deltaTime;
            float radians = orbitAngleDegrees * Mathf.Deg2Rad;
            Vector3 candidate = center + new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians)) * orbitRadius;

            float distanceToCandidate = Vector3.Distance(transform.position, candidate);
            if (distanceToCandidate <= orbitDestinationTolerance && navMeshAgent.hasPath)
            {
                return;
            }

            navMeshAgent.stoppingDistance = 0.1f;
            navMeshAgent.SetDestination(candidate);
        }

        private void PauseAgent()
        {
            if (navMeshAgent == null)
            {
                return;
            }

            navMeshAgent.isStopped = true;
            if (navMeshAgent.hasPath)
            {
                navMeshAgent.ResetPath();
            }
        }

        private void ResumeAgent()
        {
            if (navMeshAgent != null)
            {
                navMeshAgent.isStopped = false;
            }
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
    }
}
