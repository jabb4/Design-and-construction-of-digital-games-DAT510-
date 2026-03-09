using UnityEngine;
using System.Collections.Generic;
using Combat;
using Player.StateMachine;

namespace Player.Combat
{
    [RequireComponent(typeof(HealthComponent))]
    [RequireComponent(typeof(CombatFlagsComponent))]
    public class Player : MonoBehaviour, ICombatant
    {
        [SerializeField, Range(0f, 1f)] private float blockDamageMultiplier = 0.5f;
        [SerializeField, Min(0f)] private float blockLingerDuration = 0.2f;
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
        private float blockLingerUntilTime = float.NegativeInfinity;
        private bool wasBlockInputHeldLastFrame;
        private readonly List<ICombatOutcomeFeedbackHook> outcomeFeedbackHooks = new List<ICombatOutcomeFeedbackHook>(4);

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
            CacheOutcomeFeedbackHooks();
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
            DispatchOutcomeFeedback(hit, resolution);

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

            bool canUseGuard = CanUseGuard(isAlive);
            bool isBlockInputHeld = input != null && input.IsBlocking;
            UpdateBlockingState(canUseGuard, isBlockInputHeld);

            bool canOpenParryWindow = input != null && input.IsBlockPressed && canUseGuard;

            if (canOpenParryWindow)
            {
                HandleParryPressed();
            }
            else if (!isAlive)
            {
                flags.CloseParryWindow();
                ResetParrySpamState();
                ResetBlockLingerState();
            }
            else if (!canUseGuard)
            {
                flags.CloseParryWindow();
            }
        }

        private bool CanUseGuard(bool isAlive)
        {
            return input != null &&
                   stateMachine != null &&
                   stateMachine.IsEquipped &&
                   stateMachine.CanDefend &&
                   motor != null &&
                   motor.IsGrounded &&
                   isAlive;
        }

        private void UpdateBlockingState(bool canUseGuard, bool isBlockInputHeld)
        {
            if (!canUseGuard)
            {
                flags.IsBlocking = false;
                ResetBlockLingerState();
                wasBlockInputHeldLastFrame = isBlockInputHeld;
                return;
            }

            if (wasBlockInputHeldLastFrame && !isBlockInputHeld)
            {
                blockLingerUntilTime = Time.time + blockLingerDuration;
            }

            bool isBlockLingerActive = Time.time <= blockLingerUntilTime;
            flags.IsBlocking = isBlockInputHeld || isBlockLingerActive;
            wasBlockInputHeldLastFrame = isBlockInputHeld;
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

        private void ResetBlockLingerState()
        {
            blockLingerUntilTime = float.NegativeInfinity;
        }

        private void CacheOutcomeFeedbackHooks()
        {
            outcomeFeedbackHooks.Clear();
            GetComponents(outcomeFeedbackHooks);
        }

        private void DispatchOutcomeFeedback(AttackHitInfo hit, DamageResolution resolution)
        {
            if (outcomeFeedbackHooks.Count == 0)
            {
                return;
            }

            Vector3 pushDirection = ResolveDefenderPushDirection(hit.Attacker);
            var context = new CombatOutcomeFeedbackContext
            {
                Hit = hit,
                Resolution = resolution,
                Defender = this,
                DefenderPushDirection = pushDirection,
                HitPoint = hit.HitPoint,
                HitNormal = hit.HitNormal
            };

            for (int i = 0; i < outcomeFeedbackHooks.Count; i++)
            {
                outcomeFeedbackHooks[i]?.OnCombatOutcome(context);
            }
        }

        private Vector3 ResolveDefenderPushDirection(ICombatant attacker)
        {
            if (attacker is Component attackerComponent)
            {
                Vector3 fromAttacker = transform.position - attackerComponent.transform.position;
                fromAttacker.y = 0f;
                if (fromAttacker.sqrMagnitude > 0.0001f)
                {
                    return fromAttacker.normalized;
                }
            }

            Vector3 fallback = -transform.forward;
            fallback.y = 0f;
            return fallback.sqrMagnitude > 0.0001f ? fallback.normalized : Vector3.back;
        }
    }
}
