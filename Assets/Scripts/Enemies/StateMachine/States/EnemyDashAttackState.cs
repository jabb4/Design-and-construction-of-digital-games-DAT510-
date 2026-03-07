namespace Enemies.StateMachine.States
{
    using Enemies.AI;
    using global::Combat;
    using global::StateMachine.Core;
    using UnityEngine;

    public sealed class EnemyDashAttackState : EnemyStateBase
    {
        private bool hasAttackToken;
        private bool dashStarted;
        private bool attackFired;
        private float stateEnteredAt;
        private CombatHorizontalImpulseDriver impulseDriver;

        public float NextAllowedAt { get; private set; }

        public override void OnEnter()
        {
            Intent?.ClearAllIntents();
            Enemy?.CloseParryWindow();

            hasAttackToken = Owner != null && EnemyAttackTokenService.TryAcquire(Owner);
            dashStarted = false;
            attackFired = false;
            stateEnteredAt = Time.time;

            impulseDriver = Owner != null
                ? Owner.GetComponent<CombatHorizontalImpulseDriver>()
                : null;

            if (!hasAttackToken || Owner == null || !Owner.HasTarget)
            {
                return;
            }

            FaceTarget(99999f);
            StartDash();
        }

        public override void OnUpdate()
        {
            if (Owner == null) return;

            Enemy?.CloseParryWindow();
            FaceTarget();

            if (!dashStarted) return;

            if (impulseDriver != null && !impulseDriver.IsImpulseActive && !attackFired)
            {
                FireAttack();
            }
        }

        public override void OnExit()
        {
            if (Owner != null)
            {
                Owner.NavBridge?.Stop();
                Owner.ClearCurrentAttack();

                if (hasAttackToken)
                {
                    float cooldown = Owner.ComputeAttackTokenReleaseCooldownForCurrentGroup();
                    float reentryDelay = Profile != null ? Profile.SameAttackerReentryDelay : 0f;
                    EnemyAttackTokenService.Release(Owner, cooldown, reentryDelay);
                }
                else
                {
                    EnemyAttackTokenService.Release(Owner);
                }
            }

            impulseDriver?.StopActiveImpulse();
            hasAttackToken = false;

            float cooldownDuration = Profile != null ? Profile.DashAttackCooldown : 3f;
            NextAllowedAt = Time.time + cooldownDuration;
        }

        public override TransitionDecision EvaluateTransition()
        {
            if (TryTransitionDead(out TransitionDecision deadTransition))
            {
                return deadTransition;
            }

            if (!Owner.TryRefreshTarget())
            {
                return TransitionDecision.To(
                    Owner.GetState<EnemyIdleState>(),
                    TransitionReason.StandardFlow);
            }

            if (!hasAttackToken)
            {
                return TransitionDecision.To(
                    Owner.GetState<EnemyDefenseTurnState>(),
                    TransitionReason.StandardFlow);
            }

            if (attackFired && IsAttackAnimationComplete())
            {
                return TransitionDecision.To(
                    Owner.GetState<EnemyDefenseTurnState>(),
                    TransitionReason.AttackCombo);
            }

            // Safety timeout — don't get stuck
            if (Time.time - stateEnteredAt > 3f)
            {
                return TransitionDecision.To(
                    Owner.GetState<EnemyDefenseTurnState>(),
                    TransitionReason.StandardFlow);
            }

            return TransitionDecision.None;
        }

        private void StartDash()
        {
            if (Owner == null || Owner.CurrentTarget == null || impulseDriver == null) return;

            Vector3 toTarget = Owner.CurrentTarget.position - Owner.transform.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;
            float duration = Profile != null ? Profile.DashAttackDuration : 0.3f;

            impulseDriver.PlayImpulse(toTarget.normalized, distance, duration, 0.1f);
            dashStarted = true;

            Owner.NavBridge?.Stop();

            Owner.TryPlayAttackStepByIndex(0, 0.06f);
        }

        private void FireAttack()
        {
            attackFired = true;
        }

        private bool IsAttackAnimationComplete()
        {
            if (Owner == null) return true;

            return Owner.IsCurrentAttackAnimationComplete(0.95f);
        }
    }
}
