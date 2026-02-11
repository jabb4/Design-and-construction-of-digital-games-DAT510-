namespace Player.StateMachine.States
{
    using UnityEngine;
    using Player.StateMachine;

    public sealed class AttackState : PlayerStateBase, IAttackPhaseListener
    {
        private const float RecoveryMoveDelay = 0.5f;
        private int comboIndex;
        private bool queuedNextAttack;
        private AttackPhase currentPhase;
        private bool hasEnteredRecovery;
        private bool hasPhaseEvents;
        private bool hasLoggedMissingEventWarning;
        private float recoveryElapsed;
        private Vector3 attackDirection;
        private bool attackPushActive;
        private float attackPushElapsed;
        private float attackPushDuration;
        private float attackPushDecay;
        private float attackPushInitialSpeed;
        private float attackPushRemainingDistance;

        public AttackPhase CurrentPhase => currentPhase;

        public void SetComboIndex(int index)
        {
            int maxIndex = Mathf.Max(Owner.AttackStepCount - 1, 0);
            comboIndex = Mathf.Clamp(index, 0, maxIndex);
        }

        public override void OnEnter()
        {
            queuedNextAttack = false;
            currentPhase = AttackPhase.Windup;
            hasEnteredRecovery = false;
            hasPhaseEvents = false;
            hasLoggedMissingEventWarning = false;
            recoveryElapsed = 0f;
            attackPushActive = false;
            attackPushElapsed = 0f;
            attackPushDuration = 0f;
            attackPushDecay = 0f;
            attackPushInitialSpeed = 0f;
            attackPushRemainingDistance = 0f;

            UpdateAttackDirectionFromTransform();

            if (!TryGetCurrentAttackStep(out AttackStep step))
            {
                Owner.ChangeState(Owner.GetState<IdleState>());
                return;
            }
            Owner.SetCurrentAttack(step);

            Animator.SetBool(IsMovingHash, false);
            Animator.SetBool(IsSprintingHash, false);

            CrossFade(step.AnimationStateName, 0.1f);
        }

        public override void OnUpdate()
        {
            if (!TryGetCurrentAttackStep(out AttackStep step))
            {
                return;
            }

            WarnIfMissingAttackEvents(step);

            if (hasEnteredRecovery)
            {
                recoveryElapsed += Time.deltaTime;
            }
        }

        public override void OnFixedUpdate()
        {
            if (currentPhase == AttackPhase.Windup)
            {
                if (Motor.IsLockedOn || Input.HasMovementInput)
                {
                    RotateWithContext(requireMovementInput: true);
                    UpdateAttackDirectionFromTransform();
                }
            }

            if (currentPhase == AttackPhase.Slash && attackPushActive)
            {
                float dt = Time.fixedDeltaTime;
                if (dt <= 0f)
                {
                    attackPushActive = false;
                    Motor.SetHorizontalVelocity(Vector3.zero);
                    return;
                }

                float t0 = attackPushElapsed;
                float t1 = Mathf.Min(t0 + dt, attackPushDuration);
                float distanceThisStep;

                if (attackPushDecay > 0f)
                {
                    float exp0 = Mathf.Exp(-attackPushDecay * t0);
                    float exp1 = Mathf.Exp(-attackPushDecay * t1);
                    distanceThisStep = (attackPushInitialSpeed / attackPushDecay) * (exp0 - exp1);
                }
                else
                {
                    float remainingTime = Mathf.Max(attackPushDuration - t0, 0.0001f);
                    distanceThisStep = attackPushRemainingDistance * (t1 - t0) / remainingTime;
                }

                distanceThisStep = Mathf.Min(distanceThisStep, attackPushRemainingDistance);
                float speed = distanceThisStep / dt;
                Motor.SetHorizontalVelocity(attackDirection * speed);

                attackPushElapsed = t1;
                attackPushRemainingDistance -= distanceThisStep;

                if (attackPushRemainingDistance <= 0.0001f || attackPushElapsed >= attackPushDuration)
                {
                    attackPushActive = false;
                    Motor.SetHorizontalVelocity(Vector3.zero);
                }

                return;
            }

            Motor.Move(Vector2.zero, useSprint: false);
        }

        public override IState CheckTransitions()
        {
            if (!TryGetCurrentAttackStep(out AttackStep step))
            {
                return Owner.GetState<IdleState>();
            }

            if (hasEnteredRecovery && Input.IsAttackPressed)
            {
                queuedNextAttack = true;
            }

            if (hasEnteredRecovery && Motor.IsGrounded)
            {
                if (Input.IsBlocking && Owner.IsEquipped)
                {
                    Owner.ClearCurrentAttack();
                    return Owner.GetState<BlockingState>();
                }

                if (recoveryElapsed >= RecoveryMoveDelay)
                {
                    if (Input.IsJumpPressed)
                    {
                        Owner.ClearCurrentAttack();
                        return Owner.GetState<JumpStartState>();
                    }

                    if (Input.HasMovementInput)
                    {
                        Owner.ClearCurrentAttack();
                        return Input.IsSprinting ? Owner.GetState<SprintState>() : Owner.GetState<WalkingState>();
                    }
                }
            }

            bool isInAttackState = IsAnimatorInState(step.AnimationStateName);
            if (hasEnteredRecovery && queuedNextAttack && comboIndex < Owner.AttackStepCount - 1)
            {
                var nextState = Owner.GetState<AttackState>();
                nextState.SetComboIndex(comboIndex + 1);
                return nextState;
            }

            if (isInAttackState && IsAnimationComplete(0.98f))
            {
                Owner.ClearCurrentAttack();
                return Input.HasMovementInput
                    ? (Input.IsSprinting ? Owner.GetState<SprintState>() : Owner.GetState<WalkingState>())
                    : Owner.GetState<IdleState>();
            }

            return null;
        }

        public override void OnExit()
        {
            queuedNextAttack = false;
            recoveryElapsed = 0f;
            attackPushActive = false;
            attackPushElapsed = 0f;
            attackPushDuration = 0f;
            attackPushDecay = 0f;
            attackPushInitialSpeed = 0f;
            attackPushRemainingDistance = 0f;
        }

        public void OnAttackPhase(AttackPhase phase)
        {
            if (!TryGetCurrentAttackStep(out AttackStep step))
            {
                return;
            }

            if (!IsAnimatorInState(step.AnimationStateName))
            {
                return;
            }

            if (phase < currentPhase)
            {
                return;
            }

            hasPhaseEvents = true;
            currentPhase = phase;
            if (phase == AttackPhase.Recovery)
            {
                hasEnteredRecovery = true;
                recoveryElapsed = 0f;
                attackPushActive = false;
                attackPushElapsed = 0f;
                attackPushRemainingDistance = 0f;
            }
            else if (phase == AttackPhase.Slash)
            {
                UpdateAttackDirectionFromTransform();

                float distance = Owner.AttackForwardDistance;
                float duration = Owner.AttackPushDuration;
                if (distance <= 0f || duration <= 0f)
                {
                    attackPushActive = false;
                    return;
                }

                float endFraction = Mathf.Clamp(Owner.AttackPushEndSpeedFraction, 0.05f, 0.95f);
                attackPushDuration = duration;
                attackPushElapsed = 0f;
                attackPushRemainingDistance = distance;
                attackPushDecay = -Mathf.Log(endFraction) / duration;
                if (attackPushDecay > 0f)
                {
                    float denom = 1f - Mathf.Exp(-attackPushDecay * duration);
                    attackPushInitialSpeed = denom <= 0f ? (distance / duration) : (distance * attackPushDecay / denom);
                }
                else
                {
                    attackPushInitialSpeed = distance / duration;
                }

                attackPushActive = true;
            }
        }

        private void UpdateAttackDirectionFromTransform()
        {
            Vector3 forward = Owner.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.forward;
            }

            attackDirection = forward.normalized;
        }

        private bool TryGetCurrentAttackStep(out AttackStep step)
        {
            if (Owner.TryGetAttackStep(comboIndex, out step))
            {
                return true;
            }

            step = default;
            return false;
        }

        private void WarnIfMissingAttackEvents(AttackStep step)
        {
            if (hasPhaseEvents || hasLoggedMissingEventWarning)
            {
                return;
            }

            if (!IsAnimatorInState(step.AnimationStateName))
            {
                return;
            }

            if (GetAnimatorNormalizedTime() < 0.98f)
            {
                return;
            }

            hasLoggedMissingEventWarning = true;
            Debug.LogWarning(
                $"[AttackState] No attack phase events received for '{step.AnimationStateName}'. " +
                "Add animation events that call OnAttackWindup, OnAttackSlash, and OnAttackRecovery.",
                Animator);
        }
    }
}
