using UnityEngine;

namespace Combat
{
    public struct CombatOutcomeFeedbackContext
    {
        public AttackHitInfo Hit;
        public DamageResolution Resolution;
        public ICombatant Defender;
        public Vector3 DefenderPushDirection;
        public Vector3 HitPoint;
    }
}
