namespace Player.StateMachine.States
{
    using Player.StateMachine;
    using Player.StateMachine.Transitions;
    using global::StateMachine.Core;
    using UnityEngine;

    public sealed class AttackState : PlayerStateBase, IAttackPhaseListener
    {
        private const float RecoveryMoveDelay = 0.5f;
        private int comboIndex;
        private bool queuedNextAttack;
        private AttackPhase currentPhase;
        private bool hasEnteredRecovery;
        private bool hasPhaseEvents;
        private bool hasLoggedMissingEventWarning;
        private bool hasPlayedAttackAnimation;
        private float recoveryElapsed;
        private AttackAnimationAdapter animationAdapter;

        public AttackPhase CurrentPhase => currentPhase;

        public void SetComboIndex(int index)
        {
            int maxIndex = Mathf.Max(Owner.AttackStepCount - 1, 0);
            comboIndex = Mathf.Clamp(index, 0, maxIndex);
        }

        public override void OnEnter()
        {
            queuedNextAttack = false;
            currentPhase = AttackPhase.Windup;
            hasEnteredRecovery = false;
            hasPhaseEvents = false;
            hasLoggedMissingEventWarning = false;
            hasPlayedAttackAnimation = false;
            recoveryElapsed = 0f;

            if (!TryGetCurrentAttackStep(out AttackStep step))
            {
                Owner.ChangeState(Owner.GetState<IdleState>());
                return;
            }
            Owner.SetCurrentAttack(step);

            Animator.SetBool(IsMovingHash, false);
            Animator.SetBool(IsSprintingHash, false);

            EnsureAnimationAdapter().Play(step, 0.1f);
        }

        public override void OnUpdate()
        {
            if (!TryGetCurrentAttackStep(out AttackStep step))
            {
                return;
            }

            WarnIfMissingAttackEvents(step);

            if (hasEnteredRecovery)
            {
                recoveryElapsed += Time.deltaTime;
            }
        }

        public override void OnFixedUpdate()
        {
            if (currentPhase == AttackPhase.Windup)
            {
                if (Motor.IsLockedOn || HasMoveIntent)
                {
                    RotateWithContext(requireMovementInput: true);
                }
            }

            Motor.Move(Vector2.zero, useSprint: false);
        }

        public override TransitionDecision EvaluateTransition()
        {
            if (!TryGetCurrentAttackStep(out AttackStep step))
            {
                return TransitionDecision.To(Owner.GetState<IdleState>(), TransitionReason.MissingData, priority: TransitionPriorities.CriticalFallback);
            }

            bool isInAttackState = EnsureAnimationAdapter().IsPlaying(step);
            RegisterAttackPresence(isInAttackState);
            BufferComboInputIfNeeded(isInAttackState);

            TransitionDecision recoveryTransition = TryGetRecoveryTransition();
            if (recoveryTransition.HasTransition)
            {
                return recoveryTransition;
            }

            TransitionDecision comboTransition = TryGetComboTransition();
            if (comboTransition.HasTransition)
            {
                return comboTransition;
            }

            TransitionDecision exitTransition = TryGetAttackExitTransition(isInAttackState);
            return exitTransition;
        }

        public override void OnExit()
        {
            queuedNextAttack = false;
            recoveryElapsed = 0f;
        }

        public bool OnAttackPhase(AttackPhase phase)
        {
            if (!TryGetCurrentAttackStep(out AttackStep step))
            {
                return false;
            }

            if (!EnsureAnimationAdapter().IsPlaying(step))
            {
                return false;
            }

            if (phase < currentPhase)
            {
                return false;
            }

            hasPhaseEvents = true;
            currentPhase = phase;
            if (phase == AttackPhase.Recovery)
            {
                hasEnteredRecovery = true;
                recoveryElapsed = 0f;
            }

            return true;
        }

        private bool TryGetCurrentAttackStep(out AttackStep step)
        {
            if (Owner.TryGetAttackStep(comboIndex, out step))
            {
                return true;
            }

            step = default;
            return false;
        }

        private void RegisterAttackPresence(bool isInAttackState)
        {
            if (isInAttackState)
            {
                hasPlayedAttackAnimation = true;
            }
        }

        private void BufferComboInputIfNeeded(bool isInAttackState)
        {
            if (hasEnteredRecovery && isInAttackState && AttackPressed)
            {
                queuedNextAttack = true;
            }
        }

        private TransitionDecision TryGetRecoveryTransition()
        {
            if (!hasEnteredRecovery || !Motor.IsGrounded)
            {
                return TransitionDecision.None;
            }

            if (BlockHeld && Owner.IsEquipped)
            {
                Owner.ClearCurrentAttack();
                return TransitionDecision.To(Owner.GetState<BlockingState>(), TransitionReason.RecoveryInterrupt, priority: TransitionPriorities.RecoveryInterrupt);
            }

            if (recoveryElapsed < RecoveryMoveDelay)
            {
                return TransitionDecision.None;
            }

            if (JumpPressed)
            {
                Owner.ClearCurrentAttack();
                return TransitionDecision.To(Owner.GetState<JumpStartState>(), TransitionReason.InputJump, priority: TransitionPriorities.InputPrimary);
            }

            TransitionDecision moveTransition = GroundedTransitionEvaluator.ToLocomotion(Owner, HasMoveIntent, SprintHeld);
            if (moveTransition.HasTransition)
            {
                Owner.ClearCurrentAttack();
                return moveTransition;
            }

            return TransitionDecision.None;
        }

        private TransitionDecision TryGetComboTransition()
        {
            if (!hasEnteredRecovery || !queuedNextAttack || comboIndex >= Owner.AttackStepCount - 1)
            {
                return TransitionDecision.None;
            }

            var nextState = new AttackState();
            nextState.Initialize(Owner, Context);
            nextState.SetComboIndex(comboIndex + 1);
            return TransitionDecision.To(nextState, TransitionReason.AttackCombo, priority: TransitionPriorities.ComboContinuation);
        }

        private TransitionDecision TryGetAttackExitTransition(bool isInAttackState)
        {
            if (isInAttackState && EnsureAnimationAdapter().IsComplete(0.98f))
            {
                return ExitAttackToLocomotionOrIdle();
            }

            if (hasPlayedAttackAnimation && !isInAttackState)
            {
                return ExitAttackToLocomotionOrIdle();
            }

            return TransitionDecision.None;
        }

        private TransitionDecision ExitAttackToLocomotionOrIdle()
        {
            Owner.ClearCurrentAttack();
            return GroundedTransitionEvaluator.ToLocomotionOrIdle(
                Owner,
                HasMoveIntent,
                SprintHeld,
                moveReason: TransitionReason.InputMove,
                idleReason: TransitionReason.AnimationComplete);
        }

        private void WarnIfMissingAttackEvents(AttackStep step)
        {
            if (hasPhaseEvents || hasLoggedMissingEventWarning)
            {
                return;
            }

            if (!EnsureAnimationAdapter().IsPlaying(step))
            {
                return;
            }

            if (GetAnimatorNormalizedTime() < 0.98f)
            {
                return;
            }

            hasLoggedMissingEventWarning = true;
            Debug.LogWarning(
                $"[AttackState] No attack phase events received for '{EnsureAnimationAdapter().GetPresentationStateName(step)}'. " +
                "Add animation events that call OnAttackWindup, OnAttackSlash, and OnAttackRecovery.",
                Animator);
        }

        private AttackAnimationAdapter EnsureAnimationAdapter()
        {
            if (animationAdapter != null)
            {
                return animationAdapter;
            }

            animationAdapter = new AttackAnimationAdapter(
                (stateName, duration) => CrossFade(stateName, duration),
                stateName => IsAnimatorInState(stateName),
                threshold => IsAnimationComplete(threshold));

            return animationAdapter;
        }

        private sealed class AttackAnimationAdapter
        {
            private readonly System.Func<string, float, bool> playState;
            private readonly System.Func<string, bool> isInState;
            private readonly System.Func<float, bool> isComplete;

            public AttackAnimationAdapter(
                System.Func<string, float, bool> playState,
                System.Func<string, bool> isInState,
                System.Func<float, bool> isComplete)
            {
                this.playState = playState;
                this.isInState = isInState;
                this.isComplete = isComplete;
            }

            public void Play(AttackStep step, float blendDuration)
            {
                playState?.Invoke(step.AnimationStateName, blendDuration);
            }

            public bool IsPlaying(AttackStep step)
            {
                return isInState != null && isInState(step.AnimationStateName);
            }

            public bool IsComplete(float threshold)
            {
                return isComplete != null && isComplete(threshold);
            }

            public string GetPresentationStateName(AttackStep step)
            {
                return step.AnimationStateName;
            }
        }
    }
}
