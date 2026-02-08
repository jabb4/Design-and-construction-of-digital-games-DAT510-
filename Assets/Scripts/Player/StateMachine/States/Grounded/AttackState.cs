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
        private float recoveryElapsed;
        private Vector3 attackDirection;
        private float attackPushSpeed;
        private bool attackPushReady;

        public AttackPhase CurrentPhase => currentPhase;

        public void SetComboIndex(int index)
        {
            comboIndex = Mathf.Clamp(index, 0, AttackComboDefinition.Attacks.Length - 1);
        }

        public override void OnEnter()
        {
            queuedNextAttack = false;
            currentPhase = AttackPhase.Windup;
            hasEnteredRecovery = false;
            hasPhaseEvents = false;
            recoveryElapsed = 0f;
            attackPushSpeed = 0f;
            attackPushReady = false;

            UpdateAttackDirectionFromTransform();

            AttackStep step = AttackComboDefinition.Attacks[comboIndex];
            Owner.SetCurrentAttack(step);

            Animator.SetBool(IsMovingHash, false);
            Animator.SetBool(IsSprintingHash, false);

            CrossFade(step.AnimationStateName, 0.1f);
        }

        public override void OnUpdate()
        {
            AttackStep step = AttackComboDefinition.Attacks[comboIndex];
            float normalizedTime = GetAnimatorNormalizedTime();

            UpdatePhaseFromTime(step, normalizedTime);

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

            if (currentPhase == AttackPhase.Slash && attackPushReady)
            {
                Motor.SetHorizontalVelocity(attackDirection * attackPushSpeed);
            }
            else
            {
                Motor.Move(Vector2.zero, useSprint: false);
            }
        }

        public override IState CheckTransitions()
        {
            AttackStep step = AttackComboDefinition.Attacks[comboIndex];
            float normalizedTime = GetAnimatorNormalizedTime();

            UpdatePhaseFromTime(step, normalizedTime);

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
            bool isComboWindowOpen = hasEnteredRecovery || (!hasPhaseEvents && isInAttackState && normalizedTime >= step.ComboWindowStart);
            if (isComboWindowOpen && queuedNextAttack && comboIndex < AttackComboDefinition.Attacks.Length - 1)
            {
                var nextState = Owner.GetState<AttackState>();
                nextState.SetComboIndex(comboIndex + 1);
                return nextState;
            }

            if (isInAttackState && normalizedTime >= step.ExitTime)
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
            attackPushSpeed = 0f;
            attackPushReady = false;
        }

        public void OnAttackPhase(AttackPhase phase)
        {
            AttackStep step = AttackComboDefinition.Attacks[comboIndex];
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
                attackPushReady = false;
            }
            else if (phase == AttackPhase.Slash)
            {
                UpdateAttackDirectionFromTransform();

                float slashWindow = step.RecoveryStartTime - step.SlashStartTime;
                if (slashWindow <= 0f)
                {
                    attackPushReady = false;
                    return;
                }

                float clipLength = Animator.GetCurrentAnimatorStateInfo(0).length;
                if (clipLength <= 0f)
                {
                    attackPushReady = false;
                    return;
                }

                float slashDuration = Mathf.Max(Owner.AttackMinSlashDuration, slashWindow * clipLength);
                attackPushSpeed = Owner.AttackForwardDistance / slashDuration;
                attackPushReady = true;
            }
        }

        private void UpdatePhaseFromTime(AttackStep step, float normalizedTime)
        {
            if (hasPhaseEvents || !IsAnimatorInState(step.AnimationStateName))
            {
                return;
            }

            if (normalizedTime >= step.RecoveryStartTime)
            {
                OnAttackPhase(AttackPhase.Recovery);
                return;
            }

            if (normalizedTime >= step.SlashStartTime)
            {
                OnAttackPhase(AttackPhase.Slash);
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
    }
}
