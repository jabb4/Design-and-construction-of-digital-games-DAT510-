using UnityEngine;
using UnityEngine.UI;

public class DeathOverlay : MonoBehaviour
{
    private RawImage overlay;
    private RectTransform overlayRT;
    private RectTransform canvasRT;
    private Material overlayMaterial;
    private float delay;
    private float glowPulseDuration;
    private float baseIntensity;
    private float peakIntensity;
    private float timer;

    private const float RestScale = 0.5f;
    private const float SlamStartScale = 1.1f;
    private const float SlamOvershootScale = 0.46f;
    private const float SlamDuration = 0.06f;
    private const float BounceDuration = 0.08f;
    private const float ShakeDuration = 0.4f;
    private const float ShakeIntensity = 25f;

    private float shakeTimer;

    private enum Phase { Waiting, Slam, Bounce, Settle, Idle }
    private Phase phase = Phase.Waiting;

    public static DeathOverlay Create(Texture texture, float delay,
        float baseIntensity = 4f, float peakIntensity = 60f,
        float glowPulseDuration = 2.5f)
    {
        var go = new GameObject("DeathOverlay");

        var instance = go.AddComponent<DeathOverlay>();
        instance.delay = delay;
        instance.baseIntensity = baseIntensity;
        instance.peakIntensity = peakIntensity;
        instance.glowPulseDuration = glowPulseDuration;
        instance.BuildUI(texture);
        return instance;
    }

    private void OnDestroy()
    {
        if (overlayMaterial != null)
        {
            Destroy(overlayMaterial);
            overlayMaterial = null;
        }
    }

    private void BuildUI(Texture texture)
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = Camera.main;
        canvas.sortingOrder = 99;
        canvas.planeDistance = 1f;
        gameObject.AddComponent<CanvasScaler>();
        canvasRT = GetComponent<RectTransform>();

        var imageGO = new GameObject("Overlay");
        imageGO.transform.SetParent(transform, false);

        overlayRT = imageGO.AddComponent<RectTransform>();
        overlayRT.anchorMin = new Vector2(0.5f, 0.5f);
        overlayRT.anchorMax = new Vector2(0.5f, 0.5f);
        overlayRT.pivot = new Vector2(0.5f, 0.5f);
        overlayRT.anchoredPosition = Vector2.zero;
        overlayRT.localScale = Vector3.one * SlamStartScale;
        overlayRT.sizeDelta = new Vector2(Screen.width, Screen.height);

        overlay = imageGO.AddComponent<RawImage>();
        overlay.texture = texture;
        overlay.color = new Color(1f, 1f, 1f, 0f);
        overlay.raycastTarget = false;

        var fitter = imageGO.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = (float)texture.width / texture.height;

        ConfigureMaterial();
    }

    private void ConfigureMaterial()
    {
        Shader shader = Shader.Find("UI/Bloom");
        if (shader == null)
        {
            Debug.LogWarning("DeathOverlay: UI/Bloom shader not found, bloom will not work.");
            return;
        }

        overlayMaterial = new Material(shader) { name = "DeathOverlay_Material" };
        overlayMaterial.SetColor("_BloomColor", new Color(1f, 0f, 0f, 1f));
        overlay.material = overlayMaterial;
        SetIntensity(baseIntensity);
    }

    private void ApplyShake(float t)
    {
        float shakeT = Mathf.Clamp01(t / ShakeDuration);
        float decay = (1f - shakeT) * (1f - shakeT);
        float offsetX = Mathf.Sin(t * 127.1f) + Mathf.Sin(t * 73.7f) * 0.5f;
        float offsetY = Mathf.Sin(t * 98.3f) + Mathf.Sin(t * 53.9f) * 0.5f;
        overlayRT.anchoredPosition = new Vector2(offsetX, offsetY) * ShakeIntensity * decay;
    }

    private void SetIntensity(float intensity)
    {
        if (overlayMaterial != null)
        {
            overlayMaterial.SetFloat("_BloomIntensity", intensity);
        }
    }

    private void Update()
    {
        timer += Time.unscaledDeltaTime;

        switch (phase)
        {
            case Phase.Waiting:
                if (timer >= delay)
                {
                    timer = 0f;
                    // Instant full opacity — no fade
                    overlay.color = Color.white;
                    SetIntensity(peakIntensity);
                    phase = Phase.Slam;
                }
                break;

            // Hard linear slam from big to overshoot-small
            case Phase.Slam:
                float slamT = Mathf.Clamp01(timer / SlamDuration);
                float slamScale = Mathf.Lerp(SlamStartScale, SlamOvershootScale, slamT);
                overlayRT.localScale = Vector3.one * slamScale;

                if (slamT >= 1f)
                {
                    overlayRT.localScale = Vector3.one * SlamOvershootScale;
                    shakeTimer = 0f;
                    timer = 0f;
                    phase = Phase.Bounce;
                }
                break;

            // Bounce back + shake + glow decay all start at impact
            case Phase.Bounce:
                shakeTimer += Time.unscaledDeltaTime;
                ApplyShake(shakeTimer);

                float bounceT = Mathf.Clamp01(timer / BounceDuration);
                float bounceEase = bounceT * bounceT;
                float bounceScale = Mathf.Lerp(SlamOvershootScale, RestScale, bounceEase);
                overlayRT.localScale = Vector3.one * bounceScale;

                // Glow starts decaying from impact
                float glowT1 = Mathf.Clamp01(shakeTimer / glowPulseDuration);
                float glowEase1 = 1f - (1f - glowT1) * (1f - glowT1) * (1f - glowT1);
                SetIntensity(Mathf.Lerp(peakIntensity, baseIntensity, glowEase1));

                if (bounceT >= 1f)
                {
                    overlayRT.localScale = Vector3.one * RestScale;
                    timer = 0f;
                    phase = Phase.Settle;
                }
                break;

            // Shake + glow decay continue
            case Phase.Settle:
                shakeTimer += Time.unscaledDeltaTime;
                ApplyShake(shakeTimer);

                float settleDuration = Mathf.Max(ShakeDuration, glowPulseDuration);
                float settleT = Mathf.Clamp01(shakeTimer / settleDuration);

                float glowT2 = Mathf.Clamp01(shakeTimer / glowPulseDuration);
                float glowEase2 = 1f - (1f - glowT2) * (1f - glowT2) * (1f - glowT2);
                SetIntensity(Mathf.Lerp(peakIntensity, baseIntensity, glowEase2));

                if (settleT >= 1f)
                {
                    overlayRT.anchoredPosition = Vector2.zero;
                    SetIntensity(baseIntensity);
                    phase = Phase.Idle;
                }
                break;
        }
    }
}
