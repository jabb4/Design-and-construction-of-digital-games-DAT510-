using Combat;
using UnityEngine;
using UnityEngine.InputSystem;

//Use this Health Bar by simply attaching the script to the enemy prefab

[DisallowMultipleComponent]
[RequireComponent(typeof(HealthComponent))]
public sealed class EnemyHealthBar : MonoBehaviour
{
    [Header("Visibility")]
    [SerializeField] private bool visibleOnStart = true;
    [SerializeField] private Key toggleKey = Key.H;

    [Header("World Anchor")]
    [SerializeField, Min(0f)] private float worldOffset = 0.5f;

    [Header("Health Bar")]
    [SerializeField, Min(10f)] private float barWidth = 100f;
    [SerializeField, Min(4f)] private float barHeight = 12f;
    [SerializeField] private Vector2 screenPixelOffset = new Vector2(0f, -8f);
    [SerializeField] private Color fillColor = new Color(1f, 0f, 0f, 0.85f);
    [SerializeField] private Color backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.85f);
    [SerializeField] private Color borderColor = new Color(0f, 0f, 0f, 1f);
    [SerializeField, Min(1f)] private float borderWidth = 2f;

    private static bool barsVisible = true;
    private static bool visibilityInitialized;
    private static int lastToggleFrame = -1;
    private static Texture2D fillTexture;
    private static Texture2D backgroundTexture;
    private static Texture2D borderTexture;

    private Transform headAnchor;
    private Animator cachedAnimator;
    private Camera cachedCamera;
    private HealthComponent healthComponent;
    private float maxHealth;
    private float currentHealth;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        barsVisible = true;
        visibilityInitialized = false;
        lastToggleFrame = -1;
        fillTexture = null;
        backgroundTexture = null;
        borderTexture = null;
    }

    private void Awake()
    {
        cachedAnimator = GetComponent<Animator>();
        headAnchor = ResolveHeadAnchor();
        healthComponent = GetComponent<HealthComponent>();
        
        if (healthComponent != null)
        {
            maxHealth = healthComponent.MaxHealth;
            currentHealth = healthComponent.CurrentHealth;
        }
    }

    private void OnEnable()
    {
        if (!visibilityInitialized)
        {
            barsVisible = visibleOnStart;
            visibilityInitialized = true;
        }

        if (healthComponent != null)
        {
            healthComponent.OnDamaged += HandleDamaged;
        }
    }

    private void OnDisable()
    {
        if (healthComponent != null)
        {
            healthComponent.OnDamaged -= HandleDamaged;
        }
    }

    private void OnValidate()
    {
        worldOffset = Mathf.Max(0f, worldOffset);
        barWidth = Mathf.Max(10f, barWidth);
        barHeight = Mathf.Max(4f, barHeight);
        borderWidth = Mathf.Max(1f, borderWidth);
    }

    private void HandleDamaged(float damage, float newCurrentHealth, float newMaxHealth)
    {
        maxHealth = newMaxHealth;
        currentHealth = newCurrentHealth;
    }

    private void Update()
    {
        HandleGlobalToggleInput();
    }

    private void OnGUI()
    {
        if (!barsVisible || !enabled || !gameObject.activeInHierarchy)
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

        DrawHealthBar(screenX, screenY);
    }

    private void DrawHealthBar(float centerX, float centerY)
    {
        float x = centerX - (barWidth * 0.5f);
        float y = centerY;

        // Draw border
        var borderRect = new Rect(x - borderWidth, y - borderWidth, barWidth + (borderWidth * 2f), barHeight + (borderWidth * 2f));
        DrawTexture(borderRect, borderTexture, borderColor);

        // Draw background
        var backgroundRect = new Rect(x, y, barWidth, barHeight);
        DrawTexture(backgroundRect, backgroundTexture, backgroundColor);

        // Draw fill
        float healthPercentage = maxHealth > 0f ? currentHealth / maxHealth : 0f;
        float fillWidth = barWidth * healthPercentage;
        
        if (fillWidth > 0f)
        {
            var fillRect = new Rect(x, y, fillWidth, barHeight);
            DrawTexture(fillRect, fillTexture, fillColor);
        }
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

        barsVisible = !barsVisible;
        lastToggleFrame = Time.frameCount;
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
        if (fillTexture == null)
        {
            fillTexture = CreateTexture("EnemyHealthBarFill");
        }

        if (backgroundTexture == null)
        {
            backgroundTexture = CreateTexture("EnemyHealthBarBg");
        }

        if (borderTexture == null)
        {
            borderTexture = CreateTexture("EnemyHealthBarBorder");
        }
    }

    private static Texture2D CreateTexture(string textureName)
    {
        var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
        {
            name = textureName
        };
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return texture;
    }

    private static void DrawTexture(Rect rect, Texture2D texture, Color color)
    {
        if (texture == null)
        {
            return;
        }

        Color previousColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill);
        GUI.color = previousColor;
    }
}
