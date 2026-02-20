using UnityEngine;

public class CustomCursor : MonoBehaviour
{
    public static CustomCursor Instance { get; private set; }

    [SerializeField] private Texture2D defaultCursorTexture;
    [SerializeField] private Vector2 defaultHotSpot = Vector2.zero;

    [Space]
    [SerializeField] private Texture2D hoverCursorTexture;
    [SerializeField] private Vector2 hoverHotSpot = Vector2.zero;

    [SerializeField] private CursorMode cursorMode = CursorMode.Auto;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        SetDefaultCursor();
    }

    public void SetDefaultCursor()
    {
        if (defaultCursorTexture != null)
        {
            Cursor.SetCursor(defaultCursorTexture, defaultHotSpot, cursorMode);
        }
    }

    public void SetHoverCursor()
    {
        if (hoverCursorTexture != null)
        {
            Cursor.SetCursor(hoverCursorTexture, hoverHotSpot, cursorMode);
        }
    }
}
