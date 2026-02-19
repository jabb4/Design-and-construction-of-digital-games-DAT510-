namespace Enemies.Animation
{
    using Enemies.StateMachine;
    using Player.StateMachine;
    using UnityEngine;

    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyStateMachine))]
    public sealed class EnemyAnimationEvents : MonoBehaviour
    {
        [SerializeField] private EnemyStateMachine stateMachine;

        private void Awake()
        {
            if (stateMachine == null)
            {
                stateMachine = GetComponent<EnemyStateMachine>();
            }
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
