namespace Enemies.StateMachine.States
{
    using Enemies.Combat;
    using global::StateMachine.Core;
    using UnityEngine;

    public sealed class EnemyDefenseTurnState : EnemyStateBase
    {
        private int requiredParries;
        private int successfulParries;
        private float counterReadyAt;
        private float defenseUntilAt;
        private EnemyDefenseReactionAnimationDriver defenseReactionDriver;
        private bool counterQueued;

        public int RequiredParries => requiredParries;

        public override void OnEnter()
        {
            Intent?.ClearAllIntents();

            requiredParries = Owner != null ? Owner.SampleParriesBeforeCounter() : 1;
            successfulParries = 0;
            counterQueued = false;
            counterReadyAt = float.PositiveInfinity;
            float sampledDefenseDuration = Owner != null ? Owner.SampleDefenseDurationSeconds() : 1.2f;
            defenseUntilAt = Time.time + sampledDefenseDuration;

            if (Enemy != null)
            {
                Enemy.OnParriedAttack += HandleParriedAttack;
                Enemy.CloseParryWindow();
            }

            defenseReactionDriver = Owner != null
                ? Owner.GetComponent<EnemyDefenseReactionAnimationDriver>()
                : null;

            Owner?.TryCrossFadeState("Idle", 0.1f);
        }

        public override void OnUpdate()
        {
            FaceTarget();
            UpdateMovementMode();
            MaintainAlwaysReadyParryWindow();
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

            if (Time.time >= defenseUntilAt)
            {
                return TransitionDecision.To(
                    Owner.GetState<EnemyAttackTurnState>(),
                    TransitionReason.StandardFlow,
                    priority: TransitionPriorities.Default);
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

            Enemy?.QueueEndParryOutcomeFeedback();
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

            bool reactionActive = defenseReactionDriver != null && defenseReactionDriver.IsReactionActive;
            if (reactionActive)
            {
                Owner.NavBridge.Stop();
                return;
            }

            float orbitRadius = Profile != null ? Profile.OrbitRadius : 2.75f;
            if (Owner.DistanceToTarget > Owner.EngageRange)
            {
                Owner.NavBridge.SetPursue(Owner.CurrentTarget, Owner.AttackRange);
                Owner.TryCrossFadeStateIfNotActive("Walk Locomotion", 0.1f);
            }
            else
            {
                Owner.NavBridge.SetOrbit(Owner.CurrentTarget, orbitRadius);
                Owner.TryCrossFadeStateIfNotActive("Walk Locomotion", 0.1f);
            }
        }

        private void MaintainAlwaysReadyParryWindow()
        {
            if (Enemy == null || Owner == null || !Owner.HasTarget)
            {
                Enemy?.CloseParryWindow();
                return;
            }

            // Defensive turn is intentionally always parry-ready.
            float configuredWindow = Profile != null ? Profile.ParryWindowDuration : 0.2f;
            float minContinuousWindow = Mathf.Max(Time.deltaTime * 2f, 0.05f);
            float parryWindow = Mathf.Max(configuredWindow, minContinuousWindow);
            Enemy.OpenParryWindow(parryWindow);
        }
    }
}
