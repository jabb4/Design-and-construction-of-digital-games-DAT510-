namespace Player.StateMachine.States
{
    using UnityEngine;
    using Player.StateMachine;
    using global::StateMachine.Core;

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

            CrossFade(step.AnimationStateName, 0.1f);
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
                if (Motor.IsLockedOn || Input.HasMovementInput)
                {
                    RotateWithContext(requireMovementInput: true);
                }
            }

            Motor.Move(Vector2.zero, useSprint: false);
        }

        public override IState CheckTransitions()
        {
            if (!TryGetCurrentAttackStep(out AttackStep step))
            {
                return Owner.GetState<IdleState>();
            }

            bool isInAttackState = IsAnimatorInState(step.AnimationStateName);
            RegisterAttackPresence(isInAttackState);
            BufferComboInputIfNeeded(isInAttackState);

            IState recoveryTransition = TryGetRecoveryTransition();
            if (recoveryTransition != null)
            {
                return recoveryTransition;
            }

            IState comboTransition = TryGetComboTransition();
            if (comboTransition != null)
            {
                return comboTransition;
            }

            IState exitTransition = TryGetAttackExitTransition(isInAttackState);
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

            if (!IsAnimatorInState(step.AnimationStateName))
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
            if (hasEnteredRecovery && isInAttackState && Input.IsAttackPressed)
            {
                queuedNextAttack = true;
            }
        }

        private IState TryGetRecoveryTransition()
        {
            if (!hasEnteredRecovery || !Motor.IsGrounded)
            {
                return null;
            }

            if (Input.IsBlocking && Owner.IsEquipped)
            {
                Owner.ClearCurrentAttack();
                return Owner.GetState<BlockingState>();
            }

            if (recoveryElapsed < RecoveryMoveDelay)
            {
                return null;
            }

            if (Input.IsJumpPressed)
            {
                Owner.ClearCurrentAttack();
                return Owner.GetState<JumpStartState>();
            }

            if (Input.HasMovementInput)
            {
                Owner.ClearCurrentAttack();
                return Input.IsSprinting ? Owner.GetState<SprintState>() : Owner.GetState<WalkingState>();
            }

            return null;
        }

        private IState TryGetComboTransition()
        {
            if (!hasEnteredRecovery || !queuedNextAttack || comboIndex >= Owner.AttackStepCount - 1)
            {
                return null;
            }

            var nextState = Owner.GetState<AttackState>();
            nextState.SetComboIndex(comboIndex + 1);
            return nextState;
        }

        private IState TryGetAttackExitTransition(bool isInAttackState)
        {
            if (isInAttackState && IsAnimationComplete(0.98f))
            {
                return ExitAttackToLocomotionOrIdle();
            }

            if (hasPlayedAttackAnimation && !isInAttackState)
            {
                return ExitAttackToLocomotionOrIdle();
            }

            return null;
        }

        private IState ExitAttackToLocomotionOrIdle()
        {
            Owner.ClearCurrentAttack();
            return Input.HasMovementInput
                ? (Input.IsSprinting ? Owner.GetState<SprintState>() : Owner.GetState<WalkingState>())
                : Owner.GetState<IdleState>();
        }

        private void WarnIfMissingAttackEvents(AttackStep step)
        {
            if (hasPhaseEvents || hasLoggedMissingEventWarning)
            {
                return;
            }

            if (!IsAnimatorInState(step.AnimationStateName))
            {
                return;
            }

            if (GetAnimatorNormalizedTime() < 0.98f)
            {
                return;
            }

            hasLoggedMissingEventWarning = true;
            Debug.LogWarning(
                $"[AttackState] No attack phase events received for '{step.AnimationStateName}'. " +
                "Add animation events that call OnAttackWindup, OnAttackSlash, and OnAttackRecovery.",
                Animator);
        }
    }
}
