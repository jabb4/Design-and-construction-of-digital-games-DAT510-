using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RaidRibbon : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{

    private Image image;
    public GameObject raidMap;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        image = GetComponent<Image>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (raidMap != null) raidMap.SetActive(true);
        image.color = Color.black;

        // Disable Main Camera for performance
        Camera mainCam = Camera.main;
        if (mainCam != null) mainCam.enabled = false;

        if (transform.parent != null) transform.parent.gameObject.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        image.color = Color.blue;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        image.color = Color.black;
    }
}
