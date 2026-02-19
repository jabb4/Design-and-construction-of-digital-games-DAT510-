namespace Enemies.StateMachine.States
{
    using Enemies.AI;
    using global::StateMachine.Core;
    using UnityEngine;

    public sealed class EnemyAttackTurnState : EnemyStateBase
    {
        private const float InterAttackDelay = 0.08f;

        private bool hasAttackToken;
        private int plannedChainLength;
        private int currentAttackIndex;
        private bool attackInProgress;
        private bool chainComplete;
        private int observedRecoveryVersion;
        private float nextAttackStartAt;

        public override void OnEnter()
        {
            Intent?.ClearAllIntents();
            Enemy?.CloseParryWindow();

            hasAttackToken = Owner != null && EnemyAttackTokenService.TryAcquire(Owner);
            plannedChainLength = Owner != null ? Owner.SampleAttackChainLength() : 1;
            currentAttackIndex = 0;
            attackInProgress = false;
            chainComplete = plannedChainLength <= 0;
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
                if (Owner.AttackRecoveryVersion != observedRecoveryVersion)
                {
                    observedRecoveryVersion = Owner.AttackRecoveryVersion;
                    AdvanceAttackStep();
                    return;
                }

                if (Owner.IsCurrentAttackAnimationComplete())
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

            if (!Owner.HasTarget)
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
    }
}
