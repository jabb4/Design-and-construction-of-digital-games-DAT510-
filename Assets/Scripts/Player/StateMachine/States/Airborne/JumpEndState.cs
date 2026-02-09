namespace Player.StateMachine.States
{
    using UnityEngine;

    public class JumpEndState : PlayerStateBase
    {
        private static readonly int JumpEndHash = Animator.StringToHash("Jump End");
        private float landingTimer;
        private float landingDuration;

        public override void OnEnter()
        {
            CrossFade("Jump End", 0.15f);
            Animator.SetTrigger(LandHash);

            landingDuration = Motor.LandingMoveBlendTime;
            landingTimer = 0f;
        }

        public override void OnFixedUpdate()
        {
            landingTimer += Time.fixedDeltaTime;
            float blend = landingDuration <= 0f ? 1f : Mathf.Clamp01(landingTimer / landingDuration);

            Motor.Move(Input.MoveInput, Input.IsSprinting, blend);

            if (landingTimer >= landingDuration)
            {
                RotateWithContext(requireMovementInput: true);
            }
        }

        public override IState CheckTransitions()
        {
            if (Input.IsJumpBuffered && GetAnimatorNormalizedTime() >= 0.9f)
            {
                return Owner.GetState<JumpStartState>();
            }

            AnimatorStateInfo stateInfo = Animator.GetCurrentAnimatorStateInfo(0);
            bool inJumpEnd = stateInfo.shortNameHash == JumpEndHash;

            if (inJumpEnd && IsAnimationComplete(0.9f))
            {
                if (Input.HasMovementInput)
                {
                    if (Input.IsSprinting)
                    {
                        return Owner.GetState<SprintState>();
                    }
                    else
                    {
                        return Owner.GetState<WalkingState>();
                    }
                }
                else
                {
                    return Owner.GetState<IdleState>();
                }
            }
            
            return null;
        }
    }
}
