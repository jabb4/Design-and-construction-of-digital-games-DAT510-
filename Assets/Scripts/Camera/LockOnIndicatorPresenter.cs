using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class LockOnIndicatorPresenter : IDisposable
{
    private readonly Camera camera;
    private readonly float indicatorPositionSmoothing;
    private readonly bool enableDebugLogs;

    private Canvas uiCanvas;
    private GameObject indicatorObject;
    private Image indicatorImage;
    private Vector2 smoothedLocalPosition;
    private bool hasSmoothedPosition;

    public LockOnIndicatorPresenter(Camera camera, float indicatorPositionSmoothing, bool enableDebugLogs)
    {
        this.camera = camera;
        this.indicatorPositionSmoothing = Mathf.Max(0f, indicatorPositionSmoothing);
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

            indicatorImage.rectTransform.localPosition = new Vector3(smoothedLocalPosition.x, smoothedLocalPosition.y, 0f);
        }

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

        indicatorObject = null;
        indicatorImage = null;
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
        indicatorImage = indicatorObject.AddComponent<Image>();
        indicatorImage.color = Color.white;
        indicatorImage.rectTransform.sizeDelta = new Vector2(10f, 10f);

        Texture2D reticleTexture = Resources.Load<Texture2D>("LockOnIndicator");
        if (reticleTexture != null)
        {
            indicatorImage.sprite = Sprite.Create(
                reticleTexture,
                new Rect(0f, 0f, reticleTexture.width, reticleTexture.height),
                new Vector2(0.5f, 0.5f));
        }
        else if (enableDebugLogs)
        {
            Debug.LogWarning("LockOnIndicatorPresenter: LockOnIndicator texture not found in Resources.");
        }

        indicatorObject.SetActive(false);
    }
}
