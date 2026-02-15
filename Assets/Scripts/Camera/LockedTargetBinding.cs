using System;
using Combat;
using UnityEngine;

public sealed class LockedTargetBinding : IDisposable
{
    private HealthComponent currentHealth;

    public Transform CurrentTarget { get; private set; }

    public event Action TargetDied;

    public void SetTarget(Transform target)
    {
        if (CurrentTarget == target)
        {
            return;
        }

        UnsubscribeCurrent();
        CurrentTarget = target;
        SubscribeCurrent();
    }

    public void ClearTarget()
    {
        SetTarget(null);
    }

    public void Dispose()
    {
        UnsubscribeCurrent();
        CurrentTarget = null;
    }

    private void SubscribeCurrent()
    {
        if (CurrentTarget == null)
        {
            return;
        }

        currentHealth = CurrentTarget.GetComponent<HealthComponent>();
        if (currentHealth == null)
        {
            currentHealth = CurrentTarget.GetComponentInChildren<HealthComponent>();
        }

        if (currentHealth == null)
        {
            currentHealth = CurrentTarget.GetComponentInParent<HealthComponent>();
        }

        if (currentHealth != null)
        {
            currentHealth.OnDied += HandleTargetDied;
        }
    }

    private void UnsubscribeCurrent()
    {
        if (currentHealth != null)
        {
            currentHealth.OnDied -= HandleTargetDied;
            currentHealth = null;
        }
    }

    private void HandleTargetDied()
    {
        TargetDied?.Invoke();
    }
}