namespace Player.StateMachine
{
    public partial class PlayerStateMachine
    {
        public GuardSide CurrentGuardSide { get; private set; } = GuardSide.Left;
        public bool IsDefenseReactionActive { get; private set; }

        public void SetGuardSide(GuardSide side)
        {
            CurrentGuardSide = side;
        }

        public void SetDefenseReactionActive(bool isActive)
        {
            IsDefenseReactionActive = isActive;
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
