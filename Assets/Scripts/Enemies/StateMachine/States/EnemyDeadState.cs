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
                Owner.ClearCurrentAttack();
                Owner.TryCrossFadeState("Idle", 0.05f);
            }
        }

        public override TransitionDecision EvaluateTransition()
        {
            return TransitionDecision.None;
        }
    }
}
