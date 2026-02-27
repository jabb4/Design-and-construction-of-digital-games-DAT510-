using UnityEngine;
using UnityEngine.InputSystem;

public class VanViewUIController : MonoBehaviour
{
    public static VanViewUIController Instance { get; private set; }

    [SerializeField] private GameObject backMenuUI;
    [SerializeField] private GameObject raidMap;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    

    // Update is called once per frame
    void Update()
    {
        // If we press esc we want to open backmenuUI
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            backMenuUI.SetActive(true);
        }
        
    }

    public void OpenRaidMap()
    {
        raidMap?.SetActive(true);

        // Disable Main Camera for performance
        Camera mainCam = Camera.main;
        if (mainCam != null) mainCam.enabled = false;

        // Disable VanViewUI
        gameObject.SetActive(false);
    }


}
