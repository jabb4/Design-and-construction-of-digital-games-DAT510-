using UnityEngine;

namespace Combat
{
    public static class DamageResolver
    {
        private const float DefaultBlockMultiplier = 0.5f;

        public static DamageResolution ResolveDamage(float rawDamage, CombatFlagsComponent defenderFlags, float blockMultiplier = DefaultBlockMultiplier)
        {
            float incomingDamage = Mathf.Max(0f, rawDamage);
            if (incomingDamage <= 0f)
            {
                return new DamageResolution(DamageOutcome.Ignored, 0f);
            }

            if (defenderFlags == null)
            {
                return new DamageResolution(DamageOutcome.FullHit, incomingDamage);
            }

            if (!defenderFlags.IsVulnerable)
            {
                return new DamageResolution(DamageOutcome.Ignored, 0f);
            }

            if (defenderFlags.IsParryWindowActive)
            {
                return new DamageResolution(DamageOutcome.Parried, 0f);
            }

            if (defenderFlags.IsBlocking)
            {
                float applied = incomingDamage * Mathf.Clamp01(blockMultiplier);
                return new DamageResolution(DamageOutcome.Blocked, applied);
            }

            return new DamageResolution(DamageOutcome.FullHit, incomingDamage);
        }
    }
}
