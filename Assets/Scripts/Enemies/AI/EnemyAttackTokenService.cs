namespace Enemies.AI
{
    using System.Collections.Generic;
    using Enemies.StateMachine;
    using UnityEngine;

    /// <summary>
    /// Global one-attacker token for enemy coordination.
    /// </summary>
    public static class EnemyAttackTokenService
    {
        private static int currentHolderId = -1;
        private static EnemyStateMachine currentHolder;
        private static int priorityOwnerId = -1;
        private static float priorityOwnerUntilTime = float.NegativeInfinity;
        private static EnemyStateMachine priorityOwner;
        private static float globalBlockedUntilTime = float.NegativeInfinity;
        private static readonly Dictionary<int, float> reentryBlockedUntilByOwnerId = new Dictionary<int, float>(16);

        public static bool TryAcquire(EnemyStateMachine owner)
        {
            if (!IsAcquireAllowed(owner, out int instanceId))
            {
                return false;
            }

            currentHolderId = instanceId;
            currentHolder = owner;
            return true;
        }

        public static bool CanAcquire(EnemyStateMachine owner)
        {
            return IsAcquireAllowed(owner, out _);
        }

        public static void Release(EnemyStateMachine owner)
        {
            Release(owner, 0f, 0f);
        }

        public static void Release(EnemyStateMachine owner, float globalCooldownSeconds, float reentryDelaySeconds)
        {
            if (owner == null)
            {
                return;
            }

            int instanceId = owner.GetInstanceID();
            if (currentHolderId == instanceId)
            {
                currentHolderId = -1;
                currentHolder = null;
            }

            float now = Time.realtimeSinceStartup;
            float cooldown = Mathf.Max(0f, globalCooldownSeconds);
            float reentryDelay = Mathf.Max(0f, reentryDelaySeconds);

            if (cooldown > 0f)
            {
                globalBlockedUntilTime = Mathf.Max(globalBlockedUntilTime, now + cooldown);
            }

            if (reentryDelay > 0f)
            {
                reentryBlockedUntilByOwnerId[instanceId] = now + reentryDelay;
            }
        }

        public static bool IsHolder(EnemyStateMachine owner)
        {
            if (owner == null)
            {
                return false;
            }

            CleanupStaleOwners(Time.realtimeSinceStartup);
            return owner.GetInstanceID() == currentHolderId;
        }

        public static bool IsHeldByOther(EnemyStateMachine owner)
        {
            CleanupStaleOwners(Time.realtimeSinceStartup);
            if (owner == null)
            {
                return currentHolderId != -1;
            }

            return currentHolderId != -1 && currentHolderId != owner.GetInstanceID();
        }

        public static bool IsPriorityOwner(EnemyStateMachine owner)
        {
            if (owner == null)
            {
                return false;
            }

            CleanupStaleOwners(Time.realtimeSinceStartup);
            return priorityOwnerId != -1 && priorityOwnerId == owner.GetInstanceID();
        }

        public static bool TryGetPriorityOwner(out EnemyStateMachine owner)
        {
            CleanupStaleOwners(Time.realtimeSinceStartup);
            owner = priorityOwnerId == -1 ? null : priorityOwner;
            return owner != null;
        }

        public static bool TryGetTokenHolder(out EnemyStateMachine owner)
        {
            CleanupStaleOwners(Time.realtimeSinceStartup);
            owner = currentHolderId == -1 ? null : currentHolder;
            return owner != null;
        }

        public static void SetPriorityOwner(EnemyStateMachine owner, float durationSeconds)
        {
            if (owner == null)
            {
                return;
            }

            float duration = Mathf.Max(0f, durationSeconds);
            if (duration <= 0f)
            {
                return;
            }

            float now = Time.realtimeSinceStartup;
            priorityOwnerId = owner.GetInstanceID();
            priorityOwner = owner;
            priorityOwnerUntilTime = now + duration;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            currentHolderId = -1;
            currentHolder = null;
            priorityOwnerId = -1;
            priorityOwner = null;
            priorityOwnerUntilTime = float.NegativeInfinity;
            globalBlockedUntilTime = float.NegativeInfinity;
            reentryBlockedUntilByOwnerId.Clear();
        }

        private static bool IsAcquireAllowed(EnemyStateMachine owner, out int instanceId)
        {
            instanceId = -1;
            if (owner == null)
            {
                return false;
            }

            instanceId = owner.GetInstanceID();
            float now = Time.realtimeSinceStartup;
            CleanupStaleOwners(now);

            if (currentHolderId != -1 && currentHolderId != instanceId)
            {
                return false;
            }

            // Scope coordination to the active local encounter.
            // Enemies far from their target should not contend for this token.
            if (owner.HasTarget)
            {
                float maxCoordinationDistance = owner.EngageRange;
                if (owner.CombatProfile != null)
                {
                    maxCoordinationDistance = Mathf.Max(maxCoordinationDistance, owner.CombatProfile.GroupAwarenessRadius);
                }

                if (owner.DistanceToTarget > maxCoordinationDistance)
                {
                    return false;
                }
            }

            // Focus priority is an advisory preference, not a hard global lock.
            // Other enemies may still acquire when they are otherwise eligible.
            if (priorityOwnerId == instanceId)
            {
                return true;
            }

            if (now < globalBlockedUntilTime)
            {
                return false;
            }

            if (!reentryBlockedUntilByOwnerId.TryGetValue(instanceId, out float blockedUntil))
            {
                return true;
            }

            if (now >= blockedUntil)
            {
                reentryBlockedUntilByOwnerId.Remove(instanceId);
                return true;
            }

            return false;
        }

        private static void CleanupStaleOwners(float now)
        {
            if (currentHolderId != -1 && (currentHolder == null || !currentHolder.isActiveAndEnabled || !currentHolder.IsAlive))
            {
                currentHolderId = -1;
                currentHolder = null;
            }

            if (priorityOwnerId == -1)
            {
                return;
            }

            if (priorityOwner == null || !priorityOwner.isActiveAndEnabled || !priorityOwner.IsAlive)
            {
                priorityOwnerId = -1;
                priorityOwner = null;
                priorityOwnerUntilTime = float.NegativeInfinity;
                return;
            }

            if (now <= priorityOwnerUntilTime)
            {
                return;
            }

            priorityOwnerId = -1;
            priorityOwner = null;
            priorityOwnerUntilTime = float.NegativeInfinity;
        }
    }
}
