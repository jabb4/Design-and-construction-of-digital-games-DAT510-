using UnityEngine;

public class BarUiController : MonoBehaviour
{
    public float MaxResource, Width, Height, Resource;

    [SerializeField]
    private RectTransform bar;

    public void SetMaxResource(float maxResource)
    {
        MaxResource = maxResource;
    }

    public void SetCurrentResource(float resource)
    {
        Resource = resource;

        float newWidth = Width * (Resource/MaxResource);

        bar.sizeDelta = new Vector2(newWidth, Height);
    }
}
