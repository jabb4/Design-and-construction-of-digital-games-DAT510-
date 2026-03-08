using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class MenuController : MonoBehaviour
{
    [SerializeField] private GameObject loadGameButton;
    [SerializeField] private GameObject newGameButton;
    [SerializeField] private GameObject quitGameButton;

    [SerializeField] private GameObject videoPlayer;
    [SerializeField] private GameObject screen;
    [SerializeField] private AudioSource menuAudioSource;

    private Vector3 originalNewGameButtonLocalPosition;
    private Vector3 originalLoadGameButtonLocalPosition;
    private Vector3 originalQuitGameButtonLocalPosition;
    private VideoPlayer cachedVideoPlayer;
    private bool isStartingNewGame;

    private void Awake()
    {
        // Save original button positions
        originalLoadGameButtonLocalPosition = loadGameButton.transform.localPosition;
        originalNewGameButtonLocalPosition = newGameButton.transform.localPosition;
        originalQuitGameButtonLocalPosition = quitGameButton.transform.localPosition;

        if (videoPlayer != null)
        {
            cachedVideoPlayer = videoPlayer.GetComponent<VideoPlayer>();
        }
    }

    private void OnEnable()
    {
        screen.gameObject.SetActive(false);

        if (cachedVideoPlayer != null)
        {
            cachedVideoPlayer.Stop();
        }

        isStartingNewGame = false;

        // Deactivate Load game button if there isn't any saved data
        if (!GameStateManager.Instance.gameSaveExists)
        {
            loadGameButton.SetActive(false);
            newGameButton.transform.localPosition = originalLoadGameButtonLocalPosition;
            quitGameButton.transform.localPosition = originalNewGameButtonLocalPosition;
        }
        else
        {
            loadGameButton.SetActive(true);
            newGameButton.transform.localPosition = originalNewGameButtonLocalPosition;
            quitGameButton.transform.localPosition = originalQuitGameButtonLocalPosition;
        }
    }

    private void OnDisable()
    {
        if (cachedVideoPlayer != null)
        {
            cachedVideoPlayer.loopPointReached -= OnNewGameVideoFinished;
        }
    }

    public void NewGame()
    {
        if (isStartingNewGame)
        {
            return;
        }

        isStartingNewGame = true;

        if (menuAudioSource != null)
        {
            menuAudioSource.Stop();
        }

        if (cachedVideoPlayer == null)
        {
            CompleteNewGame();
            return;
        }

        cachedVideoPlayer.loopPointReached -= OnNewGameVideoFinished;
        cachedVideoPlayer.loopPointReached += OnNewGameVideoFinished;
        cachedVideoPlayer.Stop();
        cachedVideoPlayer.Play();
        screen.gameObject.SetActive(true);
    }

    public void LoadGame()
    {
        GameStateManager.Instance.LoadGameState();
        SceneManager.LoadSceneAsync(1);
    }

    private void OnNewGameVideoFinished(VideoPlayer source)
    {
        source.loopPointReached -= OnNewGameVideoFinished;
        CompleteNewGame();
    }

    private void CompleteNewGame()
    {
        GameStateManager.Instance.SetInitValues();
        GameStateManager.Instance.SaveGameState();
        SceneManager.LoadSceneAsync(1);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
