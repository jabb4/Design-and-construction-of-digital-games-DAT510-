namespace Combat
{
    public interface ICombatOutcomeFeedbackHook
    {
        void OnCombatOutcome(CombatOutcomeFeedbackContext context);
    }
}
