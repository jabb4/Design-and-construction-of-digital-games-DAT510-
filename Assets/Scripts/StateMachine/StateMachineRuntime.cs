namespace StateMachine.Core
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Shared runtime loop for state-driven actors.
    /// Handles transition evaluation, state updates, and transition lifecycle events.
    /// </summary>
    public sealed class StateMachineRuntime
    {
        private readonly List<ITransitionLayer> transitionLayers = new List<ITransitionLayer>(4);
        private readonly List<ITransitionLayer> stateScopedLayers = new List<ITransitionLayer>(4);

        public event Action<IState, IState> StateChanging;
        public event Action<IState, IState> StateChanged;

        public IState CurrentState { get; private set; }
        public string CurrentStateName => CurrentState?.StateName ?? "None";

        public void Tick()
        {
            TransitionDecision decision = EvaluateTransition();

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

            PerformStateChange(newState);
        }

        /// <summary>
        /// Forces a state change even when the target is the same as the current state.
        /// Use when external systems (e.g., weapon transitions) need to re-initialize
        /// the current state after temporarily suspending the state machine.
        /// </summary>
        public void ForceChangeState(IState newState)
        {
            if (newState == null)
            {
                return;
            }

            PerformStateChange(newState);
        }

        private void PerformStateChange(IState newState)
        {
            IState previous = CurrentState;
            StateChanging?.Invoke(previous, newState);

            previous?.OnExit();
            CurrentState = newState;
            CurrentState.OnEnter();

            StateChanged?.Invoke(previous, CurrentState);
        }

        public void AddTransitionLayer(ITransitionLayer layer)
        {
            if (layer == null || transitionLayers.Contains(layer))
            {
                return;
            }

            transitionLayers.Add(layer);
            transitionLayers.Sort((left, right) => left.EvaluationOrder.CompareTo(right.EvaluationOrder));
        }

        public void RemoveTransitionLayer(ITransitionLayer layer)
        {
            if (layer == null)
            {
                return;
            }

            transitionLayers.Remove(layer);
        }

        private TransitionDecision EvaluateTransition()
        {
            if (CurrentState == null)
            {
                return TransitionDecision.None;
            }

            TransitionDecision decision = CurrentState.EvaluateTransition();

            for (int i = 0; i < transitionLayers.Count; i++)
            {
                decision = SelectHigherPriority(decision, transitionLayers[i].Evaluate(CurrentState));
            }

            if (CurrentState is ITransitionLayerSource scopedLayerSource)
            {
                stateScopedLayers.Clear();
                scopedLayerSource.CollectTransitionLayers(stateScopedLayers);
                stateScopedLayers.Sort((left, right) => left.EvaluationOrder.CompareTo(right.EvaluationOrder));

                for (int i = 0; i < stateScopedLayers.Count; i++)
                {
                    decision = SelectHigherPriority(decision, stateScopedLayers[i].Evaluate(CurrentState));
                }
            }

            return decision;
        }

        private static TransitionDecision SelectHigherPriority(TransitionDecision current, TransitionDecision candidate)
        {
            if (!candidate.HasTransition)
            {
                return current;
            }

            if (!current.HasTransition || candidate.Priority > current.Priority)
            {
                return candidate;
            }

            return current;
        }
    }
}
