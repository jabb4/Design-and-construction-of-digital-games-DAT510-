namespace Player.StateMachine.Transitions
{
    using global::StateMachine.Core;
    using Player.StateMachine.States;

    /// <summary>
    /// Shared transition helpers for grounded and landing locomotion flows.
    /// Keeps state classes focused on their state-specific behavior.
    /// </summary>
    public static class GroundedTransitionEvaluator
    {
        public static TransitionDecision ToAirborneLoop(PlayerStateMachine owner, bool isGrounded)
        {
            if (owner == null || isGrounded)
            {
                return TransitionDecision.None;
            }

            return TransitionDecision.To(
                owner.GetState<JumpLoopState>(),
                TransitionReason.Airborne,
                priority: TransitionPriorities.AirStateSync);
        }

        public static TransitionDecision ToSprintFromGrounded(PlayerStateMachine owner, bool hasMoveIntent, bool sprintHeld)
        {
            if (owner == null || !hasMoveIntent || !sprintHeld)
            {
                return TransitionDecision.None;
            }

            return TransitionDecision.To(owner.GetState<SprintState>(), TransitionReason.InputMove);
        }

        public static TransitionDecision ToWalkFromSprint(PlayerStateMachine owner, bool hasMoveIntent, bool sprintHeld)
        {
            if (owner == null || !hasMoveIntent || sprintHeld)
            {
                return TransitionDecision.None;
            }

            return TransitionDecision.To(owner.GetState<WalkingState>(), TransitionReason.InputMove);
        }

        public static TransitionDecision ToLocomotion(
            PlayerStateMachine owner,
            bool hasMoveIntent,
            bool sprintHeld,
            TransitionReason reason = TransitionReason.InputMove)
        {
            if (owner == null || !hasMoveIntent)
            {
                return TransitionDecision.None;
            }

            return sprintHeld
                ? TransitionDecision.To(owner.GetState<SprintState>(), reason)
                : TransitionDecision.To(owner.GetState<WalkingState>(), reason);
        }

        public static TransitionDecision ToLocomotionOrIdle(
            PlayerStateMachine owner,
            bool hasMoveIntent,
            bool sprintHeld,
            TransitionReason moveReason = TransitionReason.InputMove,
            TransitionReason idleReason = TransitionReason.StandardFlow)
        {
            if (owner == null)
            {
                return TransitionDecision.None;
            }

            TransitionDecision moveDecision = ToLocomotion(owner, hasMoveIntent, sprintHeld, moveReason);
            if (moveDecision.HasTransition)
            {
                return moveDecision;
            }

            return TransitionDecision.To(owner.GetState<IdleState>(), idleReason);
        }
    }
}
