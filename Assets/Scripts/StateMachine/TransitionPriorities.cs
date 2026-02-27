namespace StateMachine.Core
{
    /// <summary>
    /// Shared transition priority policy.
    /// Higher values win when multiple transition candidates are evaluated in the same tick.
    /// </summary>
    public static class TransitionPriorities
    {
        public const int Default = 0;
        public const int InputSecondary = 15;
        public const int InputPrimary = 20;
        public const int AirStateSync = 25;
        public const int RecoveryInterrupt = 30;
        public const int ComboContinuation = 40;
        public const int CriticalFallback = 100;
    }
}
