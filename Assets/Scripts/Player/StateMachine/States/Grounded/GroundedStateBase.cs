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
        protected bool TryGetCommonGroundedTransition(out IState nextState)
        {
            if (Input.IsAttackPressed && Motor.IsGrounded)
            {
                if (!Owner.IsEquipped)
                {
                    Owner.RequestEquip();
                    nextState = null;
                    return true;
                }

                AttackState attackState = Owner.GetState<AttackState>();
                attackState.SetComboIndex(0);
                nextState = attackState;
                return true;
            }

            if (Input.IsBlocking && Owner.IsEquipped && Motor.IsGrounded)
            {
                nextState = Owner.GetState<BlockingState>();
                return true;
            }

            if (Input.IsJumpPressed && Motor.IsGrounded)
            {
                nextState = Owner.GetState<JumpStartState>();
                return true;
            }

            nextState = null;
            return false;
        }
    }
}
