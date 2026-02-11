namespace Player.StateMachine
{
    using UnityEngine;

    public sealed class BlockingState : PlayerStateBase
    {
        private Vector2 smoothVelocity;
        private Vector2 velocityRef;
        private const float SMOOTH_TIME = 0.1f;
        private const string DefenseIdleStateName = "DefenseIdle";
        private const string StandToDefenseLeft = "Idle2DefenseL";
        private const string DefenseToStandLeft = "Defense2IdleL";
        private bool isExiting;

        public override void OnEnter()
        {
            isExiting = false;

            if (!CrossFade(StandToDefenseLeft, 0.1f))
            {
                CrossFade(DefenseIdleStateName, 0.1f);
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

            smoothVelocity = UpdateBlendTreeParameters(smoothVelocity, ref velocityRef, SMOOTH_TIME, Motor.IsLockedOn);
        }

        public override void OnFixedUpdate()
        {
            if (Owner.IsTransitioningWeapon)
            {
                Motor.Move(Vector2.zero, useSprint: false);
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
                if (!isExiting)
                {
                    isExiting = true;
                    CrossFade(DefenseToStandLeft, 0.1f);
                }

                return null;
            }

            if (isExiting)
            {
                isExiting = false;
                CrossFade(StandToDefenseLeft, 0.1f);
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
