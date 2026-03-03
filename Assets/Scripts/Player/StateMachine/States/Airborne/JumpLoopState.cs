namespace Player.StateMachine.States
{
    using global::StateMachine.Core;
    using UnityEngine;

    public class JumpLoopState : PlayerStateBase
    {
        private float airborneElapsed;

        public float AirborneElapsed => airborneElapsed;

        public override void OnEnter()
        {
            airborneElapsed = 0f;
            CrossFade("Jump Loop", 0.1f);
        }

        public override void OnFixedUpdate()
        {
            airborneElapsed += Time.fixedDeltaTime;
            Motor.Move(MoveIntent, SprintHeld);

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
