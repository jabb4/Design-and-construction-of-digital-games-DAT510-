namespace Combat
{
    public enum DamageOutcome
    {
        Ignored,
        FullHit,
        Blocked,
        Parried
    }

    public readonly struct DamageResolution
    {
        public DamageResolution(DamageOutcome outcome, float appliedDamage)
        {
            Outcome = outcome;
            AppliedDamage = appliedDamage;
        }

        public DamageOutcome Outcome { get; }
        public float AppliedDamage { get; }
    }
}
