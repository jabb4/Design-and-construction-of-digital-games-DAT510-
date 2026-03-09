namespace Enemies.AI
{
    using global::StateMachine.Core;
    using UnityEngine;

    /// <summary>
    /// Enemy-facing intent source with the same semantics as player input intents.
    /// HFSM planners can write intents here each frame.
    /// </summary>
    public sealed class EnemyIntentSource : MonoBehaviour, IIntentSource
    {
        [SerializeField]
        [Tooltip("Minimum intent magnitude to be considered movement.")]
        private float moveThreshold = 0.1f;

        private BufferedIntentSource intents;

        public Vector2 MoveIntent => intents != null ? intents.MoveIntent : Vector2.zero;
        public bool HasMoveIntent => intents != null && intents.HasMoveIntent;
        public bool SprintHeld => intents != null && intents.SprintHeld;
        public bool BlockHeld => intents != null && intents.BlockHeld;
        public bool BlockPressed => intents != null && intents.BlockPressed;
        public bool JumpPressed => intents != null && intents.JumpPressed;
        public bool JumpBuffered => intents != null && intents.JumpBuffered;
        public bool AttackPressed => intents != null && intents.AttackPressed;
        public bool AttackBuffered => intents != null && intents.AttackBuffered;

        private void Awake()
        {
            intents = new BufferedIntentSource(moveThreshold);
        }

        private void LateUpdate()
        {
            if (intents == null)
            {
                return;
            }

            intents.ResetFrameState();
            intents.Tick(Time.deltaTime);
        }

        public void SetMoveIntent(Vector2 moveIntent)
        {
            intents?.SetMoveIntent(moveIntent);
        }

        public void SetSprintHeld(bool held)
        {
            intents?.SetSprintHeld(held);
        }

        public void SetBlockHeld(bool held)
        {
            intents?.SetBlockHeld(held);
        }

        public void PressJump(float bufferDurationSeconds = 0f)
        {
            intents?.PressJump(bufferDurationSeconds);
        }

        public void PressAttack(float bufferDurationSeconds = 0f)
        {
            intents?.PressAttack(bufferDurationSeconds);
        }

        public void ClearAllIntents()
        {
            intents?.Clear();
        }
    }
}
