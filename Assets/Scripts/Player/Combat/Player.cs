using UnityEngine;
using Combat;

namespace Player.Combat
{
    public class Player : MonoBehaviour, ICombatant
    {
        [SerializeField] private bool isVulnerable = true;

        public CombatTeam Team => CombatTeam.Player;
        public bool IsVulnerable => isVulnerable;
        public bool IsAttacking { get; set; }

        public void ReceiveHit(AttackHitInfo hit)
        {
        }
    }
}
