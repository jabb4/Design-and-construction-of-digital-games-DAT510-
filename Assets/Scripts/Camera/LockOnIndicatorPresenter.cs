using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class LockOnIndicatorPresenter : IDisposable
{
    private readonly Camera camera;
    private readonly float targetPointHeightOffset;
    private readonly bool enableDebugLogs;

    private Canvas uiCanvas;
    private GameObject indicatorObject;
    private Image indicatorImage;

    public LockOnIndicatorPresenter(Camera camera, float targetPointHeightOffset, bool enableDebugLogs)
    {
        this.camera = camera;
        this.targetPointHeightOffset = targetPointHeightOffset;
        this.enableDebugLogs = enableDebugLogs;

        BuildUi();
    }

    public void Update(Transform target, bool isLockedOn)
    {
        if (!isLockedOn || target == null || uiCanvas == null || indicatorObject == null || indicatorImage == null)
        {
            Hide();
            return;
        }

        Vector3 worldPos = TargetPointResolver.ResolveTargetPoint(target, targetPointHeightOffset);
        Vector3 screenPos = camera.WorldToScreenPoint(worldPos);

        RectTransform canvasRect = uiCanvas.GetComponent<RectTransform>();
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, null, out Vector2 localPos))
        {
            indicatorImage.rectTransform.localPosition = localPos;
        }

        indicatorObject.SetActive(true);
    }

    public void Hide()
    {
        if (indicatorObject != null)
        {
            indicatorObject.SetActive(false);
        }
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
        indicatorObject.transform.SetParent(uiCanvas.transform);
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