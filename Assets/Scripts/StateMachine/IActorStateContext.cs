namespace StateMachine.Core
{
    using UnityEngine;

    /// <summary>
    /// Runtime actor context for shared state-machine logic.
    /// </summary>
    public interface IActorStateContext
    {
        Transform ActorTransform { get; }
        bool IsGrounded { get; }
        bool IsLockedOn { get; }
        Transform LockOnTarget { get; }
        IIntentSource IntentSource { get; }

        void SetTimer(int key, float remainingSeconds);
        bool TryGetTimer(int key, out float remainingSeconds);
        void ClearTimer(int key);
    }
}
