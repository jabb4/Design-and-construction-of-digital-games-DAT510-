using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ScreenFade : MonoBehaviour
{
    private Image overlay;
    private float fadeInDelay;
    private float fadeInDuration;
    private float fadeOutDuration;
    private string targetScene;
    private float timer;
    private Phase phase;
    private bool sceneLoaded;

    private enum Phase
    {
        FadeInDelay,
        FadeIn,
        WaitForScene,
        WaitOneFrame,
        FadeOut,
        Done
    }

    public static ScreenFade Create(float fadeInDelay, float fadeInDuration,
        string targetScene = null, float fadeOutDuration = 0f)
    {
        var go = new GameObject("ScreenFade");
        var fade = go.AddComponent<ScreenFade>();
        fade.fadeInDelay = fadeInDelay;
        fade.fadeInDuration = fadeInDuration;
        fade.targetScene = targetScene;
        fade.fadeOutDuration = fadeOutDuration;
        fade.phase = Phase.FadeInDelay;
        fade.BuildUI();

        if (!string.IsNullOrEmpty(targetScene))
        {
            DontDestroyOnLoad(go);
        }

        return fade;
    }

    private void BuildUI()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        gameObject.AddComponent<CanvasScaler>();

        var imageGO = new GameObject("FadeOverlay");
        imageGO.transform.SetParent(transform, false);

        var rt = imageGO.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        overlay = imageGO.AddComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0f);
        overlay.raycastTarget = false;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        sceneLoaded = true;
    }

    private void Update()
    {
        switch (phase)
        {
            case Phase.FadeInDelay:
                timer += Time.unscaledDeltaTime;
                if (timer >= fadeInDelay)
                {
                    timer = 0f;
                    phase = Phase.FadeIn;
                }
                break;

            case Phase.FadeIn:
                timer += Time.unscaledDeltaTime;
                float fadeInT = Mathf.Clamp01(timer / fadeInDuration);
                overlay.color = new Color(0f, 0f, 0f, fadeInT);
                if (fadeInT >= 1f)
                {
                    timer = 0f;
                    if (!string.IsNullOrEmpty(targetScene))
                    {
                        sceneLoaded = false;
                        phase = Phase.WaitForScene;
                        SceneManager.LoadScene(targetScene);
                    }
                    else
                    {
                        phase = Phase.Done;
                    }
                }
                break;

            case Phase.WaitForScene:
                if (sceneLoaded)
                {
                    overlay.color = new Color(0f, 0f, 0f, 1f);
                    phase = Phase.WaitOneFrame;
                }
                break;

            // Skip one frame so the timer delta from the scene load frame is discarded.
            case Phase.WaitOneFrame:
                timer = 0f;
                phase = Phase.FadeOut;
                break;

            case Phase.FadeOut:
                timer += Time.unscaledDeltaTime;
                if (fadeOutDuration <= 0f)
                {
                    overlay.color = new Color(0f, 0f, 0f, 0f);
                    phase = Phase.Done;
                }
                else
                {
                    float fadeOutT = Mathf.Clamp01(timer / fadeOutDuration);
                    overlay.color = new Color(0f, 0f, 0f, 1f - fadeOutT);
                    if (fadeOutT >= 1f)
                    {
                        phase = Phase.Done;
                    }
                }
                break;

            case Phase.Done:
                Destroy(gameObject);
                break;
        }
    }
}
