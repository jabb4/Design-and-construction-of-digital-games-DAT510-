namespace Player.StateMachine.States
{
    using Player.StateMachine.Transitions;
    using global::StateMachine.Core;
    using UnityEngine;

    public class JumpEndState : PlayerStateBase
    {
        private const float SafetyTimeoutMultiplier = 3f;
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

            Motor.Move(MoveIntent, SprintHeld, blend);

            if (landingTimer >= landingDuration)
            {
                RotateWithContext(requireMovementInput: true);
            }
        }

        public override TransitionDecision EvaluateTransition()
        {
            if (JumpBuffered && GetAnimatorNormalizedTime() >= 0.9f)
            {
                return TransitionDecision.To(Owner.GetState<JumpStartState>(), TransitionReason.InputJump, priority: TransitionPriorities.InputPrimary);
            }

            AnimatorStateInfo stateInfo = Animator.GetCurrentAnimatorStateInfo(0);
            bool inJumpEnd = stateInfo.shortNameHash == JumpEndHash;

            if (inJumpEnd && IsAnimationComplete(0.9f))
            {
                return GroundedTransitionEvaluator.ToLocomotionOrIdle(Owner, HasMoveIntent, SprintHeld);
            }

            // Safety timeout: prevent getting stuck if animation hash never matches
            // (e.g., CrossFade failed or animator is in an unexpected state).
            if (landingTimer >= landingDuration * SafetyTimeoutMultiplier)
            {
                return GroundedTransitionEvaluator.ToLocomotionOrIdle(Owner, HasMoveIntent, SprintHeld);
            }

            return TransitionDecision.None;
        }
    }
}
