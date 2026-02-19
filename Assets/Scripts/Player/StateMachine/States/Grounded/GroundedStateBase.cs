namespace Player.StateMachine.States
{
    using Player.StateMachine;
    using global::StateMachine.Core;

    public abstract class GroundedStateBase : PlayerStateBase
    {
        /// <summary>
        /// Handles shared grounded transition checks for attack, block and jump.
        /// Returns true when a decision has been made (including "stay in current state").
        /// </summary>
        protected bool TryGetCommonGroundedTransition(out TransitionDecision decision)
        {
            if (Input.IsAttackPressed && Motor.IsGrounded)
            {
                if (!Owner.IsEquipped)
                {
                    Owner.RequestEquip();
                    decision = TransitionDecision.None;
                    return true;
                }

                AttackState attackState = Owner.GetState<AttackState>();
                attackState.SetComboIndex(0);
                decision = TransitionDecision.To(attackState, TransitionReason.InputAttack, priority: TransitionPriorities.InputPrimary);
                return true;
            }

            if (Input.IsBlocking && Owner.IsEquipped && Motor.IsGrounded)
            {
                decision = TransitionDecision.To(Owner.GetState<BlockingState>(), TransitionReason.InputBlock, priority: TransitionPriorities.InputSecondary);
                return true;
            }

            if (Input.IsJumpPressed && Motor.IsGrounded)
            {
                decision = TransitionDecision.To(Owner.GetState<JumpStartState>(), TransitionReason.InputJump, priority: TransitionPriorities.InputSecondary);
                return true;
            }

            decision = TransitionDecision.None;
            return false;
        }
    }
}
