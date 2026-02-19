namespace StateMachine.Core
{
    using System;

    /// <summary>
    /// Shared runtime loop for state-driven actors.
    /// Handles transition evaluation, state updates, and transition lifecycle events.
    /// </summary>
    public sealed class StateMachineRuntime
    {
        public event Action<IState, IState> StateChanging;
        public event Action<IState, IState> StateChanged;

        public IState CurrentState { get; private set; }
        public string CurrentStateName => CurrentState?.StateName ?? "None";

        public void Tick()
        {
            TransitionDecision decision = CurrentState != null
                ? CurrentState.EvaluateTransition()
                : TransitionDecision.None;

            if (decision.HasTransition && decision.NextState != CurrentState)
            {
                ChangeState(decision.NextState);
            }

            CurrentState?.OnUpdate();
        }

        public void FixedTick()
        {
            CurrentState?.OnFixedUpdate();
        }

        public void ChangeState(IState newState)
        {
            if (newState == null || CurrentState == newState)
            {
                return;
            }

            IState previous = CurrentState;
            StateChanging?.Invoke(previous, newState);

            previous?.OnExit();
            CurrentState = newState;
            CurrentState.OnEnter();

            StateChanged?.Invoke(previous, CurrentState);
        }
    }
}
