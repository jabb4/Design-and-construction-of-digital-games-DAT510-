using System.Collections.Generic;
using Combat;
using Player.StateMachine;
using UnityEngine;

namespace Player.Combat
{
    /// <summary>
    /// Plays block/parry defense reactions from combat outcomes and keeps guard-side state in sync.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(PlayerStateMachine))]
    public sealed class PlayerDefenseReactionAnimationDriver : MonoBehaviour, ICombatOutcomeFeedbackHook
    {
        [Header("Reaction Timing")]
        [SerializeField, Min(0.01f)] private float reactionCrossFadeDuration = 0.06f;
        [SerializeField, Min(0.01f)] private float returnCrossFadeDuration = 0.08f;
        [SerializeField, Range(0.5f, 1f)] private float reactionCompleteThreshold = 0.95f;
        [SerializeField, Min(0.1f)] private float reactionTimeoutSeconds = 1.5f;
        [SerializeField, Min(0f)] private float attackUnlockDelayAfterDefenseSeconds = 0.12f;

        [Header("Optional References")]
        [SerializeField] private Animator animator;
        [SerializeField] private PlayerStateMachine stateMachine;
        [SerializeField] private PlayerInputHandler input;
        [SerializeField] private AttackComboDirectionResolver comboDirectionResolver;

        private static readonly HashSet<int> LoggedMissingStates = new HashSet<int>();

        private bool reactionActive;
        private string activeReactionStateName;
        private int activeReactionStateHash;
        private float reactionTimeoutAt;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnValidate()
        {
            reactionCrossFadeDuration = Mathf.Max(0.01f, reactionCrossFadeDuration);
            returnCrossFadeDuration = Mathf.Max(0.01f, returnCrossFadeDuration);
            reactionCompleteThreshold = Mathf.Clamp(reactionCompleteThreshold, 0.5f, 1f);
            reactionTimeoutSeconds = Mathf.Max(0.1f, reactionTimeoutSeconds);
            attackUnlockDelayAfterDefenseSeconds = Mathf.Max(0f, attackUnlockDelayAfterDefenseSeconds);
            ResolveReferences();
        }

        private void OnDisable()
        {
            ClearReactionState();
        }

        private void Update()
        {
            if (!reactionActive || animator == null)
            {
                return;
            }

            bool timedOut = Time.time >= reactionTimeoutAt;
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            bool inActiveReactionState = stateInfo.shortNameHash == activeReactionStateHash || stateInfo.IsName(activeReactionStateName);
            bool inTransition = animator.IsInTransition(0);
            bool completeInReactionState = inActiveReactionState && !inTransition && stateInfo.normalizedTime >= reactionCompleteThreshold;
            bool transitionedOutOfReaction = !inActiveReactionState && !inTransition;

            if (completeInReactionState || transitionedOutOfReaction || timedOut)
            {
                EndReaction();
            }
        }

        public void OnCombatOutcome(CombatOutcomeFeedbackContext context)
        {
            if (!isActiveAndEnabled || animator == null || stateMachine == null)
            {
                return;
            }

            if (context.Defender is Component defenderComponent && defenderComponent.gameObject != gameObject)
            {
                return;
            }

            switch (context.Resolution.Outcome)
            {
                case DamageOutcome.Blocked:
                    PlayBlockedReaction();
                    break;
                case DamageOutcome.Parried:
                    PlayParryReaction(context.Hit);
                    break;
            }
        }

        private void PlayBlockedReaction()
        {
            string stateName = stateMachine.GetDefenseBlockReactionStateName();
            BeginReaction(stateName);
        }

        private void PlayParryReaction(AttackHitInfo hit)
        {
            AttackDirectionHint hint = ResolveAttackDirectionHint(hit);
            if (!TryGetParryState(hint, out GuardSide targetSide, out string parryState))
            {
                PlayBlockedReaction();
                return;
            }

            stateMachine.SetGuardSide(targetSide);
            BeginReaction(parryState);
        }

        private AttackDirectionHint ResolveAttackDirectionHint(AttackHitInfo hit)
        {
            if (hit.Attack.HasValue)
            {
                AttackData attack = hit.Attack.Value;
                if (attack.DirectionHint != AttackDirectionHint.None)
                {
                    return attack.DirectionHint;
                }

                if (comboDirectionResolver != null &&
                    comboDirectionResolver.TryResolve(attack.AttackId, out AttackDirectionHint fromResolver))
                {
                    return fromResolver;
                }

                if (stateMachine.AttackCombo != null &&
                    AttackComboDirectionResolver.TryResolveFromCombo(stateMachine.AttackCombo, attack.AttackId, out AttackDirectionHint fromCombo))
                {
                    return fromCombo;
                }
            }

            return ResolveAttackDirectionFromGeometry(hit);
        }

        private AttackDirectionHint ResolveAttackDirectionFromGeometry(AttackHitInfo hit)
        {
            bool isRight = false;

            if (hit.Attacker is Component attackerComponent)
            {
                Vector3 localAttacker = transform.InverseTransformPoint(attackerComponent.transform.position);
                isRight = localAttacker.x >= 0f;
            }

            bool isUp = true;
            if (hit.HitPoint != Vector3.zero)
            {
                Vector3 localHit = transform.InverseTransformPoint(hit.HitPoint);
                isUp = localHit.y >= 1f;
            }

            if (isRight)
            {
                return isUp ? AttackDirectionHint.RightUp : AttackDirectionHint.RightDown;
            }

            return isUp ? AttackDirectionHint.LeftUp : AttackDirectionHint.LeftDown;
        }

        private static bool TryGetParryState(AttackDirectionHint hint, out GuardSide guardSide, out string stateName)
        {
            switch (hint)
            {
                case AttackDirectionHint.LeftUp:
                    guardSide = GuardSide.Left;
                    stateName = "ParryLUp";
                    return true;
                case AttackDirectionHint.LeftDown:
                    guardSide = GuardSide.Left;
                    stateName = "ParryLDown";
                    return true;
                case AttackDirectionHint.RightUp:
                    guardSide = GuardSide.Right;
                    stateName = "ParryRUp";
                    return true;
                case AttackDirectionHint.RightDown:
                    guardSide = GuardSide.Right;
                    stateName = "ParryRDown";
                    return true;
                default:
                    guardSide = GuardSide.Left;
                    stateName = string.Empty;
                    return false;
            }
        }

        private void BeginReaction(string reactionStateName)
        {
            if (!CrossFade(reactionStateName, reactionCrossFadeDuration))
            {
                return;
            }

            stateMachine.BeginDefenseReaction(attackUnlockDelayAfterDefenseSeconds);
            reactionActive = true;
            activeReactionStateName = reactionStateName;
            activeReactionStateHash = Animator.StringToHash(reactionStateName);
            reactionTimeoutAt = Time.time + reactionTimeoutSeconds;
        }

        private void EndReaction()
        {
            reactionActive = false;
            stateMachine?.EndDefenseReaction();

            if (stateMachine == null || input == null || !input.IsBlocking)
            {
                return;
            }

            if (stateMachine.CurrentState is not global::Player.StateMachine.BlockingState)
            {
                return;
            }

            CrossFade(stateMachine.GetDefenseIdleStateName(), returnCrossFadeDuration);
        }

        private void ClearReactionState()
        {
            reactionActive = false;
            activeReactionStateName = null;
            activeReactionStateHash = 0;
            stateMachine?.EndDefenseReaction();
        }

        private void ResolveReferences()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            if (stateMachine == null)
            {
                stateMachine = GetComponent<PlayerStateMachine>();
            }

            if (input == null)
            {
                input = GetComponent<PlayerInputHandler>();
            }

            if (comboDirectionResolver == null)
            {
                comboDirectionResolver = GetComponent<AttackComboDirectionResolver>();
            }
        }

        private bool CrossFade(string stateName, float duration, int layer = 0)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(stateName))
            {
                return false;
            }

            if (layer < 0 || layer >= animator.layerCount)
            {
                layer = 0;
            }

            if (TryCrossFadeWithSubStatePaths(stateName, duration, layer))
            {
                return true;
            }

            int stateHash = Animator.StringToHash(stateName);
            if (animator.HasState(layer, stateHash))
            {
                animator.CrossFadeInFixedTime(stateHash, duration, layer);
                return true;
            }

            string layerName = animator.GetLayerName(layer);
            string fullPath = $"{layerName}.{stateName}";
            int fullPathHash = Animator.StringToHash(fullPath);
            if (animator.HasState(layer, fullPathHash))
            {
                animator.CrossFadeInFixedTime(fullPathHash, duration, layer);
                return true;
            }

            int logKey = (animator.GetInstanceID() * 397) ^ stateName.GetHashCode();
            if (!LoggedMissingStates.Contains(logKey))
            {
                LoggedMissingStates.Add(logKey);
                Debug.LogWarning($"[PlayerDefenseReactionAnimationDriver] State '{stateName}' not found in animator.", animator);
            }

            return false;
        }

        private bool TryCrossFadeWithSubStatePaths(string stateName, float duration, int layer)
        {
            string layerName = animator.GetLayerName(layer);
            bool isEquipped = stateMachine != null && stateMachine.IsEquipped;

            string[] preferredPaths = isEquipped
                ? new[]
                {
                    $"{layerName}.Grounded.Equip Locomotion.{stateName}",
                    $"{layerName}.Airborne.Equip Jump.{stateName}",
                    $"{layerName}.Grounded.Unequip Locomotion.{stateName}",
                    $"{layerName}.Airborne.Unequip Jump.{stateName}",
                }
                : new[]
                {
                    $"{layerName}.Grounded.Unequip Locomotion.{stateName}",
                    $"{layerName}.Airborne.Unequip Jump.{stateName}",
                    $"{layerName}.Grounded.Equip Locomotion.{stateName}",
                    $"{layerName}.Airborne.Equip Jump.{stateName}",
                };

            for (int i = 0; i < preferredPaths.Length; i++)
            {
                string path = preferredPaths[i];
                int pathHash = Animator.StringToHash(path);
                if (!animator.HasState(layer, pathHash))
                {
                    continue;
                }

                animator.CrossFadeInFixedTime(pathHash, duration, layer);
                return true;
            }

            return false;
        }
    }
}
