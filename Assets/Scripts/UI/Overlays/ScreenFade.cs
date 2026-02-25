using UnityEngine;
using UnityEngine.UI;

public class ScreenFade : MonoBehaviour
{
    private Image overlay;
    private float delay;
    private float duration;
    private float timer;

    public static ScreenFade Create(float delay, float duration)
    {
        var go = new GameObject("ScreenFade");
        var fade = go.AddComponent<ScreenFade>();
        fade.delay = delay;
        fade.duration = duration;
        fade.BuildUI();
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

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer < delay)
        {
            return;
        }

        float t = Mathf.Clamp01((timer - delay) / duration);
        overlay.color = new Color(0f, 0f, 0f, t);
    }
}
