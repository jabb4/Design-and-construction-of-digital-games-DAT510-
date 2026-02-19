namespace Player.StateMachine.States
{
    using global::StateMachine.Core;
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

        public override TransitionDecision EvaluateTransition()
        {
            if (Motor.IsGrounded)
            {
                return TransitionDecision.To(Owner.GetState<JumpEndState>(), TransitionReason.Landed, priority: TransitionPriorities.AirStateSync);
            }

            return TransitionDecision.None;
        }
    }
}
