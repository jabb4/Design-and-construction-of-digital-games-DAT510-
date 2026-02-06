namespace Player.StateMachine.States
{
    using UnityEngine;

    public class JumpLoopState : PlayerStateBase
    {
        public override void OnEnter()
        {
            CrossFade("Jump Loop", 0.1f);
        }

        public override void OnFixedUpdate()
        {
            Motor.Move(Input.MoveInput, Input.IsSprinting);

            if (Motor.Velocity.y < 0)
            {
                Motor.ApplyFallGravity();
            }
        }

        public override IState CheckTransitions()
        {
            if (Motor.IsGrounded)
            {
                return Owner.GetState<JumpEndState>();
            }

            return null;
        }
    }
}
