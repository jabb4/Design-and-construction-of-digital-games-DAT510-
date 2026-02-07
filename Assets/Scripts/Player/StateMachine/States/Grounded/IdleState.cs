namespace Player.StateMachine.States
{
    using UnityEngine;

    public class IdleState : PlayerStateBase
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

        public override IState CheckTransitions()
        {
            if (!Motor.IsGrounded)
            {
                return Owner.GetState<JumpLoopState>();
            }

            if (Input.IsAttackPressed && Motor.IsGrounded)
            {
                if (!Owner.IsEquipped)
                {
                    Owner.RequestEquip();
                    return null;
                }

                var attackState = Owner.GetState<AttackState>();
                attackState.SetComboIndex(0);
                return attackState;
            }

            if (Input.IsBlocking && Owner.IsEquipped && Motor.IsGrounded)
            {
                return Owner.GetState<BlockingState>();
            }

            if (Input.IsJumpPressed && Motor.IsGrounded)
            {
                return Owner.GetState<JumpStartState>();
            }

            if (Input.MoveInput.sqrMagnitude > 0.01f)
            {
                if (Input.IsSprinting)
                {
                    return Owner.GetState<SprintState>();
                }

                return Owner.GetState<WalkingState>();
            }

            return null;
        }

    }
}
