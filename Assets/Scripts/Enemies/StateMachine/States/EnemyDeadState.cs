namespace Enemies.StateMachine.States
{
    using global::StateMachine.Core;

    public sealed class EnemyDeadState : EnemyStateBase
    {
        public override void OnEnter()
        {
            Intent?.ClearAllIntents();
        }

        public override TransitionDecision EvaluateTransition()
        {
            return TransitionDecision.None;
        }
    }
}
