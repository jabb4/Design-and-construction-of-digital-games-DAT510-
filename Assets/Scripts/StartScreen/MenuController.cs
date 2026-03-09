using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class MenuController : MonoBehaviour
{
    [SerializeField] private GameObject loadGameButton;
    [SerializeField] private GameObject newGameButton;
    [SerializeField] private GameObject quitGameButton;

    [SerializeField] private GameObject skipIntroButton;
    [SerializeField] private GameObject videoPlayer;
    [SerializeField] private GameObject screen;
    [SerializeField] private AudioSource menuAudioSource;
    [SerializeField] private float introCursorHideDelay = 2f;

    private Vector3 originalNewGameButtonLocalPosition;
    private Vector3 originalLoadGameButtonLocalPosition;
    private Vector3 originalQuitGameButtonLocalPosition;
    private VideoPlayer cachedVideoPlayer;
    private bool isStartingNewGame;
    private bool isPlayingIntro;
    private Vector2 lastMousePosition;
    private float lastMouseMoveTime;

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
        skipIntroButton.gameObject.SetActive(false);
        SetIntroCursorVisible(true);

        if (cachedVideoPlayer != null)
        {
            cachedVideoPlayer.Stop();
        }

        isStartingNewGame = false;
        isPlayingIntro = false;
        lastMousePosition = GetMousePosition();
        lastMouseMoveTime = Time.unscaledTime;

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

    private void Update()
    {
        if (!isPlayingIntro)
        {
            return;
        }

        Vector2 currentMousePosition = GetMousePosition();
        if (currentMousePosition != lastMousePosition)
        {
            lastMousePosition = currentMousePosition;
            lastMouseMoveTime = Time.unscaledTime;
            SetIntroCursorVisible(true);
            skipIntroButton.gameObject.SetActive(true);
            return;
        }

        if (Time.unscaledTime - lastMouseMoveTime >= introCursorHideDelay)
        {
            SetIntroCursorVisible(false);
            skipIntroButton.gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        if (cachedVideoPlayer != null)
        {
            cachedVideoPlayer.loopPointReached -= OnNewGameVideoFinished;
        }

        SetIntroCursorVisible(true);
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

        isPlayingIntro = true;
        lastMousePosition = GetMousePosition();
        lastMouseMoveTime = Time.unscaledTime;
        skipIntroButton.gameObject.SetActive(false);
        SetIntroCursorVisible(false);

        cachedVideoPlayer.loopPointReached -= OnNewGameVideoFinished;
        cachedVideoPlayer.loopPointReached += OnNewGameVideoFinished;
        cachedVideoPlayer.Stop();
        screen.gameObject.SetActive(true);
        cachedVideoPlayer.Play();
    }

    public void LoadGame()
    {
        GameStateManager.Instance.LoadGameState();
        SceneManager.LoadSceneAsync(1);
    }

    public void SkipIntro()
    {
        if (cachedVideoPlayer != null)
        {
            cachedVideoPlayer.loopPointReached -= OnNewGameVideoFinished;
            cachedVideoPlayer.Stop();
        }

        CompleteNewGame();
    }

    private void OnNewGameVideoFinished(VideoPlayer source)
    {
        source.loopPointReached -= OnNewGameVideoFinished;
        CompleteNewGame();
    }

    private void CompleteNewGame()
    {
        isPlayingIntro = false;
        SetIntroCursorVisible(true);
        skipIntroButton.gameObject.SetActive(false);

        GameStateManager.Instance.SetInitValues();
        GameStateManager.Instance.SaveGameState();
        SceneManager.LoadSceneAsync(2);
    }

    private Vector2 GetMousePosition()
    {
        if (Mouse.current == null)
        {
            return lastMousePosition;
        }

        return Mouse.current.position.ReadValue();
    }

    private void SetIntroCursorVisible(bool isVisible)
    {
        Cursor.visible = isVisible;
        Cursor.lockState = CursorLockMode.None;
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
