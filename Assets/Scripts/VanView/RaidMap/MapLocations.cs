using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class MapLocations : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, IPointerExitHandler
{
    public enum Destination { Van, City, Temple }

    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Destination destination;
    [SerializeField] private float travelCost = 0f;
    [SerializeField] private float travelDistance = 0f;
    [SerializeField] private Color affordHoverColor = Color.green;
    [SerializeField] private Color cantAffordHoverColor = Color.red;
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float transitionSpeed = 20f;

    private Vector3 originalScale;
    private Vector3 targetScale;
    private Color originalColor;

    void Awake()
    {
        originalScale = titleText.transform.localScale;
        originalColor = titleText.color;
    }

    void OnEnable()
    {

        if (titleText != null)
        {
            if (destination == Destination.Van)
            {
                titleText.text = destination.ToString();
            }
            else
            {
                titleText.text = $"{destination} - {travelDistance}km\n Cost: {travelCost}l";
            }

            titleText.transform.localScale = originalScale;
            targetScale = originalScale;
            titleText.color = originalColor;
        }

    }

    // Update is called once per frame
    void Update()
    {
        if (titleText == null) return;

        if (titleText.transform.localScale != targetScale)
        {
            titleText.transform.localScale = Vector3.Lerp(titleText.transform.localScale, targetScale, Time.deltaTime * transitionSpeed);

            if (Vector3.Distance(titleText.transform.localScale, targetScale) < 0.001f)
                titleText.transform.localScale = targetScale;
        }
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        bool canAffordTravel = GameStateManager.Instance.GetFuelAmount() >= travelCost;

        if (!canAffordTravel)
        {
            return;
        }
        else if (destination != Destination.Van)
        {
            GameStateManager.Instance.RemoveFuel((int)travelCost);
            GameStateManager.Instance.SaveGameState();
        }

        switch (destination)
        {
            case Destination.Van:
                Debug.Log("Going back to VAN");
                RaidMapUIController.Instance.GoToVan();
                break;
            case Destination.City:
                Debug.Log("Entering CITY");
                RaidMapUIController.Instance.GoToCity();
                break;
            case Destination.Temple:
                Debug.Log("Entering TEMPLE");
                RaidMapUIController.Instance.GoToTemple();
                break;
            default:
                break;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Color hoverColor = GameStateManager.Instance.GetFuelAmount() >= travelCost
            ? affordHoverColor
            : cantAffordHoverColor;

        if (titleText != null)
        {
            targetScale = originalScale * hoverScale;
            titleText.color = hoverColor;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ResetHover();
    }

    public void ResetHover()
    {
        if (titleText != null)
        {
            targetScale = originalScale;
            titleText.color = originalColor;
        }
    }
}
