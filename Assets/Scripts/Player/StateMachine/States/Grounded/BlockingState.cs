namespace Player.StateMachine
{
    using UnityEngine;

    public sealed class BlockingState : PlayerStateBase
    {
        private Vector2 smoothVelocity;
        private Vector2 velocityRef;
        private const float SMOOTH_TIME = 0.1f;
        private bool isExiting;
        private GuardSide activeGuardSide;

        public override void OnEnter()
        {
            isExiting = false;
            activeGuardSide = Owner.CurrentGuardSide;

            if (!CrossFade(Owner.GetDefenseEnterStateName(activeGuardSide), 0.1f))
            {
                CrossFade(Owner.GetDefenseIdleStateName(activeGuardSide), 0.1f);
            }

            Animator.SetBool(IsMovingHash, Input.HasMovementInput);
            Animator.SetBool(IsSprintingHash, false);

            smoothVelocity = Vector2.zero;
            velocityRef = Vector2.zero;
        }

        public override void OnUpdate()
        {
            if (isExiting)
            {
                if (IsAnimationComplete(0.95f))
                {
                    if (Input.HasMovementInput)
                    {
                        Owner.ChangeState(Owner.GetState<States.WalkingState>());
                    }
                    else
                    {
                        Owner.ChangeState(Owner.GetState<States.IdleState>());
                    }
                }

                return;
            }

            if (Owner.CurrentGuardSide != activeGuardSide && !Owner.IsDefenseReactionActive)
            {
                activeGuardSide = Owner.CurrentGuardSide;
                if (!isExiting)
                {
                    CrossFade(Owner.GetDefenseIdleStateName(activeGuardSide), 0.08f);
                }
            }

            smoothVelocity = UpdateBlendTreeParameters(smoothVelocity, ref velocityRef, SMOOTH_TIME, Motor.IsLockedOn);
        }

        public override void OnFixedUpdate()
        {
            if (Owner.IsTransitioningWeapon)
            {
                Motor.Move(Vector2.zero, useSprint: false);
                return;
            }

            if (Owner.IsDefenseReactionActive)
            {
                // Match constrained movement behavior used by other one-shot combat animations.
                Motor.Move(Vector2.zero, useSprint: false);
                RotateWithContext(requireMovementInput: true);
                return;
            }

            Motor.Move(Input.MoveInput, useSprint: false);
            RotateWithContext(requireMovementInput: true);
        }

        public override IState CheckTransitions()
        {
            if (!Owner.IsEquipped)
            {
                return Owner.GetState<States.IdleState>();
            }

            if (!Input.IsBlocking)
            {
                if (Owner.IsDefenseReactionActive)
                {
                    return null;
                }

                if (!isExiting)
                {
                    isExiting = true;
                    activeGuardSide = Owner.CurrentGuardSide;
                    CrossFade(Owner.GetDefenseExitStateName(activeGuardSide), 0.1f);
                }

                return null;
            }

            if (isExiting && !Owner.IsDefenseReactionActive)
            {
                isExiting = false;
                activeGuardSide = Owner.CurrentGuardSide;
                CrossFade(Owner.GetDefenseEnterStateName(activeGuardSide), 0.1f);
            }

            if (Input.IsJumpPressed && Motor.IsGrounded)
            {
                return null;
            }

            if (Input.IsSprinting && Input.HasMovementInput)
            {
                return null;
            }

            return null;
        }
    }
}
