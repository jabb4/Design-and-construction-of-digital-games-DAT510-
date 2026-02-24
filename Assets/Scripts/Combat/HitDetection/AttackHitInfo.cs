using UnityEngine;

namespace Combat
{
    public struct AttackHitInfo
    {
        public float Damage;
        public ICombatant Attacker;
        public AttackData? Attack;
        public Vector3 HitPoint;
        public Vector3 HitNormal;
    }
}
