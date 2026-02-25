using UnityEngine;
using UnityEngine.UI;
using Combat;

namespace Player
{
    /// <summary>
    /// Adds a screen-space blood vignette whenever the player takes a full hit.
    /// Drop this on the Player GameObject alongside CombatOutcomeFeedbackHooks —
    /// Player.cs picks it up automatically via GetComponents<ICombatOutcomeFeedbackHook>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerHitVignette : MonoBehaviour, ICombatOutcomeFeedbackHook
    {
        [Header("Timing")]
        [SerializeField, Min(0.01f)] private float fadeInDuration  = 0.07f;
        [SerializeField, Min(0f)]    private float holdDuration     = 0.05f;
        [SerializeField, Min(0.01f)] private float fadeOutDuration  = 1.0f;

        [Header("Appearance")]
        [SerializeField, Range(0f, 1f)] private float peakAlpha = 0.45f;

        private RawImage vignetteImage;
        private float    timer;
        private bool     active;

        // -----------------------------------------------------------------------

        private void Awake()
        {
            vignetteImage = CreateVignetteUI();
            SetAlpha(0f);
        }

        private void OnDestroy()
        {
            if (vignetteImage != null && vignetteImage.canvas != null)
                Destroy(vignetteImage.canvas.gameObject);
        }

        // -----------------------------------------------------------------------
        // ICombatOutcomeFeedbackHook
        // -----------------------------------------------------------------------

        public void OnCombatOutcome(CombatOutcomeFeedbackContext context)
        {
            if (context.Resolution.Outcome == DamageOutcome.FullHit)
                Trigger();
        }

        // -----------------------------------------------------------------------
        // Animation
        // -----------------------------------------------------------------------

        private void Trigger()
        {
            timer  = 0f;
            active = true;
        }

        private void Update()
        {
            if (!active) return;

            timer += Time.deltaTime;

            if (timer < fadeInDuration)
            {
                SetAlpha(Mathf.Lerp(0f, peakAlpha, timer / fadeInDuration));
            }
            else if (timer < fadeInDuration + holdDuration)
            {
                SetAlpha(peakAlpha);
            }
            else
            {
                float t = (timer - fadeInDuration - holdDuration) / fadeOutDuration;
                if (t < 1f)
                {
                    SetAlpha(Mathf.Lerp(peakAlpha, 0f, t));
                }
                else
                {
                    SetAlpha(0f);
                    active = false;
                }
            }
        }

        private void SetAlpha(float alpha)
        {
            if (vignetteImage == null) return;
            Color c = vignetteImage.color;
            c.a = alpha;
            vignetteImage.color = c;
        }

        // -----------------------------------------------------------------------
        // UI setup
        // -----------------------------------------------------------------------

        private RawImage CreateVignetteUI()
        {
            // Standalone canvas so it isn't affected if this GameObject is disabled
            var canvasGO = new GameObject("PlayerHitVignette_Canvas");
            var canvas   = canvasGO.AddComponent<Canvas>();
            canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 99;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            var imageGO = new GameObject("VignetteImage");
            imageGO.transform.SetParent(canvasGO.transform, false);

            var rt        = imageGO.AddComponent<RectTransform>();
            rt.anchorMin  = Vector2.zero;
            rt.anchorMax  = Vector2.one;
            rt.offsetMin  = Vector2.zero;
            rt.offsetMax  = Vector2.zero;

            var image            = imageGO.AddComponent<RawImage>();
            image.texture        = CreateVignetteTexture();
            image.color          = new Color(1f, 1f, 1f, 0f);
            image.raycastTarget  = false;

            return image;
        }

        // Radial gradient: dark red at screen edges, fully transparent at centre
        private static Texture2D CreateVignetteTexture()
        {
            const int size   = 256;
            const float r    = 0.55f; // red channel
            var tex          = new Texture2D(size, size, TextureFormat.RGBA32, mipChain: false);
            var pixels       = new Color[size * size];
            var center       = new Vector2(0.5f, 0.5f);
            const float diag = 0.7071f; // half-diagonal length in UV space

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u    = x / (float)(size - 1);
                    float v    = y / (float)(size - 1);
                    float dist = Vector2.Distance(new Vector2(u, v), center) / diag;
                    dist       = Mathf.Clamp01(dist);

                    // Power curve: invisible at centre, full red at corners
                    float alpha = Mathf.Pow(dist, 1.6f);
                    pixels[y * size + x] = new Color(r, 0f, 0f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(updateMipmaps: false, makeNoLongerReadable: true);
            return tex;
        }
    }
}
