namespace Player.StateMachine
{
    using UnityEngine;
    using Player.StateMachine.States;

    public partial class PlayerStateMachine
    {
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private bool slowMotionEnabled;
        [SerializeField, Range(0.05f, 1f)] private float slowMotionScale = 0.2f;

        private void OnValidate()
        {
            UpdateTimeScale();
        }

        private void OnDestroy()
        {
            if (!slowMotionEnabled)
            {
                return;
            }

            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
        }

        private void UpdateTimeScale()
        {
            if (!slowMotionEnabled)
            {
                return;
            }

            float clampedScale = Mathf.Clamp(slowMotionScale, 0.01f, 1f);
            Time.timeScale = clampedScale;
            Time.fixedDeltaTime = 0.02f * clampedScale;
        }

        private void OnGUI()
        {
            if (!showDebugInfo)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.BeginVertical("box");

            GUILayout.Label("<b>Player State Machine</b>");
            GUILayout.Space(5);

            GUILayout.Label($"State: <b>{CurrentStateName}</b>");
            GUILayout.Label($"Equipped: <b>{IsEquipped}</b>");
            GUILayout.Label($"Transitioning: <b>{IsTransitioningWeapon}</b>");

            if (CurrentState is AttackState attackState)
            {
                GUILayout.Label($"Attack Phase: <b>{attackState.CurrentPhase}</b>");
            }

            GUILayout.Label($"Slow Motion: <b>{(slowMotionEnabled ? $"On ({slowMotionScale:0.00}x)" : "Off")}</b>");

            bool isLockedOn = CameraController != null && CameraController.IsLockedOn;

            if (hasPendingUnequipRequest)
            {
                if (isLockedOn || requestedEquipWhilePending)
                {
                    GUILayout.Label("Unequip: <b>paused</b>");
                }
                else
                {
                    GUILayout.Label($"Unequip in: <b>{unequipTimer:F1}s</b>");
                }
            }

            GUILayout.Label($"Locked On: <b>{isLockedOn}</b>");

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}
