using UnityEngine;
using UnityEngine.InputSystem;


public class VanMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject vanViewUI;

    private void Awake()
    {
        gameObject.SetActive(false);
    }
    
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CloseVanMenuUI();
        }
    }

    public void CloseVanMenuUI()
    {
        vanViewUI.SetActive(true);
        gameObject.SetActive(false);
    }
}
