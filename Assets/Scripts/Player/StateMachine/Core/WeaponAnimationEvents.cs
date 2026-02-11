namespace Player.StateMachine
{
    using UnityEngine;
    using Player.Combat;

    /// <summary>
    /// Receives animation events for weapon transitions and notifies the state machine.
    /// Attach this to the same GameObject as the Animator.
    /// </summary>
    public class WeaponAnimationEvents : MonoBehaviour
    {
        [SerializeField] private PlayerStateMachine stateMachine;

        private void Awake()
        {
            if (stateMachine == null)
            {
                stateMachine = GetComponent<PlayerStateMachine>();
            }

            if (GetComponent<PlayerAttackHitboxDriver>() == null)
            {
                gameObject.AddComponent<PlayerAttackHitboxDriver>();
            }
        }

        public void OnEquipAnimationComplete()
        {
            stateMachine?.NotifyEquipAnimationComplete();
        }

        public void OnUnequipAnimationComplete()
        {
            stateMachine?.NotifyUnequipAnimationComplete();
        }

        public void OnAttackWindup()
        {
            stateMachine?.NotifyAttackPhase(AttackPhase.Windup);
        }

        public void OnAttackSlash()
        {
            stateMachine?.NotifyAttackPhase(AttackPhase.Slash);
        }

        public void OnAttackRecovery()
        {
            stateMachine?.NotifyAttackPhase(AttackPhase.Recovery);
        }
    }
}
