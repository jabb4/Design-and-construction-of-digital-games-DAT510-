namespace Player.StateMachine.States
{
    using UnityEngine;
    using Player.StateMachine;

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
            if (isInAttackState)
            {
                hasPlayedAttackAnimation = true;
            }

            if (hasEnteredRecovery && isInAttackState && Input.IsAttackPressed)
            {
                queuedNextAttack = true;
            }

            if (hasEnteredRecovery && Motor.IsGrounded)
            {
                if (Input.IsBlocking && Owner.IsEquipped)
                {
                    Owner.ClearCurrentAttack();
                    return Owner.GetState<BlockingState>();
                }

                if (recoveryElapsed >= RecoveryMoveDelay)
                {
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
                }
            }

            if (hasEnteredRecovery && queuedNextAttack && comboIndex < Owner.AttackStepCount - 1)
            {
                var nextState = Owner.GetState<AttackState>();
                nextState.SetComboIndex(comboIndex + 1);
                return nextState;
            }

            if (isInAttackState && IsAnimationComplete(0.98f))
            {
                Owner.ClearCurrentAttack();
                return Input.HasMovementInput
                    ? (Input.IsSprinting ? Owner.GetState<SprintState>() : Owner.GetState<WalkingState>())
                    : Owner.GetState<IdleState>();
            }

            if (hasPlayedAttackAnimation && !isInAttackState)
            {
                Owner.ClearCurrentAttack();
                return Input.HasMovementInput
                    ? (Input.IsSprinting ? Owner.GetState<SprintState>() : Owner.GetState<WalkingState>())
                    : Owner.GetState<IdleState>();
            }

            return null;
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
