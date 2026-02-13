using UnityEngine;
using UnityEngine.InputSystem;

namespace Combat.Debugging
{
    [AddComponentMenu("Combat/Debug/Combat Hit Simulator")]
    public sealed class CombatHitSimulator : MonoBehaviour, ICombatant
    {
        [Header("Target")]
        [SerializeField] private MonoBehaviour targetCombatantComponent;
        [SerializeField] private Transform targetPointOverride;
        [SerializeField] private bool autoFindPlayerTarget = true;

        [Header("Hit Settings")]
        [SerializeField, Min(0f)] private float damage = 20f;
        [SerializeField] private AttackDirectionHint attackDirection = AttackDirectionHint.LeftUp;
        [SerializeField] private bool cycleDirectionsPerHit;
        [SerializeField] private Key triggerKey = Key.H;
        [SerializeField] private bool autoFire;
        [SerializeField, Min(0.05f)] private float autoFireInterval = 1f;

        private static readonly AttackDirectionHint[] DirectionCycle =
        {
            AttackDirectionHint.LeftUp,
            AttackDirectionHint.LeftDown,
            AttackDirectionHint.RightUp,
            AttackDirectionHint.RightDown
        };

        private ICombatant targetCombatant;
        private float nextAutoFireTime;
        private bool hasLoggedMissingTargetWarning;
        private int directionCycleIndex;

        public CombatTeam Team => CombatTeam.Enemy;
        public bool IsVulnerable => false;
        public bool IsAttacking => true;

        private void Awake()
        {
            ResolveTargetCombatant();
        }

        private void OnValidate()
        {
            ResolveTargetCombatant();
            autoFireInterval = Mathf.Max(0.05f, autoFireInterval);
            damage = Mathf.Max(0f, damage);
        }

        private void Update()
        {
            if (targetCombatant == null)
            {
                ResolveTargetCombatant();
            }

            if (WasTriggerKeyPressedThisFrame())
            {
                FireTestHit();
            }

            if (!autoFire || Time.time < nextAutoFireTime)
            {
                return;
            }

            FireTestHit();
            nextAutoFireTime = Time.time + autoFireInterval;
        }

        [ContextMenu("Fire Test Hit")]
        public void FireTestHit()
        {
            if (targetCombatant == null)
            {
                ResolveTargetCombatant();
            }

            if (targetCombatant == null)
            {
                if (!hasLoggedMissingTargetWarning)
                {
                    Debug.LogWarning("[CombatHitSimulator] Target is missing or does not implement ICombatant.", this);
                    hasLoggedMissingTargetWarning = true;
                }
                return;
            }

            Vector3 hitPoint = ResolveHitPoint();
            var hit = new AttackHitInfo
            {
                Damage = damage,
                Attacker = this,
                Attack = new AttackData
                {
                    AttackId = "debug_hit",
                    Damage = damage,
                    DirectionHint = ResolveDirectionHint()
                },
                HitPoint = hitPoint
            };

            targetCombatant.ReceiveHit(hit);
        }

        public void ReceiveHit(AttackHitInfo hit)
        {
            // Intentionally ignored for this debug attacker.
        }

        private bool WasTriggerKeyPressedThisFrame()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return false;
            }

            return keyboard[triggerKey].wasPressedThisFrame;
        }

        private AttackDirectionHint ResolveDirectionHint()
        {
            if (!cycleDirectionsPerHit)
            {
                return attackDirection;
            }

            AttackDirectionHint hint = DirectionCycle[directionCycleIndex];
            directionCycleIndex = (directionCycleIndex + 1) % DirectionCycle.Length;
            return hint;
        }

        private void ResolveTargetCombatant()
        {
            targetCombatant = targetCombatantComponent as ICombatant;
            if (targetCombatant != null)
            {
                hasLoggedMissingTargetWarning = false;
                return;
            }

            if (!autoFindPlayerTarget)
            {
                return;
            }

            global::Player.Combat.Player player = FindAnyObjectByType<global::Player.Combat.Player>();
            if (player == null)
            {
                return;
            }

            targetCombatantComponent = player;
            targetCombatant = player;
            hasLoggedMissingTargetWarning = false;
        }

        private Vector3 ResolveHitPoint()
        {
            if (targetPointOverride != null)
            {
                return targetPointOverride.position;
            }

            if (targetCombatant is Component targetComponent)
            {
                return targetComponent.transform.position;
            }

            return Vector3.zero;
        }
    }
}
