namespace StateMachine.Core
{
    using UnityEngine;

    /// <summary>
    /// Reusable press-plus-buffer intent helper for state-driven input and AI plans.
    /// </summary>
    public sealed class IntentBuffer
    {
        private bool pressedThisFrame;
        private float bufferTimer;

        public bool IsPressedThisFrame => pressedThisFrame;
        public bool IsBuffered => bufferTimer > 0f;

        public void RecordPress(float bufferDurationSeconds = 0f)
        {
            pressedThisFrame = true;
            if (bufferDurationSeconds > 0f)
            {
                bufferTimer = Mathf.Max(bufferTimer, bufferDurationSeconds);
            }
        }

        public void Tick(float deltaTime)
        {
            if (bufferTimer <= 0f)
            {
                return;
            }

            bufferTimer -= deltaTime;
            if (bufferTimer < 0f)
            {
                bufferTimer = 0f;
            }
        }

        public void ResetFrameState()
        {
            pressedThisFrame = false;
        }

        public bool ConsumeBuffered()
        {
            if (bufferTimer <= 0f)
            {
                return false;
            }

            bufferTimer = 0f;
            return true;
        }

        public void Clear()
        {
            pressedThisFrame = false;
            bufferTimer = 0f;
        }
    }
}
