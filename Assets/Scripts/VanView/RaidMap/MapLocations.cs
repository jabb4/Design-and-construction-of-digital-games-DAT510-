using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MapLocations : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, IPointerExitHandler
{
    private Image image;
    public Color hoverColor = new Color32(0, 0, 0, 100);
    private Color transparentColor = new Color32(0, 0, 0, 0);

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        image = GetComponent<Image>();
        image.color = transparentColor;
    }

    // Update is called once per frame
    void Update()
    {

    }
    public void OnPointerClick(PointerEventData eventData)
    {
        switch (tag)
        {
            case "van":
                Debug.Log("Going back to VAN");
                RaidMapUIController.Instance.GoToVan();
                break;
            case "city":
                Debug.Log("Entering CITY");
                RaidMapUIController.Instance.GoToCity();
                break;
            case "temple":
                Debug.Log("Entering TEMPLE");
                RaidMapUIController.Instance.GoToTemple();
                break;
            default:
                // no action for other tags
                break;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        image.color = hoverColor;

    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ResetHover();
    }

    public void ResetHover()
    {
        image.color = transparentColor;
    }
}
