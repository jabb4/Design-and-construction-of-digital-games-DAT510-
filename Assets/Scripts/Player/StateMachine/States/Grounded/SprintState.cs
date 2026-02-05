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
                () => GetSprintStartAnimation(isEquipped),
                () => "Sprint",
                () => GetSprintStopAnimation(isEquipped),
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
            UpdateBlendTreeParameters();
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

            if (Motor.IsLockedOn)
            {
                Motor.RotateTowardsLockOnTarget();
            }
            else if (Input.HasMovementInput)
            {
                Motor.RotateTowardsMovement(Input.MoveInput);
            }
        }

        public override IState CheckTransitions()
        {
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

        private string GetSprintStartAnimation(bool isEquipped)
        {
            return isEquipped ? "Run Start" : "Run Start";
        }

        private string GetSprintStopAnimation(bool isEquipped)
        {
            return isEquipped ? "Run Stop" : "Run Stop";
        }

        private void UpdateBlendTreeParameters()
        {
            Vector2 targetVelocity = Motor.IsLockedOn
                ? Input.MoveInput
                : new Vector2(0f, Input.MoveInput.magnitude);

            smoothVelocity = Vector2.SmoothDamp(smoothVelocity, targetVelocity, ref velocityRef, SMOOTH_TIME);

            Animator.SetFloat(VelocityXHash, smoothVelocity.x);
            Animator.SetFloat(VelocityZHash, smoothVelocity.y);
            Animator.SetFloat(SpeedHash, smoothVelocity.magnitude);
        }
    }
}
