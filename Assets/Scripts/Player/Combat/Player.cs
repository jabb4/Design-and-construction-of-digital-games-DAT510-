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
        [SerializeField, Min(0f)] private float baseParryWindowDuration = 0.2f;
        [SerializeField, Min(0f)] private float reducedParryWindowDuration = 0.1f;
        [SerializeField, Min(0f)] private float rapidParryPressResetDelay = 0.5f;

        private HealthComponent health;
        private CombatFlagsComponent flags;
        private PlayerInputHandler input;
        private PlayerStateMachine stateMachine;
        private CharacterMotor motor;
        private int rapidParryPressCount;
        private float lastParryPressTime = float.NegativeInfinity;

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
            if (rapidParryPressCount > 0 &&
                rapidParryPressResetDelay > 0f &&
                Time.time - lastParryPressTime > rapidParryPressResetDelay)
            {
                ResetParrySpamState();
            }

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

            if (resolution.Outcome == DamageOutcome.Parried)
            {
                ResetParrySpamState();
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

            bool canOpenParryWindow = input != null &&
                                      input.IsBlockPressed &&
                                      stateMachine != null &&
                                      stateMachine.IsEquipped &&
                                      motor != null &&
                                      motor.IsGrounded &&
                                      isAlive;

            if (canOpenParryWindow)
            {
                HandleParryPressed();
            }
            else if (!isAlive)
            {
                flags.CloseParryWindow();
                ResetParrySpamState();
            }
        }

        private void HandleParryPressed()
        {
            if (rapidParryPressResetDelay <= 0f || Time.time - lastParryPressTime > rapidParryPressResetDelay)
            {
                rapidParryPressCount = 0;
            }

            rapidParryPressCount++;
            lastParryPressTime = Time.time;

            float parryWindowDuration = GetParryWindowDuration(rapidParryPressCount);
            if (parryWindowDuration > 0f)
            {
                flags.OpenParryWindow(parryWindowDuration);
            }
            else
            {
                flags.CloseParryWindow();
            }
        }

        private float GetParryWindowDuration(int rapidPressCount)
        {
            if (rapidPressCount <= 2)
            {
                return baseParryWindowDuration;
            }

            if (rapidPressCount == 3)
            {
                return reducedParryWindowDuration;
            }

            return 0f;
        }

        private void ResetParrySpamState()
        {
            rapidParryPressCount = 0;
            lastParryPressTime = float.NegativeInfinity;
        }
    }
}
