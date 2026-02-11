using UnityEngine;
using Combat;
using Player.StateMachine;

namespace Player.Combat
{
    [RequireComponent(typeof(HealthComponent))]
    [RequireComponent(typeof(CombatFlagsComponent))]
    public class Player : MonoBehaviour, ICombatant
    {
        [SerializeField, Range(0f, 1f)] private float blockDamageMultiplier = 0.5f;

        private HealthComponent health;
        private CombatFlagsComponent flags;
        private PlayerInputHandler input;
        private PlayerStateMachine stateMachine;
        private CharacterMotor motor;

        public CombatTeam Team => CombatTeam.Player;
        public bool IsVulnerable => flags != null && flags.IsVulnerable;
        public bool IsAttacking
        {
            get => flags != null && flags.IsAttacking;
            set
            {
                if (flags != null)
                {
                    flags.IsAttacking = value;
                }
            }
        }

        private void Awake()
        {
            health = GetComponent<HealthComponent>();
            flags = GetComponent<CombatFlagsComponent>();
            if (health == null)
            {
                health = gameObject.AddComponent<HealthComponent>();
            }

            if (flags == null)
            {
                flags = gameObject.AddComponent<CombatFlagsComponent>();
            }

            input = GetComponent<PlayerInputHandler>();
            stateMachine = GetComponent<PlayerStateMachine>();
            motor = GetComponent<CharacterMotor>();
            SyncCombatFlags();
        }

        private void Update()
        {
            SyncCombatFlags();
        }

        public void ReceiveHit(AttackHitInfo hit)
        {
            if (health == null)
            {
                return;
            }

            DamageResolution resolution = DamageResolver.ResolveDamage(hit.Damage, flags, blockDamageMultiplier);
            if (resolution.Outcome == DamageOutcome.Ignored)
            {
                return;
            }

            health.ApplyDamage(resolution.AppliedDamage);
            SyncCombatFlags();
        }

        private void SyncCombatFlags()
        {
            if (flags == null)
            {
                return;
            }

            bool isAlive = health == null || !health.IsDead;
            flags.IsVulnerable = isAlive;

            bool canBlock = input != null &&
                            input.IsBlocking &&
                            stateMachine != null &&
                            stateMachine.IsEquipped &&
                            motor != null &&
                            motor.IsGrounded &&
                            isAlive;

            flags.IsBlocking = canBlock;
        }
    }
}
