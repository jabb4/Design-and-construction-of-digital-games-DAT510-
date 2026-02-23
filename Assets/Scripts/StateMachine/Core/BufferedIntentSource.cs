namespace StateMachine.Core
{
    using UnityEngine;

    /// <summary>
    /// Mutable intent source for AI/planner-driven actors.
    /// Mirrors player input semantics (held, pressed-this-frame, buffered).
    /// </summary>
    public sealed class BufferedIntentSource : IIntentSource
    {
        private readonly IntentBuffer blockIntent = new IntentBuffer();
        private readonly IntentBuffer jumpIntent = new IntentBuffer();
        private readonly IntentBuffer attackIntent = new IntentBuffer();
        private readonly float moveThresholdSquared;

        public BufferedIntentSource(float moveThreshold = 0.1f)
        {
            float threshold = Mathf.Max(0f, moveThreshold);
            moveThresholdSquared = threshold * threshold;
        }

        public Vector2 MoveIntent { get; private set; }
        public bool HasMoveIntent => MoveIntent.sqrMagnitude > moveThresholdSquared;
        public bool SprintHeld { get; private set; }
        public bool BlockHeld { get; private set; }
        public bool BlockPressed => blockIntent.IsPressedThisFrame;
        public bool JumpPressed => jumpIntent.IsPressedThisFrame;
        public bool JumpBuffered => jumpIntent.IsBuffered;
        public bool AttackPressed => attackIntent.IsPressedThisFrame;

        public void SetMoveIntent(Vector2 moveIntent)
        {
            MoveIntent = moveIntent;
        }

        public void SetSprintHeld(bool held)
        {
            SprintHeld = held;
        }

        public void SetBlockHeld(bool held)
        {
            if (held && !BlockHeld)
            {
                blockIntent.RecordPress();
            }

            BlockHeld = held;
        }

        public void PressJump(float bufferDurationSeconds = 0f)
        {
            jumpIntent.RecordPress(bufferDurationSeconds);
        }

        public void PressAttack(float bufferDurationSeconds = 0f)
        {
            attackIntent.RecordPress(bufferDurationSeconds);
        }

        public void Tick(float deltaTime)
        {
            jumpIntent.Tick(deltaTime);
            attackIntent.Tick(deltaTime);
        }

        public void ResetFrameState()
        {
            blockIntent.ResetFrameState();
            jumpIntent.ResetFrameState();
            attackIntent.ResetFrameState();
        }

        public void Clear()
        {
            MoveIntent = Vector2.zero;
            SprintHeld = false;
            BlockHeld = false;
            blockIntent.Clear();
            jumpIntent.Clear();
            attackIntent.Clear();
        }
    }
}
