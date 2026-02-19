namespace Player.StateMachine.States
{
    using global::StateMachine.Core;
    using UnityEngine;

    public class IdleState : GroundedStateBase
    {
        public override void OnEnter()
        {
            Animator.SetFloat(SpeedHash, 0f);
            Animator.SetFloat(VelocityXHash, 0f);
            Animator.SetFloat(VelocityZHash, 0f);
            Animator.SetBool(IsMovingHash, false);
            Animator.SetBool(IsSprintingHash, false);

            CrossFade("Idle", 0.2f);
        }

        public override void OnFixedUpdate()
        {
            if (Owner.IsTransitioningWeapon)
            {
                Motor.Move(Vector2.zero, useSprint: false);
                return;
            }

            if (!Motor.IsLockedOn)
            {
                return;
            }

            Transform target = Motor.GetLockOnTarget();
            if (target == null)
            {
                return;
            }

            Vector3 toTarget = target.position - Motor.transform.position;
            toTarget.y = 0;

            if (toTarget.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(toTarget);
                Motor.transform.rotation = Quaternion.Slerp(
                    Motor.transform.rotation,
                    targetRotation,
                    Time.fixedDeltaTime * 5f
                );
            }
        }

        public override TransitionDecision EvaluateTransition()
        {
            if (!Motor.IsGrounded)
            {
                return TransitionDecision.To(Owner.GetState<JumpLoopState>(), TransitionReason.Airborne, priority: 25);
            }

            if (TryGetCommonGroundedTransition(out TransitionDecision decision))
            {
                return decision;
            }

            if (Input.MoveInput.sqrMagnitude > 0.01f)
            {
                if (Input.IsSprinting)
                {
                    return TransitionDecision.To(Owner.GetState<SprintState>(), TransitionReason.InputMove);
                }

                return TransitionDecision.To(Owner.GetState<WalkingState>(), TransitionReason.InputMove);
            }

            return TransitionDecision.None;
        }

    }
}
