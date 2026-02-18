namespace Combat
{
    public interface ICombatAttackFeedbackHook
    {
        void OnCombatAttackPhase(CombatAttackFeedbackContext context);
    }
}
