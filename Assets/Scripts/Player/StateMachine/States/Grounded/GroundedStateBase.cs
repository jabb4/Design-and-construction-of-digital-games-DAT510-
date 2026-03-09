namespace Player.StateMachine.States
{
    using Player.StateMachine;
    using Player.StateMachine.Transitions;
    using global::StateMachine.Core;

    public abstract class GroundedStateBase : PlayerStateBase
    {
        /// <summary>
        /// Handles shared grounded transition checks: airborne, attack, block and jump.
        /// Returns true when a decision has been made (including "stay in current state").
        /// </summary>
        protected bool TryGetCommonGroundedTransition(out TransitionDecision decision)
        {
            TransitionDecision airborneTransition = GroundedTransitionEvaluator.ToAirborneLoop(Owner, Motor.IsGrounded);
            if (airborneTransition.HasTransition)
            {
                decision = airborneTransition;
                return true;
            }

            if ((AttackPressed || AttackBuffered) && Motor.IsGrounded)
            {
                if (!Owner.IsEquipped)
                {
                    Owner.RequestEquipWithPendingAttack();
                    decision = TransitionDecision.None;
                    return true;
                }

                AttackState attackState = Owner.GetState<AttackState>();
                attackState.SetComboIndex(0);
                decision = TransitionDecision.To(attackState, TransitionReason.InputAttack, priority: TransitionPriorities.InputPrimary);
                return true;
            }

            if (BlockHeld && Owner.IsEquipped && Motor.IsGrounded)
            {
                decision = TransitionDecision.To(Owner.GetState<BlockingState>(), TransitionReason.InputBlock, priority: TransitionPriorities.InputSecondary);
                return true;
            }

            if (JumpPressed && Motor.IsGrounded)
            {
                decision = TransitionDecision.To(Owner.GetState<JumpStartState>(), TransitionReason.InputJump, priority: TransitionPriorities.InputSecondary);
                return true;
            }

            decision = TransitionDecision.None;
            return false;
        }
    }
}
