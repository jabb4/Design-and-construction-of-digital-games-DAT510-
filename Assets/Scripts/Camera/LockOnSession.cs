using System;
using UnityEngine;

public sealed class LockOnSession : IDisposable
{
    private readonly LockOnTargetService targetService;
    private readonly LockedTargetBinding targetBinding;
    private readonly bool retargetOnInvalid;
    private readonly bool enableDebugLogs;

    private bool isLockedOn;

    public event Action OnLockOnStarted;
    public event Action OnLockOnEnded;
    public event Action<Transform, Transform> OnLockTargetChanged;

    public bool IsLockedOn => isLockedOn;
    public Transform LockedTarget => targetBinding.CurrentTarget;

    public LockOnSession(
        LockOnTargetService targetService,
        LockedTargetBinding targetBinding,
        bool retargetOnInvalid,
        bool enableDebugLogs)
    {
        this.targetService = targetService;
        this.targetBinding = targetBinding;
        this.retargetOnInvalid = retargetOnInvalid;
        this.enableDebugLogs = enableDebugLogs;

        this.targetBinding.TargetDied += HandleBoundTargetDied;
    }

    public bool TryLockBestTarget()
    {
        if (isLockedOn)
        {
            return false;
        }

        Transform target = targetService.FindBestTarget();
        if (target == null)
        {
            return false;
        }

        StartLock(target);
        return true;
    }

    public bool TrySwitchTarget(Vector2 inputDirection)
    {
        if (!isLockedOn || LockedTarget == null)
        {
            return false;
        }

        Transform bestTarget = targetService.FindSwitchTarget(LockedTarget, inputDirection);
        if (bestTarget == null || bestTarget == LockedTarget)
        {
            return false;
        }

        SwitchTarget(bestTarget, LockInvalidReason.Manual);
        return true;
    }

    public void Validate(float deltaTime)
    {
        if (!isLockedOn)
        {
            return;
        }

        if (targetService.TryGetInvalidReason(LockedTarget, deltaTime, out LockInvalidReason reason))
        {
            ResolveInvalidTarget(reason);
        }
    }

    public void UnlockManual()
    {
        Unlock(LockInvalidReason.Manual);
    }

    public void Dispose()
    {
        targetBinding.TargetDied -= HandleBoundTargetDied;
        targetBinding.ClearTarget();
        targetService.ResetState();
        isLockedOn = false;
    }

    private void HandleBoundTargetDied()
    {
        if (!isLockedOn)
        {
            return;
        }

        ResolveInvalidTarget(LockInvalidReason.Dead);
    }

    private void ResolveInvalidTarget(LockInvalidReason reason)
    {
        if (!isLockedOn)
        {
            return;
        }

        if (retargetOnInvalid)
        {
            Transform replacement = targetService.FindBestTarget(LockedTarget);
            if (replacement != null)
            {
                SwitchTarget(replacement, reason);
                return;
            }
        }

        Unlock(reason);
    }

    private void StartLock(Transform target)
    {
        isLockedOn = true;
        targetBinding.SetTarget(target);
        targetService.NotifyLockedTargetChanged();

        if (enableDebugLogs)
        {
            Debug.Log($"[LockOnSession] Lock started on '{target.name}'.");
        }

        OnLockOnStarted?.Invoke();
    }

    private void SwitchTarget(Transform target, LockInvalidReason reason)
    {
        Transform previousTarget = LockedTarget;
        targetBinding.SetTarget(target);
        targetService.NotifyLockedTargetChanged();

        if (enableDebugLogs)
        {
            Debug.Log($"[LockOnSession] Lock target changed '{previousTarget?.name}' -> '{target.name}' ({reason}).");
        }

        OnLockTargetChanged?.Invoke(previousTarget, target);
    }

    private void Unlock(LockInvalidReason reason)
    {
        if (!isLockedOn)
        {
            return;
        }

        Transform previousTarget = LockedTarget;

        isLockedOn = false;
        targetBinding.ClearTarget();
        targetService.ResetState();

        if (enableDebugLogs)
        {
            Debug.Log($"[LockOnSession] Lock released from '{previousTarget?.name}' ({reason}).");
        }

        OnLockOnEnded?.Invoke();
    }
}