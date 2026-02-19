namespace Enemies.StateMachine.States
{
    using global::StateMachine.Core;

    public sealed class EnemyDefenseTurnState : EnemyStateBase
    {
        public override void OnEnter()
        {
            Intent?.ClearAllIntents();
        }

        public override void OnUpdate()
        {
            FaceTarget();
        }

        public override TransitionDecision EvaluateTransition()
        {
            if (Enemy == null || !Enemy.gameObject.activeInHierarchy)
            {
                return TransitionDecision.To(Owner.GetState<EnemyDeadState>(), TransitionReason.StandardFlow);
            }

            if (!Owner.TryRefreshTarget())
            {
                return TransitionDecision.To(Owner.GetState<EnemyIdleState>(), TransitionReason.StandardFlow);
            }

            return TransitionDecision.None;
        }
    }
}
