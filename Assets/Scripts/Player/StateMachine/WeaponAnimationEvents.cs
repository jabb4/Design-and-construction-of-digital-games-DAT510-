namespace Player.StateMachine
{
    using UnityEngine;

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
        }

        public void OnEquipAnimationComplete()
        {
            stateMachine?.NotifyEquipAnimationComplete();
        }

        public void OnUnequipAnimationComplete()
        {
            stateMachine?.NotifyUnequipAnimationComplete();
        }
    }
}
