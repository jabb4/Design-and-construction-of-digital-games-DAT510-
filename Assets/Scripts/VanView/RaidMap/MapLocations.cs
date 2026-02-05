using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MapLocations : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, IPointerExitHandler
{
    private Image image;
    public Color hoverColor = new Color32(0, 0, 0, 100);
    private Color transparentColor = new Color32(0, 0, 0, 0);

    public GameObject vanViewUI;

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
                image.color = transparentColor;

                // Re-enable Main Camera 
                GameObject camObj = GameObject.FindGameObjectWithTag("MainCamera");
                if (camObj != null)
                {
                    Camera c = camObj.GetComponent<Camera>();
                    if (c != null) c.enabled = true;
                }

                if (vanViewUI != null) vanViewUI.SetActive(true);
                if (transform.parent != null) transform.parent.gameObject.SetActive(false);
                break;
            case "city":
                Debug.Log("Entering CITY");
                image.color = transparentColor;
                SceneManager.LoadSceneAsync(0);
                break;
            case "temple":
                Debug.Log("Entering TEMPLE");
                image.color = transparentColor;
                SceneManager.LoadSceneAsync(0);
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
        image.color = transparentColor;
    }
}
