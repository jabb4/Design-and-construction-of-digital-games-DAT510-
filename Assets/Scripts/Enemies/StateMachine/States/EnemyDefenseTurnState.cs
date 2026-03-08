namespace Enemies.StateMachine.States
{
    using Enemies.AI;
    using Enemies.Combat;
    using global::StateMachine.Core;
    using UnityEngine;

    public sealed class EnemyDefenseTurnState : EnemyStateBase
    {
        private const float FocusPriorityDurationSeconds = 1.25f;
        private const float SupportRingHysteresisSeconds = 0.5f;

        private int requiredParries;
        private int successfulParries;
        private float counterReadyAt;
        private float defenseUntilAt;
        private float parryExchangeExpiresAt;
        private float lastParryThreatAt;
        private EnemyDefenseReactionAnimationDriver defenseReactionDriver;
        private bool counterQueued;
        private bool hasPriorityToken;
        private bool handoffTokenToAttackTurn;
        private bool latchedSupportRing;
        private float supportRingLatchUntil;
        private float orbitRadiusJitter;

        public int RequiredParries => requiredParries;
        public float DefenseTimeRemainingSeconds => Mathf.Max(0f, defenseUntilAt - Time.time);
        public float? CounterPrepTimeRemainingSeconds => counterQueued
            ? Mathf.Max(0f, counterReadyAt - Time.time)
            : null;
        public float? ParryAttemptCooldownRemainingSeconds => null;

        public override void OnEnter()
        {
            Intent?.ClearAllIntents();

            requiredParries = Owner != null ? Owner.SampleParriesBeforeCounter() : 1;
            successfulParries = 0;
            counterQueued = false;
            counterReadyAt = float.PositiveInfinity;
            float sampledDefenseDuration = Owner != null ? Owner.SampleDefenseDurationSeconds() : 1.2f;
            defenseUntilAt = Time.time + sampledDefenseDuration;
            parryExchangeExpiresAt = float.NegativeInfinity;
            lastParryThreatAt = float.NegativeInfinity;
            hasPriorityToken = false;
            handoffTokenToAttackTurn = false;
            latchedSupportRing = false;
            supportRingLatchUntil = float.NegativeInfinity;
            orbitRadiusJitter = Random.Range(-0.35f, 0.35f);
            if (Enemy != null)
            {
                Enemy.OnParriedAttack += HandleParriedAttack;
                Enemy.OnDamageResolved += HandleDamageResolved;
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
            ResetPartialParryExchangeIfExpired();
            MaintainPriorityToken();
            UpdateMovementMode();
            MaintainThreatDrivenParryWindow();
        }

        public override void OnExit()
        {
            Owner?.NavBridge?.Stop();

            if (hasPriorityToken && !handoffTokenToAttackTurn)
            {
                EnemyAttackTokenService.Release(Owner);
            }

            hasPriorityToken = false;
            handoffTokenToAttackTurn = false;

            if (Enemy != null)
            {
                Enemy.OnParriedAttack -= HandleParriedAttack;
                Enemy.OnDamageResolved -= HandleDamageResolved;
                Enemy.CloseParryWindow();
            }
        }

        public override TransitionDecision EvaluateTransition()
        {
            ResetPartialParryExchangeIfExpired();

            if (TryTransitionDead(out TransitionDecision deadTransition))
            {
                return deadTransition;
            }

            if (!Owner.TryRefreshTarget())
            {
                return TransitionDecision.To(Owner.GetState<EnemyIdleState>(), TransitionReason.StandardFlow);
            }

            if (Owner.IsTargetingRagdoll)
            {
                return TransitionDecision.None;
            }

            if (counterQueued)
            {
                if (IsCounterResponseReady())
                {
                    if (!EnsurePriorityToken())
                    {
                        return TransitionDecision.None;
                    }

                    handoffTokenToAttackTurn = true;
                    return TransitionDecision.To(
                        Owner.GetState<EnemyAttackTurnState>(),
                        TransitionReason.AttackCombo,
                        priority: TransitionPriorities.ComboContinuation);
                }

                return TransitionDecision.None;
            }

            if (IsParrySequenceInProgress() || IsDefenseReactionActive())
            {
                return TransitionDecision.None;
            }

            if (ShouldDashAttack())
            {
                handoffTokenToAttackTurn = true;
                return TransitionDecision.To(
                    Owner.GetState<EnemyDashAttackState>(),
                    TransitionReason.AttackCombo,
                    priority: TransitionPriorities.ComboContinuation);
            }

            if (Time.time >= defenseUntilAt)
            {
                bool isFocusPriorityOwner = EnemyAttackTokenService.IsPriorityOwner(Owner);
                if (!isFocusPriorityOwner && !Owner.IsClosestEnemyToCurrentTarget(requireTokenEligibility: true))
                {
                    return TransitionDecision.None;
                }

                if (!EnemyAttackTokenService.CanAcquire(Owner))
                {
                    return TransitionDecision.None;
                }

                handoffTokenToAttackTurn = true;
                return TransitionDecision.To(
                    Owner.GetState<EnemyAttackTurnState>(),
                    TransitionReason.StandardFlow,
                    priority: TransitionPriorities.Default);
            }

            return TransitionDecision.None;
        }

        private void HandleParriedAttack(global::Combat.AttackHitInfo hit)
        {
            if (counterQueued)
            {
                return;
            }

            if (hit.Attacker != null && hit.Attacker.Team == global::Combat.CombatTeam.Player)
            {
                EnemyAttackTokenService.SetPriorityOwner(Owner, FocusPriorityDurationSeconds);
            }

            successfulParries++;
            EnsurePriorityToken();
            if (successfulParries < requiredParries)
            {
                RefreshParryExchangeTimeout();
                return;
            }

            Enemy?.QueueEndParryOutcomeFeedback();
            counterQueued = true;
            parryExchangeExpiresAt = float.PositiveInfinity;
            float prepDelay = Profile != null ? Profile.CounterPrepDelay : 0f;
            counterReadyAt = Time.time + Mathf.Max(0f, prepDelay);
        }

        private void HandleDamageResolved(global::Combat.AttackHitInfo hit, global::Combat.DamageResolution resolution)
        {
            if (Owner == null || hit.Attacker == null || hit.Attacker.Team != global::Combat.CombatTeam.Player)
            {
                return;
            }

            if (resolution.Outcome != global::Combat.DamageOutcome.Parried)
            {
                return;
            }

            EnemyAttackTokenService.SetPriorityOwner(Owner, FocusPriorityDurationSeconds);
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

            int nearbyEnemyCount = Owner.GetNearbyEnemyCount(Profile != null ? Profile.GroupAwarenessRadius : 8f);
            bool isFrontliner = EnemyAttackTokenService.IsHolder(Owner) || EnemyAttackTokenService.IsPriorityOwner(Owner);
            bool ongoingFight = !isFrontliner && EnemyAttackTokenService.IsHeldByOther(Owner);
            bool wantsSupportRing = (nearbyEnemyCount > 1 || ongoingFight) && !isFrontliner;

            if (wantsSupportRing && !latchedSupportRing)
            {
                latchedSupportRing = true;
                supportRingLatchUntil = Time.time + SupportRingHysteresisSeconds;
            }
            else if (!wantsSupportRing && latchedSupportRing && Time.time >= supportRingLatchUntil)
            {
                latchedSupportRing = false;
            }

            bool useSupportRing = latchedSupportRing;
            float orbitRadius = useSupportRing
                ? Owner.ComputeSupportOrbitRadiusForCurrentGroup()
                : (Profile != null ? Profile.OrbitRadius : 2.75f);

            if (useSupportRing && TryGetCurrentFrontliner(out EnemyStateMachine frontliner) && frontliner != null && frontliner != Owner)
            {
                float frontlinerDistanceToTarget = frontliner.DistanceToTarget;
                float minSeparationFromFrontliner = Profile != null ? Profile.SupportDistanceFromPriorityEnemy : 2.25f;
                orbitRadius = Mathf.Max(orbitRadius, frontlinerDistanceToTarget + Mathf.Max(0f, minSeparationFromFrontliner));
            }

            orbitRadius += orbitRadiusJitter;

            float engageRange = Mathf.Max(Owner.EngageRange, orbitRadius + 0.5f);
            float pursueStopDistance = useSupportRing ? Mathf.Max(Owner.AttackRange, orbitRadius) : Owner.AttackRange;

            if (Owner.DistanceToTarget > engageRange)
            {
                Owner.NavBridge.SetPursue(Owner.CurrentTarget, pursueStopDistance);
                Owner.TryCrossFadeStateIfNotActive("Walk Locomotion", 0.1f);
            }
            else
            {
                Owner.NavBridge.SetOrbit(Owner.CurrentTarget, orbitRadius);
                Owner.TryCrossFadeStateIfNotActive("Walk Locomotion", 0.1f);
            }
        }

        private void MaintainThreatDrivenParryWindow()
        {
            if (Enemy == null || Owner == null || !Owner.HasTarget)
            {
                Enemy?.CloseParryWindow();
                return;
            }

            if (counterQueued)
            {
                Enemy.CloseParryWindow();
                return;
            }

            bool underThreat = Owner.IsTargetAttacking &&
                               Owner.DistanceToTarget <= (Profile != null ? Profile.ParryTriggerRange : 3f);
            if (underThreat)
            {
                lastParryThreatAt = Time.time;
            }

            float threatMemory = Profile != null ? Profile.ParryThreatMemoryDuration : 0.15f;
            bool hasRecentThreat = Time.time <= lastParryThreatAt + Mathf.Max(0f, threatMemory);
            if (!hasRecentThreat)
            {
                Enemy.CloseParryWindow();
                return;
            }

            float configuredWindow = Profile != null ? Profile.ParryWindowDuration : 0.2f;
            float minContinuousWindow = Mathf.Max(Time.deltaTime * 2f, 0.05f);
            float parryWindow = Mathf.Max(configuredWindow, minContinuousWindow);
            Enemy.OpenParryWindow(parryWindow);
        }

        private bool IsCounterResponseReady()
        {
            return Time.time >= counterReadyAt;
        }

        private bool IsParrySequenceInProgress()
        {
            return successfulParries > 0 && successfulParries < requiredParries;
        }

        private bool IsDefenseReactionActive()
        {
            return defenseReactionDriver != null && defenseReactionDriver.IsReactionActive;
        }

        private void MaintainPriorityToken()
        {
            if (Owner == null)
            {
                return;
            }

            if (hasPriorityToken && !EnemyAttackTokenService.IsHolder(Owner))
            {
                hasPriorityToken = false;
            }

            bool shouldHoldPriority = IsParrySequenceInProgress() || counterQueued;
            if (!shouldHoldPriority)
            {
                if (hasPriorityToken && EnemyAttackTokenService.IsHolder(Owner) && !handoffTokenToAttackTurn)
                {
                    EnemyAttackTokenService.Release(Owner);
                }

                hasPriorityToken = false;
                return;
            }

            EnsurePriorityToken();
        }

        private bool EnsurePriorityToken()
        {
            if (Owner == null)
            {
                return false;
            }

            if (EnemyAttackTokenService.IsHolder(Owner))
            {
                hasPriorityToken = true;
                return true;
            }

            if (!EnemyAttackTokenService.TryAcquire(Owner))
            {
                return false;
            }

            hasPriorityToken = true;
            return true;
        }

        private void RefreshParryExchangeTimeout()
        {
            float refreshSeconds = Profile != null ? Profile.ParryExchangeRefreshSeconds : 2f;
            parryExchangeExpiresAt = Time.time + Mathf.Max(0.1f, refreshSeconds);
        }

        private void ResetPartialParryExchangeIfExpired()
        {
            if (!IsParrySequenceInProgress())
            {
                return;
            }

            if (Time.time <= parryExchangeExpiresAt)
            {
                return;
            }

            successfulParries = 0;
            parryExchangeExpiresAt = float.NegativeInfinity;

            if (hasPriorityToken && EnemyAttackTokenService.IsHolder(Owner) && !handoffTokenToAttackTurn)
            {
                EnemyAttackTokenService.Release(Owner);
            }

            hasPriorityToken = false;
        }

        private bool ShouldDashAttack()
        {
            if (Owner == null || Profile == null || !Owner.HasTarget) return false;
            if (Owner.Tier != EnemyTier.Boss) return false;
            if (!Profile.DashAttackEnabled) return false;
            EnemyDashAttackState dashState = Owner.GetState<EnemyDashAttackState>();
            if (dashState != null && Time.time < dashState.NextAllowedAt) return false;
            if (!EnemyAttackTokenService.CanAcquire(Owner)) return false;

            float distance = Owner.DistanceToTarget;
            if (distance < Profile.DashAttackMinRange || distance > Profile.DashAttackMaxRange) return false;

            return Owner.HasClearDashPath;
        }

        private bool TryGetCurrentFrontliner(out EnemyStateMachine frontliner)
        {
            if (EnemyAttackTokenService.TryGetPriorityOwner(out frontliner))
            {
                if (frontliner != null && frontliner.HasTarget && Owner != null && frontliner.CurrentTarget == Owner.CurrentTarget)
                {
                    return true;
                }
            }

            if (EnemyAttackTokenService.TryGetTokenHolder(out frontliner))
            {
                if (frontliner != null && frontliner.HasTarget && Owner != null && frontliner.CurrentTarget == Owner.CurrentTarget)
                {
                    return true;
                }
            }

            frontliner = null;
            return false;
        }
    }
}
