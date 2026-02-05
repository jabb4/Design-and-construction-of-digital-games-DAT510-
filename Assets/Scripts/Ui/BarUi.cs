using UnityEngine;

public class BarUi : MonoBehaviour
{
    public float MaxResource, Width, Height, Resource;

    [SerializeField]
    private RectTransform bar;

    public void setMaxResource(float maxResource)
    {
        MaxResource = maxResource;
    }

    public void setCurrentResource(float resource)
    {
        Resource = resource;

        float newWidth = Width * (Resource/MaxResource);

        bar.sizeDelta = new Vector2(newWidth, Height);
    }
}
