namespace Player.StateMachine.States
{
    using UnityEngine;
    
    public class JumpStartState : PlayerStateBase
    {
        private static readonly int JumpStartHash = Animator.StringToHash("Jump Start Low");

        public override void OnEnter()
        {
            CrossFade("Jump Start Low", 0.1f);
            Animator.SetTrigger(JumpHash);
            Motor.Jump();
        }
        
        public override void OnFixedUpdate()
        {
            Motor.Move(Input.MoveInput, Input.IsSprinting);
        }
        
        public override IState CheckTransitions()
        {
            AnimatorStateInfo stateInfo = Animator.GetCurrentAnimatorStateInfo(0);
            bool inJumpStart = stateInfo.shortNameHash == JumpStartHash;

            if (inJumpStart)
            {
                if (IsAnimationComplete(0.8f) || Motor.Velocity.y < 0)
                {
                    return Owner.GetState<JumpLoopState>();
                }
            }
            else if (Motor.Velocity.y < 0)
            {
                return Owner.GetState<JumpLoopState>();
            }
            
            return null;
        }
    }
}
