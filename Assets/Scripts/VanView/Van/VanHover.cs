using UnityEngine;
using UnityEngine.EventSystems;

public class VanHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public GameObject vanMenu;
    private Outline outline;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        outline = GetComponent<Outline>();
        if (outline != null)
        {
            outline.OutlineMode = Outline.Mode.OutlineVisible;
            outline.enabled = false; // Start disabled
        }
    }

    // Update is called once per frame
    void Update()
    {
    }

    // This will be called when mouse pointer is entering the object
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (outline != null) outline.enabled = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (outline != null) outline.enabled = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        vanMenu.SetActive(true);
    }
}
