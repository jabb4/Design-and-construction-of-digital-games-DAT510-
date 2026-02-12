using UnityEngine;
using UnityEngine.InputSystem;

public class VanViewUIController : MonoBehaviour
{
    public static VanViewUIController Instance { get; private set; }

    public BackMenuUI backMenuUI;

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
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // If we press esc we want to open backmenuUI
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            backMenuUI.gameObject.SetActive(true);
        }
        
    }
}
