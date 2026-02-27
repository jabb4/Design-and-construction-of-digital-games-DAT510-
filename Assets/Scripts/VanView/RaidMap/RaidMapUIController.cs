using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class RaidMapUIController : MonoBehaviour
{
    public static RaidMapUIController Instance { get; private set; }

    public GameObject vanViewUI;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        else
        {
            Instance = this;
        }
        gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        // If we press esc we want to go back to the van view from the raid map.
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            GoToVan();
        }
        
    }

    // Goes back from 
    public void GoToVan()
    {
        // Re-enable Main Camera 
        GameObject camObj = GameObject.FindGameObjectWithTag("MainCamera");
        if (camObj != null)
        {
            Camera c = camObj.GetComponent<Camera>();
            if (c != null) c.enabled = true;
        }

        if (vanViewUI != null) vanViewUI.SetActive(true);
        
        // Reset hover effect in children 
        foreach (var loc in GetComponentsInChildren<MapLocations>())
        {
            loc.ResetHover();
        }

        gameObject.SetActive(false);
    }

    public void GoToCity()
    {
        SceneManager.LoadSceneAsync(0);
    }

    public void GoToTemple()
    {
        SceneManager.LoadSceneAsync(0);
    }
}
