namespace Enemies.StateMachine.States
{
    using global::StateMachine.Core;

    public sealed class EnemyDeadState : EnemyStateBase
    {
        public override void OnEnter()
        {
            Intent?.ClearAllIntents();
            Enemy?.CloseParryWindow();
            if (Owner != null)
            {
                Owner.NavBridge?.Stop();
                Owner.ClearCurrentAttack();
            }
        }

        public override TransitionDecision EvaluateTransition()
        {
            return TransitionDecision.None;
        }
    }
}
