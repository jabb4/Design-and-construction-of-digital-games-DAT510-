using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    [SerializeField] private GameObject loadGameButton;
    [SerializeField] private GameObject newGameButton;
    [SerializeField] private GameObject quitGameButton;

    private Vector3 originalNewGameButtonLocalPosition;
    private Vector3 originalLoadGameButtonLocalPosition;
    private Vector3 originalQuitGameButtonLocalPosition;

    private void Awake()
    {
        // Save original button positions
        originalLoadGameButtonLocalPosition = loadGameButton.transform.localPosition;
        originalNewGameButtonLocalPosition = newGameButton.transform.localPosition;
        originalQuitGameButtonLocalPosition = quitGameButton.transform.localPosition;
        
    }
     
    private void OnEnable()
    {
        // Deactivate Load game button if there isn't any saved data
        if (!GameStateManager.Instance.gameSaveExists)
        {
            loadGameButton.SetActive(false);
            newGameButton.transform.localPosition = originalLoadGameButtonLocalPosition;
            quitGameButton.transform.localPosition = originalNewGameButtonLocalPosition;
        } else {
            loadGameButton.SetActive(true);
            newGameButton.transform.localPosition = originalNewGameButtonLocalPosition;
            quitGameButton.transform.localPosition = originalQuitGameButtonLocalPosition;
        }
    }

    public void NewGame()
    {
        GameStateManager.Instance.SetInitValues();
        GameStateManager.Instance.SaveGameState();
        SceneManager.LoadSceneAsync(1);
    }

    public void LoadGame()
    {
        // Make sure we load the saved values
        GameStateManager.Instance.LoadGameState();
        SceneManager.LoadSceneAsync(1);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
