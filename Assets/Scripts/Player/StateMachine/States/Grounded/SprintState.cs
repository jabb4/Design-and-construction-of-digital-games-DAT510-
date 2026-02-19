namespace Player.StateMachine.States
{
    using global::StateMachine.Core;
    using UnityEngine;

    public class SprintState : GroundedStateBase
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
            locomotion.Update(HasMoveIntent && SprintHeld);
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
                Motor.Move(MoveIntent, useSprint: true);
            }

            RotateWithContext(requireMovementInput: true);
        }

        public override TransitionDecision EvaluateTransition()
        {
            if (TryGetCommonGroundedTransition(out TransitionDecision decision))
            {
                return decision;
            }

            if (!Motor.IsGrounded)
            {
                return TransitionDecision.To(Owner.GetState<JumpLoopState>(), TransitionReason.Airborne, priority: TransitionPriorities.AirStateSync);
            }

            if (!SprintHeld && HasMoveIntent)
            {
                return TransitionDecision.To(Owner.GetState<WalkingState>(), TransitionReason.InputMove);
            }

            if (locomotion.CurrentPhase == WeightedLocomotion.Phase.Stop &&
                locomotion.IsStopComplete())
            {
                if (HasMoveIntent)
                {
                    return SprintHeld
                        ? TransitionDecision.To(Owner.GetState<SprintState>(), TransitionReason.InputMove)
                        : TransitionDecision.To(Owner.GetState<WalkingState>(), TransitionReason.InputMove);
                }

                return TransitionDecision.To(Owner.GetState<IdleState>(), TransitionReason.StandardFlow);
            }

            return TransitionDecision.None;
        }

        public override void OnExit()
        {
            locomotion?.Reset();
            Animator.SetBool(IsSprintingHash, false);
        }

    }
}
