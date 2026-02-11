using UnityEngine;
using Player.StateMachine;

namespace Combat
{
    public struct AttackHitInfo
    {
        public float Damage;
        public ICombatant Attacker;
        public AttackStep? AttackStep;
        public Vector3 HitPoint;
    }
}
