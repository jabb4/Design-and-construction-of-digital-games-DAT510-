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
        Cursor.visible = false;
        if (backMenuUI != null)
            backMenuUI.SetActive(false);
        else
            Debug.LogError("backMenuUI is not assigned in RaidManager!", this);
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
