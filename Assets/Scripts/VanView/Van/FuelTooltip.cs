using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class FuelTooltip : MonoBehaviour, IPointerExitHandler
{

    public TextMeshProUGUI fuelLevelText;
    public TextMeshProUGUI buyButtonText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        this.gameObject.SetActive(false);
        fuelLevelText.text = "Fuel: " + "10" + "/" + "1000";
        buyButtonText.text = "Fill tank for: " + "990€";
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Debug.Log("Mouse Exited");
        this.gameObject.SetActive(false);
    }
}
