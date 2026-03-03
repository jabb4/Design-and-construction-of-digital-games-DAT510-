using UnityEngine;
using UnityEngine.InputSystem;
using Player.StateMachine;

public class RaidManager : MonoBehaviour
{
    [SerializeField] private GameObject backMenuUI;
    [SerializeField] private PlayerInputHandler playerInput;
    [SerializeField] private CameraController cameraController;

    private bool isPaused = false;

    private void Awake()
    {
        backMenuUI.SetActive(false);
    }

    private void Update()
    {
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

        if (playerInput != null) playerInput.IsBlocked = paused;
        if (cameraController != null) cameraController.IsRotationBlocked = paused;

        Cursor.visible = paused;
        Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
    }
}
