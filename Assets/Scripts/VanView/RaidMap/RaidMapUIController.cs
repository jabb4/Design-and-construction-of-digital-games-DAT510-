using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;

public class RaidMapUIController : MonoBehaviour
{
    public static RaidMapUIController Instance { get; private set; }

    [SerializeField] private TMP_Text fuelCounterText;
    [SerializeField] private GameObject vanViewUI;
    [SerializeField] private int citySceneNumber = 2;
    [SerializeField] private int templeSceneNumber = 3;

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

    private void OnEnable()
    {
        fuelCounterText.text = $"Fuel: {GameStateManager.Instance.GetFuelAmount()}/{GameStateManager.Instance.GetMaxFuelAmount()}";
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
        SceneManager.LoadSceneAsync(citySceneNumber);
    }

    public void GoToTemple()
    {
        SceneManager.LoadSceneAsync(templeSceneNumber);
    }
}
