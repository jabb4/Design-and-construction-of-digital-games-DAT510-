namespace Player.StateMachine.States
{
    using UnityEngine;

    public class SprintState : PlayerStateBase
    {
        private WeightedLocomotion locomotion;
        private Vector2 smoothVelocity;
        private Vector2 velocityRef;
        private const float SMOOTH_TIME = 0.1f;
        
        public override void OnEnter()
        {
            bool isEquipped = Animator.GetBool(IsEquippedHash);
            
            locomotion = new WeightedLocomotion(
                Animator,
                () => "Run Start",
                () => "Sprint",
                () => "Run Stop",
                startDuration: 0.15f,
                loopDuration: 0.2f,
                stopDuration: 0.15f
            );
            if (Motor.IsLockedOn)
            {
                locomotion.ForceLoop();
            }
            else
            {
                locomotion.Begin();
            }

            Animator.SetBool(IsSprintingHash, true);
            Animator.SetBool(IsMovingHash, true);

            smoothVelocity = Vector2.zero;
            velocityRef = Vector2.zero;
        }

        public override void OnUpdate()
        {
            locomotion.Update(Input.HasMovementInput && Input.IsSprinting);
            smoothVelocity = UpdateBlendTreeParameters(smoothVelocity, ref velocityRef, SMOOTH_TIME, Motor.IsLockedOn);
        }

        public override void OnFixedUpdate()
        {
            if (Owner.IsTransitioningWeapon)
            {
                Motor.Move(Vector2.zero, useSprint: false);
                return;
            }

            if (locomotion.CurrentPhase == WeightedLocomotion.Phase.Stop)
            {
                Motor.Move(Vector2.zero, useSprint: false);
            }
            else
            {
                Motor.Move(Input.MoveInput, useSprint: true);
            }

            RotateWithContext(requireMovementInput: true);
        }

        public override IState CheckTransitions()
        {
            if (Input.IsBlocking && Owner.IsEquipped)
            {
                return Owner.GetState<global::Player.StateMachine.BlockingState>();
            }

            if (Input.IsJumpPressed && Motor.IsGrounded)
            {
                return Owner.GetState<JumpStartState>();
            }

            if (!Motor.IsGrounded)
            {
                return Owner.GetState<JumpLoopState>();
            }

            if (!Input.IsSprinting && Input.HasMovementInput)
            {
                return Owner.GetState<WalkingState>();
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

            return null;
        }

        public override void OnExit()
        {
            locomotion?.Reset();
            Animator.SetBool(IsSprintingHash, false);
        }

    }
}
