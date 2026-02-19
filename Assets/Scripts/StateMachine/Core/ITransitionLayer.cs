namespace StateMachine.Core
{
    using System.Collections.Generic;

    /// <summary>
    /// A transition source evaluated alongside the active state's own transition check.
    /// </summary>
    public interface ITransitionLayer
    {
        int EvaluationOrder { get; }
        TransitionDecision Evaluate(IState currentState);
    }

    /// <summary>
    /// Optional state extension point for exposing child/parent transition layers at runtime.
    /// </summary>
    public interface ITransitionLayerSource
    {
        void CollectTransitionLayers(List<ITransitionLayer> destination);
    }
}
