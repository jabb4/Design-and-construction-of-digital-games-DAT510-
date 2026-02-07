namespace Player.StateMachine.States
{
    using UnityEngine;
    using Player.StateMachine;

    public sealed class AttackState : PlayerStateBase, IAttackPhaseListener
    {
        private int comboIndex;
        private bool queuedNextAttack;
        private AttackPhase currentPhase;
        private bool hasEnteredRecovery;
        private bool hasPhaseEvents;

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
        }

        public override void OnFixedUpdate()
        {
            Motor.Move(Vector2.zero, useSprint: false);
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
    }
}
