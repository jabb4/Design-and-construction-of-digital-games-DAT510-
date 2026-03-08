using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class BackMenuUI : MonoBehaviour
{
    
    [SerializeField] private bool isInGame = false;
    [SerializeField] private GameObject backMenuBanner;
    [SerializeField] private GameObject backMenuConfirmation;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Resume();
    }

    void OnEnable()
    {
        backMenuConfirmation.SetActive(false);
        backMenuBanner.SetActive(true);
    }

    public void Resume()
    {
        gameObject.SetActive(false);
    }

    private void ToggleConfirmation()
    {
        backMenuBanner.SetActive(false);
        backMenuConfirmation.SetActive(true);
    }

    public void ConfirmExit()
    {
        SceneManager.LoadSceneAsync(0);
    }

    public void ExitToMainMenu()
    {
        if (isInGame)
        {
            ToggleConfirmation();
            return;
        }
        SceneManager.LoadSceneAsync(0);
    }


}
