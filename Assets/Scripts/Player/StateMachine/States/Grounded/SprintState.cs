namespace Player.StateMachine.States
{
    using Player.StateMachine.Transitions;
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

            TransitionDecision walkTransition = GroundedTransitionEvaluator.ToWalkFromSprint(Owner, HasMoveIntent, SprintHeld);
            if (walkTransition.HasTransition)
            {
                return walkTransition;
            }

            if (locomotion.CurrentPhase == WeightedLocomotion.Phase.Stop &&
                locomotion.IsStopComplete())
            {
                return GroundedTransitionEvaluator.ToLocomotionOrIdle(Owner, HasMoveIntent, SprintHeld);
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
