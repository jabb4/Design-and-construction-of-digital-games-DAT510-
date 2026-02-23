using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class LockOnIndicatorPresenter : IDisposable
{
    private static Sprite proceduralGlowSprite;

    private readonly Camera camera;
    private readonly float indicatorPositionSmoothing;
    private readonly float indicatorBloomIntensity;
    private readonly float indicatorBloomScale;
    private readonly float indicatorBloomPulseAmplitude;
    private readonly float indicatorBloomPulseSpeed;
    private readonly bool enableDebugLogs;

    private Canvas uiCanvas;
    private GameObject indicatorObject;
    private Image indicatorImage;
    private Image indicatorGlowImage;
    private Material indicatorGlowMaterial;
    private RectTransform indicatorRectTransform;
    private Vector2 baseIndicatorSize;
    private Vector2 smoothedLocalPosition;
    private bool hasSmoothedPosition;

    public LockOnIndicatorPresenter(
        Camera camera,
        float indicatorPositionSmoothing,
        float indicatorBloomIntensity,
        float indicatorBloomScale,
        float indicatorBloomPulseAmplitude,
        float indicatorBloomPulseSpeed,
        bool enableDebugLogs)
    {
        this.camera = camera;
        this.indicatorPositionSmoothing = Mathf.Max(0f, indicatorPositionSmoothing);
        this.indicatorBloomIntensity = Mathf.Max(0f, indicatorBloomIntensity);
        this.indicatorBloomScale = Mathf.Max(1f, indicatorBloomScale);
        this.indicatorBloomPulseAmplitude = Mathf.Max(0f, indicatorBloomPulseAmplitude);
        this.indicatorBloomPulseSpeed = Mathf.Max(0f, indicatorBloomPulseSpeed);
        this.enableDebugLogs = enableDebugLogs;

        BuildUi();
    }

    public void Update(Vector3 worldPos, bool isLockedOn, float deltaTime)
    {
        if (!isLockedOn || uiCanvas == null || indicatorObject == null || indicatorImage == null)
        {
            Hide();
            return;
        }

        Vector3 screenPos = camera.WorldToScreenPoint(worldPos);
        if (screenPos.z <= 0f)
        {
            Hide();
            return;
        }

        RectTransform canvasRect = uiCanvas.GetComponent<RectTransform>();
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, null, out Vector2 localPos))
        {
            if (!hasSmoothedPosition || indicatorPositionSmoothing <= 0f)
            {
                smoothedLocalPosition = localPos;
                hasSmoothedPosition = true;
            }
            else
            {
                float lerpFactor = 1f - Mathf.Exp(-indicatorPositionSmoothing * Mathf.Max(0f, deltaTime));
                smoothedLocalPosition = Vector2.Lerp(smoothedLocalPosition, localPos, lerpFactor);
            }

            if (indicatorRectTransform != null)
            {
                indicatorRectTransform.anchoredPosition = smoothedLocalPosition;
            }
        }

        UpdateGlow();
        indicatorObject.SetActive(true);
    }

    public void Hide()
    {
        if (indicatorObject != null)
        {
            indicatorObject.SetActive(false);
        }

        hasSmoothedPosition = false;
        smoothedLocalPosition = Vector2.zero;
    }

    public void Dispose()
    {
        if (uiCanvas != null)
        {
            UnityEngine.Object.Destroy(uiCanvas.gameObject);
            uiCanvas = null;
        }

        if (indicatorGlowMaterial != null)
        {
            UnityEngine.Object.Destroy(indicatorGlowMaterial);
            indicatorGlowMaterial = null;
        }

        indicatorObject = null;
        indicatorImage = null;
        indicatorGlowImage = null;
        indicatorRectTransform = null;
        baseIndicatorSize = Vector2.zero;
        hasSmoothedPosition = false;
        smoothedLocalPosition = Vector2.zero;
    }

    private void BuildUi()
    {
        GameObject canvasGO = new GameObject("LockOnCanvas");
        uiCanvas = canvasGO.AddComponent<Canvas>();
        uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        uiCanvas.sortingOrder = 100;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        indicatorObject = new GameObject("LockOnIndicator");
        indicatorObject.transform.SetParent(uiCanvas.transform, false);
        indicatorRectTransform = indicatorObject.GetComponent<RectTransform>();
        if (indicatorRectTransform == null)
        {
            indicatorRectTransform = indicatorObject.AddComponent<RectTransform>();
        }

        indicatorRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        indicatorRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        indicatorRectTransform.pivot = new Vector2(0.5f, 0.5f);
        indicatorRectTransform.anchoredPosition = Vector2.zero;
        indicatorRectTransform.localScale = Vector3.one;

        GameObject glowObject = new GameObject("LockOnIndicatorGlow");
        glowObject.transform.SetParent(indicatorObject.transform, false);
        indicatorGlowImage = glowObject.AddComponent<Image>();
        indicatorGlowImage.color = new Color(1f, 1f, 1f, 0.45f);
        indicatorGlowImage.raycastTarget = false;
        RectTransform glowRect = indicatorGlowImage.rectTransform;
        glowRect.anchorMin = new Vector2(0.5f, 0.5f);
        glowRect.anchorMax = new Vector2(0.5f, 0.5f);
        glowRect.pivot = new Vector2(0.5f, 0.5f);
        glowRect.anchoredPosition = Vector2.zero;
        glowRect.localScale = Vector3.one;

        indicatorImage = indicatorObject.AddComponent<Image>();
        indicatorImage.color = Color.white;
        indicatorImage.raycastTarget = false;
        baseIndicatorSize = new Vector2(10f, 10f);
        indicatorImage.rectTransform.sizeDelta = baseIndicatorSize;

        Texture2D reticleTexture = Resources.Load<Texture2D>("LockOnIndicator");
        if (reticleTexture != null)
        {
            Sprite reticleSprite = Sprite.Create(
                reticleTexture,
                new Rect(0f, 0f, reticleTexture.width, reticleTexture.height),
                new Vector2(0.5f, 0.5f));
            indicatorImage.sprite = reticleSprite;
            indicatorGlowImage.sprite = GetOrCreateProceduralGlowSprite();
            indicatorGlowImage.rectTransform.sizeDelta = baseIndicatorSize * indicatorBloomScale;
            ConfigureGlowMaterial();
        }
        else if (enableDebugLogs)
        {
            Debug.LogWarning("LockOnIndicatorPresenter: LockOnIndicator texture not found in Resources.");
        }

        indicatorObject.SetActive(false);
    }

    private void ConfigureGlowMaterial()
    {
        if (indicatorGlowImage == null)
        {
            return;
        }

        Shader shader = Shader.Find("UI/Default");
        if (shader == null)
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning("LockOnIndicatorPresenter: UI/Default shader not found; indicator glow falls back to regular UI rendering.");
            }

            return;
        }

        indicatorGlowMaterial = new Material(shader)
        {
            name = "LockOnIndicatorGlow_Material"
        };

        if (indicatorGlowMaterial.HasProperty("_Color"))
        {
            float hdr = Mathf.Max(1f, indicatorBloomIntensity);
            indicatorGlowMaterial.SetColor("_Color", new Color(hdr, hdr, hdr, 1f));
        }

        indicatorGlowImage.material = indicatorGlowMaterial;
    }

    private void UpdateGlow()
    {
        if (indicatorGlowImage == null)
        {
            return;
        }

        float pulseScale = 1f;
        if (indicatorBloomPulseAmplitude > 0f && indicatorBloomPulseSpeed > 0f)
        {
            pulseScale += Mathf.Sin(Time.unscaledTime * indicatorBloomPulseSpeed) * indicatorBloomPulseAmplitude;
        }

        indicatorGlowImage.rectTransform.sizeDelta = baseIndicatorSize * indicatorBloomScale * pulseScale;
    }

    private static Sprite GetOrCreateProceduralGlowSprite()
    {
        if (proceduralGlowSprite != null)
        {
            return proceduralGlowSprite;
        }

        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true)
        {
            name = "LockOnIndicatorGlowTexture",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float invRadius = 1f / (size * 0.5f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int index = y * size + x;
                Vector2 offset = new Vector2(x, y) - center;
                float distance01 = Mathf.Clamp01(offset.magnitude * invRadius);
                float alpha = Mathf.Pow(1f - distance01, 2.5f);
                pixels[index] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, false);

        proceduralGlowSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            size);
        proceduralGlowSprite.name = "LockOnIndicatorGlowSprite";
        return proceduralGlowSprite;
    }
}
