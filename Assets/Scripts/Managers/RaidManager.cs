using UnityEngine;
using UnityEngine.InputSystem;
using Player.StateMachine;

public class RaidManager : MonoBehaviour
{
    [SerializeField] private GameObject backMenuUI;
    [SerializeField] private PlayerInputHandler playerInput;
    [SerializeField] private CameraController cameraController;
    [SerializeField] private TutorialScript tutorial;
    [SerializeField] private EnemyWaveSystem enemyWaveSystem;

    private bool isPaused = false;
    private bool tutorialActive = false;

    private void Start()
    {
        Cursor.visible = false;
        if (backMenuUI != null)
            backMenuUI.SetActive(false);
        else
            Debug.LogError("backMenuUI is not assigned in RaidManager!", this);

        if (tutorial != null && tutorial.ShouldShow())
        {
            tutorialActive = true;
            enemyWaveSystem.gameObject.SetActive(false);
            tutorial.OpenSlide();
            SetPaused(true);
        }
    }

    private void Update()
    {
        // While tutorial is visible, block pause menu and ESC
        if (tutorialActive)
        {
            if (tutorial == null || !tutorial.IsOpen)
            {
                tutorialActive = false;
                enemyWaveSystem.gameObject.SetActive(true);
                SetPaused(false);
            }
            return;
        }

        // Sync state if the menu was closed externally (e.g. via the Resume button in BackMenuUI)
        if (isPaused && !backMenuUI.activeSelf)
        {
            SetPaused(false);
            return;
        }

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            SetPaused(!isPaused);
        }
    }

    public void SetPaused(bool paused)
    {
        isPaused = paused;
        backMenuUI.SetActive(paused);

        if (paused) playerInput.attackBufferDuration = 0f;
        else playerInput.attackBufferDuration = 0.2f;
        if (playerInput != null) playerInput.IsBlocked = paused;
        if (cameraController != null) cameraController.IsRotationBlocked = paused;

        Cursor.visible = paused;
        Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
    }
}
