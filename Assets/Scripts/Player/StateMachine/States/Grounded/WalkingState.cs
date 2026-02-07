namespace Player.StateMachine.States
{
    using UnityEngine;

    public class WalkingState : PlayerStateBase
    {
        private WeightedLocomotion locomotion;
        private Vector2 smoothVelocity;
        private Vector2 velocityRef;
        private Vector2 lastMoveDirection;

        private const float SMOOTH_TIME = 0.1f;

        public override void OnEnter()
        {
            bool isEquipped = Animator.GetBool(IsEquippedHash);
            bool isLockedOn = Motor.IsLockedOn;

            lastMoveDirection = Input.MoveInput;

            locomotion = new WeightedLocomotion(
                Animator,
                () => GetWalkStartAnimation(isEquipped, isLockedOn),
                () => "Walk Locomotion",
                () => GetWalkStopAnimation(isEquipped, isLockedOn),
                startDuration: 0.1f,
                loopDuration: 0.25f,
                stopDuration: 0.1f
            );

            if (isLockedOn || !isEquipped)
            {
                locomotion.ForceLoop();
                smoothVelocity = isLockedOn ? Input.MoveInput : Vector2.zero;
            }
            else
            {
                locomotion.Begin();
                smoothVelocity = Vector2.zero;
            }

            Animator.SetBool(IsMovingHash, true);
            Animator.SetBool(IsSprintingHash, false);

            velocityRef = Vector2.zero;
        }

        private string GetWalkStartAnimation(bool isEquipped, bool isLockedOn)
        {
            if (!isEquipped)
            {
                return "Walk Start";
            }

            if (!isLockedOn)
            {
                return "Walk Start F";
            }

            Vector2 input = Input.MoveInput;
            if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
            {
                return input.x > 0 ? "Walk Start R" : "Walk Start L";
            }
            else
            {
                return input.y >= 0 ? "Walk Start F" : "Walk Start B";
            }
        }

        private string GetWalkStopAnimation(bool isEquipped, bool isLockedOn)
        {
            if (!isEquipped)
            {
                return "Walk Stop";
            }

            if (!isLockedOn)
            {
                return "Walk Stop F";
            }

            Vector2 vel = smoothVelocity;
            if (Mathf.Abs(vel.x) > Mathf.Abs(vel.y))
            {
                return vel.x > 0 ? "Walk Stop R" : "Walk Stop L";
            }
            else
            {
                return vel.y >= 0 ? "Walk Stop F" : "Walk Stop B";
            }
        }

        public override void OnUpdate()
        {
            locomotion.Update(Input.HasMovementInput);
            smoothVelocity = UpdateBlendTreeParameters(smoothVelocity, ref velocityRef, SMOOTH_TIME, Motor.IsLockedOn);

            if (Input.HasMovementInput)
            {
                lastMoveDirection = Input.MoveInput;
            }
        }

        public override void OnFixedUpdate()
        {
            if (Owner.IsTransitioningWeapon)
            {
                Motor.Move(Vector2.zero, useSprint: false);
                return;
            }

            Motor.Move(Input.MoveInput, useSprint: false);

            RotateWithContext();
        }

        public override IState CheckTransitions()
        {
            if (Input.IsBlocking && Owner.IsEquipped && Motor.IsGrounded)
            {
                return Owner.GetState<BlockingState>();
            }

            if (Input.IsJumpPressed && Motor.IsGrounded)
            {
                return Owner.GetState<JumpStartState>();
            }

            if (!Motor.IsGrounded)
            {
                return Owner.GetState<JumpLoopState>();
            }

            if (Input.IsSprinting && Input.HasMovementInput)
            {
                return Owner.GetState<SprintState>();
            }

            if (locomotion.CurrentPhase == WeightedLocomotion.Phase.Stop &&
                locomotion.IsStopComplete())
            {
                if (Input.HasMovementInput)
                {
                    return Input.IsSprinting
                        ? Owner.GetState<SprintState>()
                        : Owner.GetState<WalkingState>();
                }

                return Owner.GetState<IdleState>();
            }

            if (!Input.HasMovementInput && locomotion.IsLooping)
            {
                locomotion.RequestStop();
            }

            return null;
        }

        public override void OnExit()
        {
            locomotion?.Reset();
            Animator.SetBool(IsMovingHash, false);
        }
    }
}
