using UnityEngine;

namespace Combat
{
    public enum CombatAttackPhase
    {
        Windup,
        Slash,
        Recovery
    }

    public struct CombatAttackFeedbackContext
    {
        public CombatAttackPhase Phase;
        public AttackData? Attack;
        public ICombatant Attacker;
        public Vector3 AttackDirection;
    }
}
