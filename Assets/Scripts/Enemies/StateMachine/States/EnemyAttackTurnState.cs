namespace Enemies.StateMachine.States
{
    using Enemies.AI;
    using global::StateMachine.Core;
    using UnityEngine;

    public sealed class EnemyAttackTurnState : EnemyStateBase
    {
        private const float InterAttackDelay = 0.08f;
        private const float FinalAttackCompleteThreshold = 0.995f;

        private bool hasAttackToken;
        private int plannedChainLength;
        private int currentAttackIndex;
        private bool attackInProgress;
        private bool chainComplete;
        private bool waitingForFinalAttackCompletion;
        private int observedRecoveryVersion;
        private float nextAttackStartAt;

        public int PlannedChainLength => plannedChainLength;

        public override void OnEnter()
        {
            Intent?.ClearAllIntents();
            Enemy?.CloseParryWindow();

            hasAttackToken = Owner != null && EnemyAttackTokenService.TryAcquire(Owner);
            plannedChainLength = Owner != null ? Owner.SampleAttackChainLength() : 1;
            currentAttackIndex = 0;
            attackInProgress = false;
            chainComplete = plannedChainLength <= 0;
            waitingForFinalAttackCompletion = false;
            observedRecoveryVersion = Owner != null ? Owner.AttackRecoveryVersion : 0;
            nextAttackStartAt = Time.time;

            Owner?.TryCrossFadeState("Idle", 0.08f);
        }

        public override void OnUpdate()
        {
            if (Owner == null)
            {
                return;
            }

            FaceTarget();
            UpdateMovementMode();

            if (!hasAttackToken)
            {
                hasAttackToken = EnemyAttackTokenService.TryAcquire(Owner);
                return;
            }

            if (chainComplete)
            {
                return;
            }

            if (!Owner.HasTarget)
            {
                return;
            }

            if (attackInProgress)
            {
                bool isFinalAttackInChain = IsFinalAttackIndex(currentAttackIndex);
                if (!waitingForFinalAttackCompletion &&
                    Owner.AttackRecoveryVersion != observedRecoveryVersion)
                {
                    observedRecoveryVersion = Owner.AttackRecoveryVersion;
                    if (isFinalAttackInChain)
                    {
                        // For the last hit, let the recovery tail visibly finish before locomotion resumes.
                        waitingForFinalAttackCompletion = true;
                    }
                    else
                    {
                        AdvanceAttackStep();
                    }

                    return;
                }

                bool isAttackStepComplete = waitingForFinalAttackCompletion
                    ? Owner.IsCurrentAttackAnimationComplete(FinalAttackCompleteThreshold) || HasExitedCurrentAttackAnimation()
                    : Owner.IsCurrentAttackAnimationComplete(0.98f);

                if (isAttackStepComplete)
                {
                    AdvanceAttackStep();
                }

                return;
            }

            if (Time.time < nextAttackStartAt)
            {
                return;
            }

            if (Owner.DistanceToTarget > Owner.AttackRange)
            {
                return;
            }

            if (!Owner.TryPlayAttackStepByIndex(currentAttackIndex))
            {
                chainComplete = true;
                return;
            }

            attackInProgress = true;
        }

        public override void OnExit()
        {
            if (Owner != null)
            {
                Owner.NavBridge?.Stop();
                Owner.ClearCurrentAttack();
                EnemyAttackTokenService.Release(Owner);
            }

            hasAttackToken = false;
        }

        public override TransitionDecision EvaluateTransition()
        {
            if (TryTransitionDead(out TransitionDecision deadTransition))
            {
                return deadTransition;
            }

            if (!Owner.TryRefreshTarget())
            {
                return TransitionDecision.To(Owner.GetState<EnemyIdleState>(), TransitionReason.StandardFlow);
            }

            if (chainComplete && !attackInProgress)
            {
                return TransitionDecision.To(Owner.GetState<EnemyDefenseTurnState>(), TransitionReason.AttackCombo);
            }

            return TransitionDecision.None;
        }

        private void AdvanceAttackStep()
        {
            waitingForFinalAttackCompletion = false;
            attackInProgress = false;
            currentAttackIndex++;
            nextAttackStartAt = Time.time + InterAttackDelay;

            if (currentAttackIndex >= plannedChainLength || currentAttackIndex >= Owner.AttackStepCount)
            {
                chainComplete = true;
            }
        }

        private void UpdateMovementMode()
        {
            if (Owner == null || Owner.NavBridge == null)
            {
                return;
            }

            if (chainComplete)
            {
                Owner.NavBridge.Stop();
                Owner.TryCrossFadeStateIfNotActive("Idle", 0.08f);
                return;
            }

            if (!Owner.HasTarget)
            {
                Owner.NavBridge.Stop();
                if (!attackInProgress)
                {
                    Owner.TryCrossFadeStateIfNotActive("Idle", 0.08f);
                }
                return;
            }

            // Do not let locomotion crossfades interrupt an active attack clip.
            if (attackInProgress)
            {
                Owner.NavBridge.Stop();
                return;
            }

            if (Owner.DistanceToTarget > Owner.AttackRange)
            {
                Owner.NavBridge.SetPursue(Owner.CurrentTarget, Owner.AttackRange * 0.85f);
                Owner.TryCrossFadeStateIfNotActive("Walk Locomotion", 0.1f);
            }
            else
            {
                Owner.NavBridge.Stop();
                Owner.TryCrossFadeStateIfNotActive("Idle", 0.08f);
            }
        }

        private bool IsFinalAttackIndex(int attackIndex)
        {
            if (Owner == null || Owner.AttackStepCount <= 0)
            {
                return true;
            }

            int lastPlannedIndex = Mathf.Max(0, plannedChainLength - 1);
            int lastComboIndex = Owner.AttackStepCount - 1;
            int finalPlayableIndex = Mathf.Min(lastPlannedIndex, lastComboIndex);
            return attackIndex >= finalPlayableIndex;
        }

        private bool HasExitedCurrentAttackAnimation()
        {
            if (Owner == null || Owner.Animator == null || !Owner.CurrentAttackStep.HasValue)
            {
                return true;
            }

            string stateName = Owner.CurrentAttackStep.Value.AnimationStateName;
            if (string.IsNullOrWhiteSpace(stateName))
            {
                return true;
            }

            Animator animator = Owner.Animator;
            int shortNameHash = Animator.StringToHash(stateName);

            AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
            if (current.shortNameHash == shortNameHash || current.IsName(stateName))
            {
                return false;
            }

            if (animator.IsInTransition(0))
            {
                AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(0);
                if (next.shortNameHash == shortNameHash || next.IsName(stateName))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
