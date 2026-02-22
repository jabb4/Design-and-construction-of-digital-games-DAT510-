namespace Enemies.StateMachine.States
{
    using Enemies.Combat;
    using global::StateMachine.Core;
    using UnityEngine;

    public sealed class EnemyDefenseTurnState : EnemyStateBase
    {
        private int requiredParries;
        private int successfulParries;
        private float nextParryAttemptAt;
        private float counterReadyAt;
        private float defenseUntilAt;
        private float lastTargetAttackSeenAt;
        private EnemyDefenseReactionAnimationDriver defenseReactionDriver;
        private bool counterQueued;

        public override void OnEnter()
        {
            Intent?.ClearAllIntents();

            requiredParries = Owner != null ? Owner.SampleParriesBeforeCounter() : 1;
            successfulParries = 0;
            counterQueued = false;
            counterReadyAt = float.PositiveInfinity;
            nextParryAttemptAt = Time.time;
            float sampledDefenseDuration = Owner != null ? Owner.SampleDefenseDurationSeconds() : 1.2f;
            defenseUntilAt = Time.time + sampledDefenseDuration;
            lastTargetAttackSeenAt = float.NegativeInfinity;

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

            if (Enemy == null || Owner == null || !Owner.HasTarget || counterQueued)
            {
                return;
            }

            if (Owner.IsTargetAttacking)
            {
                lastTargetAttackSeenAt = Time.time;
            }

            if (Time.time < nextParryAttemptAt)
            {
                return;
            }

            float cooldown = Profile != null ? Profile.ParryAttemptCooldown : 0.35f;
            float configuredParryThreatMemory = Profile != null ? Profile.ParryThreatMemoryDuration : 0.15f;
            float parryThreatMemory = Mathf.Max(configuredParryThreatMemory, cooldown + 0.05f);
            bool isAttackThreatActive = Owner.IsTargetAttacking || Time.time - lastTargetAttackSeenAt <= parryThreatMemory;
            if (!isAttackThreatActive)
            {
                return;
            }

            float orbitRadius = Profile != null ? Profile.OrbitRadius : 2.75f;
            float configuredParryTriggerRange = Profile != null ? Profile.ParryTriggerRange : Mathf.Max(Owner.AttackRange, 3f);
            float parryTriggerRange = Mathf.Max(configuredParryTriggerRange, orbitRadius + 0.1f);
            if (Owner.DistanceToTarget > parryTriggerRange)
            {
                return;
            }

            float parryWindow = Profile != null ? Profile.ParryWindowDuration : 0.2f;

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
            bool parryWindowActive = Enemy != null && Enemy.IsParryWindowActive;
            if (reactionActive || parryWindowActive)
            {
                Owner.NavBridge.Stop();
                if (!reactionActive)
                {
                    Owner.TryCrossFadeStateIfNotActive("DefenseIdle", 0.08f);
                }

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
    }
}
