namespace Player.StateMachine
{
    using UnityEngine;

    public partial class PlayerStateMachine
    {
        public GuardSide CurrentGuardSide { get; private set; } = GuardSide.Left;
        public bool IsDefenseReactionActive { get; private set; }
        public bool IsDefenseAttackUnlocked =>
            !IsDefenseReactionActive || Time.time >= defenseAttackUnlockTime;

        private float defenseAttackUnlockTime = float.NegativeInfinity;

        public void SetGuardSide(GuardSide side)
        {
            CurrentGuardSide = side;
        }

        public void SetDefenseReactionActive(bool isActive)
        {
            IsDefenseReactionActive = isActive;
            if (!isActive)
            {
                defenseAttackUnlockTime = float.NegativeInfinity;
            }
        }

        public void BeginDefenseReaction(float attackUnlockDelaySeconds)
        {
            IsDefenseReactionActive = true;
            float delay = Mathf.Max(0f, attackUnlockDelaySeconds);
            defenseAttackUnlockTime = Time.time + delay;
        }

        public void EndDefenseReaction()
        {
            IsDefenseReactionActive = false;
            defenseAttackUnlockTime = float.NegativeInfinity;
        }

        public string GetDefenseEnterStateName()
        {
            return GetDefenseEnterStateName(CurrentGuardSide);
        }

        public string GetDefenseEnterStateName(GuardSide side)
        {
            return side == GuardSide.Right ? "Idle2DefenseR" : "Idle2DefenseL";
        }

        public string GetDefenseIdleStateName()
        {
            return GetDefenseIdleStateName(CurrentGuardSide);
        }

        public string GetDefenseIdleStateName(GuardSide side)
        {
            return side == GuardSide.Right ? "DefenseIdleR" : "DefenseIdle";
        }

        public string GetDefenseExitStateName()
        {
            return GetDefenseExitStateName(CurrentGuardSide);
        }

        public string GetDefenseExitStateName(GuardSide side)
        {
            return side == GuardSide.Right ? "Defense2IdleR" : "Defense2IdleL";
        }

        public string GetDefenseBlockReactionStateName()
        {
            return CurrentGuardSide == GuardSide.Right ? "DefenseRBlock" : "DefenseLBlock";
        }
    }
}
