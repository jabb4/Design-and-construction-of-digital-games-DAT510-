using System;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class LockOnInputRouter : IDisposable
{
    private readonly InputSystem_Actions controls;
    private readonly float targetSwitchCooldown;

    private float switchCooldownRemaining;

    public event Action ToggleLockRequested;

    public LockOnInputRouter(float targetSwitchCooldown)
    {
        this.targetSwitchCooldown = Mathf.Max(0f, targetSwitchCooldown);

        controls = new InputSystem_Actions();
        controls.Player.LockOn.performed += HandleLockOnPerformed;
    }

    public void Enable()
    {
        controls.Enable();
    }

    public void Disable()
    {
        controls.Disable();
    }

    public void Tick(float deltaTime)
    {
        if (switchCooldownRemaining <= 0f)
        {
            return;
        }

        switchCooldownRemaining = Mathf.Max(0f, switchCooldownRemaining - Mathf.Max(0f, deltaTime));
    }

    public Vector2 ReadLookInput()
    {
        return controls.Player.Look.ReadValue<Vector2>();
    }

    public bool TryConsumeSwitchInput(Vector2 lookInput, float switchSensitivity, out Vector2 switchDirection)
    {
        switchDirection = Vector2.zero;

        if (switchCooldownRemaining > 0f)
        {
            return false;
        }

        if (lookInput.magnitude <= switchSensitivity)
        {
            return false;
        }

        switchDirection = lookInput;
        switchCooldownRemaining = targetSwitchCooldown;
        return true;
    }

    public void Dispose()
    {
        controls.Player.LockOn.performed -= HandleLockOnPerformed;
        controls.Dispose();
    }

    private void HandleLockOnPerformed(InputAction.CallbackContext _)
    {
        ToggleLockRequested?.Invoke();
    }
}