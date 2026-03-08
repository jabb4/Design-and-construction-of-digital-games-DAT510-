namespace Player.StateMachine
{
    using global::StateMachine.Core;
    using UnityEngine;

    public sealed class BlockingState : PlayerStateBase
    {
        private Vector2 smoothVelocity;
        private Vector2 velocityRef;
        private const float SMOOTH_TIME = 0.1f;
        private bool isExiting;
        private GuardSide activeGuardSide;

        public override void OnEnter()
        {
            isExiting = false;
            activeGuardSide = Owner.CurrentGuardSide;

            if (!CrossFade(Owner.GetDefenseEnterStateName(activeGuardSide), 0.1f))
            {
                CrossFade(Owner.GetDefenseIdleStateName(activeGuardSide), 0.1f);
            }

            Animator.SetBool(IsMovingHash, HasMoveIntent);
            Animator.SetBool(IsSprintingHash, false);

            smoothVelocity = Vector2.zero;
            velocityRef = Vector2.zero;
        }

        public override void OnUpdate()
        {
            if (isExiting)
            {
                if (IsAnimationComplete(0.95f))
                {
                    if (HasMoveIntent)
                    {
                        Owner.ChangeState(Owner.GetState<States.WalkingState>());
                    }
                    else
                    {
                        Owner.ChangeState(Owner.GetState<States.IdleState>());
                    }
                }

                return;
            }

            if (Owner.CurrentGuardSide != activeGuardSide && !Owner.IsDefenseReactionActive)
            {
                activeGuardSide = Owner.CurrentGuardSide;
                if (!isExiting)
                {
                    CrossFade(Owner.GetDefenseIdleStateName(activeGuardSide), 0.08f);
                }
            }

            smoothVelocity = UpdateBlendTreeParameters(smoothVelocity, ref velocityRef, SMOOTH_TIME, Motor.IsLockedOn);
        }

        public override void OnFixedUpdate()
        {
            if (Owner.IsTransitioningWeapon)
            {
                Motor.Move(Vector2.zero, useSprint: false);
                return;
            }

            if (Owner.IsDefenseReactionActive)
            {
                // Freeze both movement and facing while defense reaction one-shots are active.
                Motor.Move(Vector2.zero, useSprint: false);
                return;
            }

            Motor.Move(MoveIntent, useSprint: false);
            RotateWithContext(requireMovementInput: true);
        }

        public override TransitionDecision EvaluateTransition()
        {
            if (!Owner.IsEquipped)
            {
                return TransitionDecision.To(Owner.GetState<States.IdleState>(), TransitionReason.StandardFlow);
            }

            if (Owner.IsDefenseReactionActive &&
                Owner.IsDefenseAttackUnlocked &&
                AttackPressed &&
                Motor.IsGrounded)
            {
                // Allow earlier attack cancel after successful defense,
                // while movement remains constrained until reaction end.
                Owner.EndDefenseReaction();

                States.AttackState attackState = Owner.GetState<States.AttackState>();
                attackState.SetComboIndex(0);
                return TransitionDecision.To(attackState, TransitionReason.RecoveryInterrupt, priority: TransitionPriorities.RecoveryInterrupt);
            }

            if (!BlockHeld)
            {
                if (Owner.IsDefenseReactionActive)
                {
                    return TransitionDecision.None;
                }

                if (!isExiting)
                {
                    isExiting = true;
                    activeGuardSide = Owner.CurrentGuardSide;
                    CrossFade(Owner.GetDefenseExitStateName(activeGuardSide), 0.1f);
                }

                // Allow attack to interrupt the block-to-idle exit animation.
                if (isExiting && AttackPressed && Motor.IsGrounded)
                {
                    States.AttackState attackState = Owner.GetState<States.AttackState>();
                    attackState.SetComboIndex(0);
                    return TransitionDecision.To(attackState, TransitionReason.RecoveryInterrupt, priority: TransitionPriorities.RecoveryInterrupt);
                }

                return TransitionDecision.None;
            }

            if (isExiting && !Owner.IsDefenseReactionActive)
            {
                isExiting = false;
                activeGuardSide = Owner.CurrentGuardSide;
                CrossFade(Owner.GetDefenseEnterStateName(activeGuardSide), 0.1f);
            }

            if (JumpPressed && Motor.IsGrounded)
            {
                return TransitionDecision.None;
            }

            if (SprintHeld && HasMoveIntent)
            {
                return TransitionDecision.None;
            }

            return TransitionDecision.None;
        }
    }
}
