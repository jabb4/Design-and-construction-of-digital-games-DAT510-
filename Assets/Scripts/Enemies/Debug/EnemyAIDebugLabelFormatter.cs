namespace Enemies.Debug
{
    using System.Text;
    using Player.StateMachine;

    public readonly struct EnemyAIDebugSnapshot
    {
        public EnemyAIDebugSnapshot(string stateName, AttackPhase attackPhase, int? requiredParries, int? plannedChainLength)
        {
            StateName = stateName;
            AttackPhase = attackPhase;
            RequiredParries = requiredParries;
            PlannedChainLength = plannedChainLength;
        }

        public string StateName { get; }
        public AttackPhase AttackPhase { get; }
        public int? RequiredParries { get; }
        public int? PlannedChainLength { get; }
    }

    public static class EnemyAIDebugLabelFormatter
    {
        public static string Format(in EnemyAIDebugSnapshot snapshot)
        {
            var builder = new StringBuilder(96);
            builder.Append("State: ");
            builder.Append(string.IsNullOrWhiteSpace(snapshot.StateName) ? "None" : snapshot.StateName);
            builder.Append('\n');
            builder.Append("Attack Phase: ");
            builder.Append(snapshot.AttackPhase);

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

            return builder.ToString();
        }
    }
}
