using UnityEngine;
using UnityEngine.EventSystems;

public class FuelHover : MonoBehaviour, IPointerEnterHandler
{

    public GameObject fuelTooltip;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log("FuelHover started");
    }

    // Update is called once per frame
    void Update()
    {
    }

    // This will be called when mouse pointer is entering the object
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Debug.Log("Mouse Entered");
        fuelTooltip.gameObject.SetActive(true);
    }
}
