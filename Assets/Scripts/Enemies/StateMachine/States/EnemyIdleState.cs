namespace Enemies.StateMachine.States
{
    using global::StateMachine.Core;

    public sealed class EnemyIdleState : EnemyStateBase
    {
        public override void OnEnter()
        {
            Intent?.ClearAllIntents();
            Enemy?.CloseParryWindow();
            Owner?.NavBridge?.Stop();
            Owner?.TryCrossFadeState("Idle", 0.15f);
        }

        public override void OnUpdate()
        {
            FaceTarget();
        }

        public override TransitionDecision EvaluateTransition()
        {
            if (TryTransitionDead(out TransitionDecision deadTransition))
            {
                return deadTransition;
            }

            if (Owner.TryRefreshTarget())
            {
                return TransitionDecision.To(Owner.GetState<EnemyDefenseTurnState>(), TransitionReason.StandardFlow);
            }

            return TransitionDecision.None;
        }
    }
}
