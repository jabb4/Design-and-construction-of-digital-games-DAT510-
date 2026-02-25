namespace Enemies.Debug
{
    using System.Text;

    public readonly struct EnemyAIDebugSnapshot
    {
        public EnemyAIDebugSnapshot(
            string stateName,
            int? requiredParries,
            int? plannedChainLength,
            float? defenseTimeRemainingSeconds = null,
            float? counterPrepTimeRemainingSeconds = null,
            float? parryAttemptCooldownRemainingSeconds = null,
            float? nextAttackStartInSeconds = null)
        {
            StateName = stateName;
            RequiredParries = requiredParries;
            PlannedChainLength = plannedChainLength;
            DefenseTimeRemainingSeconds = defenseTimeRemainingSeconds;
            CounterPrepTimeRemainingSeconds = counterPrepTimeRemainingSeconds;
            ParryAttemptCooldownRemainingSeconds = parryAttemptCooldownRemainingSeconds;
            NextAttackStartInSeconds = nextAttackStartInSeconds;
        }

        public string StateName { get; }
        public int? RequiredParries { get; }
        public int? PlannedChainLength { get; }
        public float? DefenseTimeRemainingSeconds { get; }
        public float? CounterPrepTimeRemainingSeconds { get; }
        public float? ParryAttemptCooldownRemainingSeconds { get; }
        public float? NextAttackStartInSeconds { get; }
    }

    public static class EnemyAIDebugLabelFormatter
    {
        public static string Format(in EnemyAIDebugSnapshot snapshot)
        {
            var builder = new StringBuilder(96);
            builder.Append("State: ");
            builder.Append(string.IsNullOrWhiteSpace(snapshot.StateName) ? "None" : snapshot.StateName);

            if (snapshot.RequiredParries.HasValue)
            {
                builder.Append('\n');
                builder.Append("Parries Before Counter: ");
                builder.Append(snapshot.RequiredParries.Value);
            }

            if (snapshot.PlannedChainLength.HasValue)
            {
                builder.Append('\n');
                builder.Append("Planned Combo Attacks: ");
                builder.Append(snapshot.PlannedChainLength.Value);
            }

            AppendTimerLine(builder, "Defense Time Left", snapshot.DefenseTimeRemainingSeconds);
            AppendTimerLine(builder, "Counter Prep Left", snapshot.CounterPrepTimeRemainingSeconds);
            AppendTimerLine(builder, "Parry Retry In", snapshot.ParryAttemptCooldownRemainingSeconds);
            AppendTimerLine(builder, "Next Attack In", snapshot.NextAttackStartInSeconds);

            return builder.ToString();
        }

        private static void AppendTimerLine(StringBuilder builder, string label, float? seconds)
        {
            if (!seconds.HasValue)
            {
                return;
            }

            float clampedSeconds = seconds.Value < 0f ? 0f : seconds.Value;
            builder.Append('\n');
            builder.Append(label);
            builder.Append(": ");
            builder.Append(clampedSeconds.ToString("0.00"));
            builder.Append('s');
        }
    }
}
