namespace Combat
{
    public interface ICombatant
    {
        CombatTeam Team { get; }
        bool IsVulnerable { get; }
        bool IsAttacking { get; }
        void ReceiveHit(AttackHitInfo hit);
    }
}
