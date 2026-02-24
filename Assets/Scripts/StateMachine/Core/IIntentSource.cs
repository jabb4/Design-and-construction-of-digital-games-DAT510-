namespace StateMachine.Core
{
    using UnityEngine;

    /// <summary>
    /// Input/intent surface consumed by state logic.
    /// Player input and AI planners should both map into this contract.
    /// </summary>
    public interface IIntentSource
    {
        Vector2 MoveIntent { get; }
        bool HasMoveIntent { get; }
        bool SprintHeld { get; }
        bool BlockHeld { get; }
        bool BlockPressed { get; }
        bool JumpPressed { get; }
        bool JumpBuffered { get; }
        bool AttackPressed { get; }
    }
}
