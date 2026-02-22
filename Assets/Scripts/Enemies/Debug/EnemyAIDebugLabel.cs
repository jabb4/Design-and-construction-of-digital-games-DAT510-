namespace Enemies.Debug
{
    using Enemies.StateMachine;
    using Enemies.StateMachine.States;
    using UnityEngine;
    using UnityEngine.InputSystem;

    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyStateMachine))]
    public sealed class EnemyAIDebugLabel : MonoBehaviour
    {
        [Header("Visibility")]
        [SerializeField] private bool visibleOnStart = true;
        [SerializeField] private Key toggleKey = Key.K;

        [Header("World Anchor")]
        [SerializeField, Min(0f)] private float worldOffset = 0.35f;

        [Header("Label")]
        [SerializeField, Min(0.02f)] private float textRefreshInterval = 0.1f;
        [SerializeField, Min(8f)] private float fontSize = 16f;
        [SerializeField, Min(120f)] private float maxLabelWidth = 260f;
        [SerializeField] private Vector2 screenPixelOffset = new Vector2(0f, -8f);
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.65f);
        [SerializeField] private float labelScale = 0.1f;

        private static bool labelsVisible = true;
        private static bool visibilityInitialized;
        private static int lastToggleFrame = -1;
        private static Texture2D backgroundTexture;

        private EnemyStateMachine stateMachine;
        private Animator cachedAnimator;
        private Transform headAnchor;
        private Camera cachedCamera;
        private float nextTextRefreshAt;
        private string cachedLabelText = string.Empty;
        private GUIStyle labelStyle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            labelsVisible = true;
            visibilityInitialized = false;
            lastToggleFrame = -1;
            backgroundTexture = null;
        }

        private void Awake()
        {
            stateMachine = GetComponent<EnemyStateMachine>();
            cachedAnimator = GetComponent<Animator>();
            headAnchor = ResolveHeadAnchor();
            RefreshLabelText();
        }

        private void OnEnable()
        {
            if (!visibilityInitialized)
            {
                labelsVisible = visibleOnStart;
                visibilityInitialized = true;
            }

            nextTextRefreshAt = Time.time;
        }

        private void OnValidate()
        {
            textRefreshInterval = Mathf.Max(0.02f, textRefreshInterval);
            worldOffset = Mathf.Max(0f, worldOffset);
            fontSize = Mathf.Max(8f, fontSize);
            maxLabelWidth = Mathf.Max(120f, maxLabelWidth);
            labelScale = Mathf.Max(0.01f, labelScale);
            UpdateGuiStyle();
        }

        private void Update()
        {
            HandleGlobalToggleInput();

            if (Time.time >= nextTextRefreshAt)
            {
                RefreshLabelText();
                nextTextRefreshAt = Time.time + textRefreshInterval;
            }
        }

        private void OnGUI()
        {
            if (!labelsVisible || !enabled || !gameObject.activeInHierarchy)
            {
                return;
            }

            if (Event.current == null || Event.current.type != EventType.Repaint)
            {
                return;
            }

            Camera activeCamera = ResolveCamera();
            if (activeCamera == null)
            {
                return;
            }

            Transform anchor = ResolveActiveAnchor();
            Vector3 worldAnchor = anchor.position + Vector3.up * worldOffset;
            Vector3 screenPoint = activeCamera.WorldToScreenPoint(worldAnchor);
            if (screenPoint.z <= 0f)
            {
                return;
            }

            EnsureGuiResources();

            float screenX = screenPoint.x + screenPixelOffset.x;
            float screenY = (Screen.height - screenPoint.y) + screenPixelOffset.y;

            var content = new GUIContent(cachedLabelText);
            float width = maxLabelWidth;
            float height = labelStyle.CalcHeight(content, width);

            var rect = new Rect(screenX - (width * 0.5f), screenY - height, width, height);
            DrawBackground(rect, backgroundColor);
            GUI.Label(rect, content, labelStyle);
        }

        private void HandleGlobalToggleInput()
        {
            if (toggleKey == Key.None || Time.frameCount == lastToggleFrame)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            var keyControl = keyboard[toggleKey];
            if (keyControl == null || !keyControl.wasPressedThisFrame)
            {
                return;
            }

            labelsVisible = !labelsVisible;
            lastToggleFrame = Time.frameCount;
        }

        private void RefreshLabelText()
        {
            EnemyAIDebugSnapshot snapshot = BuildSnapshot();
            cachedLabelText = EnemyAIDebugLabelFormatter.Format(snapshot);
        }

        private EnemyAIDebugSnapshot BuildSnapshot()
        {
            string stateName = stateMachine != null ? stateMachine.CurrentStateName : "None";
            int? requiredParries = null;
            int? plannedChainLength = null;
            float? defenseTimeRemainingSeconds = null;
            float? counterPrepTimeRemainingSeconds = null;
            float? parryAttemptCooldownRemainingSeconds = null;
            float? nextAttackStartInSeconds = null;

            if (stateMachine != null)
            {
                if (stateMachine.CurrentState is EnemyDefenseTurnState defenseTurnState)
                {
                    requiredParries = defenseTurnState.RequiredParries;
                    defenseTimeRemainingSeconds = defenseTurnState.DefenseTimeRemainingSeconds;
                    counterPrepTimeRemainingSeconds = defenseTurnState.CounterPrepTimeRemainingSeconds;
                    parryAttemptCooldownRemainingSeconds = defenseTurnState.ParryAttemptCooldownRemainingSeconds;
                }
                else if (stateMachine.CurrentState is EnemyAttackTurnState attackTurnState)
                {
                    plannedChainLength = attackTurnState.PlannedChainLength;
                    nextAttackStartInSeconds = attackTurnState.NextAttackStartInSeconds;
                }
            }

            return new EnemyAIDebugSnapshot(
                stateName,
                requiredParries,
                plannedChainLength,
                defenseTimeRemainingSeconds,
                counterPrepTimeRemainingSeconds,
                parryAttemptCooldownRemainingSeconds,
                nextAttackStartInSeconds);
        }

        private Transform ResolveActiveAnchor()
        {
            if (headAnchor == null || !headAnchor.gameObject.activeInHierarchy)
            {
                headAnchor = ResolveHeadAnchor();
            }

            return headAnchor != null ? headAnchor : transform;
        }

        private Transform ResolveHeadAnchor()
        {
            if (cachedAnimator == null)
            {
                cachedAnimator = GetComponent<Animator>();
            }

            if (cachedAnimator != null && cachedAnimator.isHuman)
            {
                Transform humanHead = cachedAnimator.GetBoneTransform(HumanBodyBones.Head);
                if (humanHead != null)
                {
                    return humanHead;
                }

                Transform humanNeck = cachedAnimator.GetBoneTransform(HumanBodyBones.Neck);
                if (humanNeck != null)
                {
                    return humanNeck;
                }
            }

            Transform[] allTransforms = GetComponentsInChildren<Transform>(true);
            Transform fallbackNeck = null;

            for (int i = 0; i < allTransforms.Length; i++)
            {
                Transform candidate = allTransforms[i];
                if (candidate == null)
                {
                    continue;
                }

                string lowerName = candidate.name.ToLowerInvariant();
                if (lowerName.Contains("head"))
                {
                    return candidate;
                }

                if (fallbackNeck == null && lowerName.Contains("neck"))
                {
                    fallbackNeck = candidate;
                }
            }

            return fallbackNeck;
        }

        private Camera ResolveCamera()
        {
            if (cachedCamera != null && cachedCamera.isActiveAndEnabled)
            {
                return cachedCamera;
            }

            cachedCamera = Camera.main;
            if (cachedCamera != null && cachedCamera.isActiveAndEnabled)
            {
                return cachedCamera;
            }

            Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            for (int i = 0; i < cameras.Length; i++)
            {
                Camera candidate = cameras[i];
                if (candidate != null && candidate.isActiveAndEnabled)
                {
                    cachedCamera = candidate;
                    return cachedCamera;
                }
            }

            return null;
        }

        private void EnsureGuiResources()
        {
            if (backgroundTexture == null)
            {
                backgroundTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
                {
                    name = "EnemyAIDebugLabelBg"
                };
                backgroundTexture.SetPixel(0, 0, Color.white);
                backgroundTexture.Apply();
            }

            UpdateGuiStyle();
        }

        private void UpdateGuiStyle()
        {
            if (labelStyle == null)
            {
                labelStyle = new GUIStyle();
                labelStyle.alignment = TextAnchor.UpperCenter;
                labelStyle.wordWrap = true;
                labelStyle.richText = false;
                labelStyle.padding = new RectOffset(6, 6, 4, 4);
            }

            labelStyle.fontSize = Mathf.RoundToInt(fontSize);
            labelStyle.normal.textColor = textColor;
        }

        private static void DrawBackground(Rect rect, Color color)
        {
            if (backgroundTexture == null)
            {
                return;
            }

            Color previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, backgroundTexture, ScaleMode.StretchToFill);
            GUI.color = previousColor;
        }
    }
}
