namespace Enemies.StateMachine.States
{
    using global::StateMachine.Core;
    using UnityEngine;

    public sealed class EnemyDefenseTurnState : EnemyStateBase
    {
        private int requiredParries;
        private int successfulParries;
        private float nextParryAttemptAt;
        private float counterReadyAt;
        private bool counterQueued;

        public override void OnEnter()
        {
            Intent?.ClearAllIntents();

            requiredParries = Owner != null ? Owner.SampleParriesBeforeCounter() : 1;
            successfulParries = 0;
            counterQueued = false;
            counterReadyAt = float.PositiveInfinity;
            nextParryAttemptAt = Time.time;

            if (Enemy != null)
            {
                Enemy.OnParriedAttack += HandleParriedAttack;
                Enemy.CloseParryWindow();
            }

            Owner?.TryCrossFadeState("DefenseIdle", 0.1f);
        }

        public override void OnUpdate()
        {
            FaceTarget();
            UpdateMovementMode();

            if (Enemy == null || Owner == null || !Owner.HasTarget || counterQueued)
            {
                return;
            }

            if (Time.time < nextParryAttemptAt)
            {
                return;
            }

            if (!Owner.IsTargetAttacking)
            {
                return;
            }

            if (Owner.DistanceToTarget > Owner.EngageRange)
            {
                return;
            }

            float parryWindow = Profile != null ? Profile.ParryWindowDuration : 0.2f;
            float cooldown = Profile != null ? Profile.ParryAttemptCooldown : 0.35f;

            Enemy.OpenParryWindow(parryWindow);
            nextParryAttemptAt = Time.time + cooldown;
        }

        public override void OnExit()
        {
            Owner?.NavBridge?.Stop();

            if (Enemy != null)
            {
                Enemy.OnParriedAttack -= HandleParriedAttack;
                Enemy.CloseParryWindow();
            }
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

            if (counterQueued && Time.time >= counterReadyAt)
            {
                return TransitionDecision.To(
                    Owner.GetState<EnemyAttackTurnState>(),
                    TransitionReason.AttackCombo,
                    priority: TransitionPriorities.ComboContinuation);
            }

            return TransitionDecision.None;
        }

        private void HandleParriedAttack(global::Combat.AttackHitInfo hit)
        {
            successfulParries++;
            if (successfulParries < requiredParries)
            {
                return;
            }

            counterQueued = true;
            float prepDelay = Profile != null ? Profile.CounterPrepDelay : 0f;
            counterReadyAt = Time.time + Mathf.Max(0f, prepDelay);
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

            float orbitRadius = Profile != null ? Profile.OrbitRadius : 2.75f;
            if (Owner.DistanceToTarget > Owner.EngageRange)
            {
                Owner.NavBridge.SetPursue(Owner.CurrentTarget, Owner.AttackRange);
            }
            else
            {
                Owner.NavBridge.SetOrbit(Owner.CurrentTarget, orbitRadius);
            }
        }
    }
}
