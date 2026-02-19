namespace Enemies.AI
{
    using Enemies.StateMachine;
    using UnityEngine;

    /// <summary>
    /// Global one-attacker token for enemy coordination.
    /// </summary>
    public static class EnemyAttackTokenService
    {
        private static int currentHolderId = -1;

        public static bool TryAcquire(EnemyStateMachine owner)
        {
            if (owner == null)
            {
                return false;
            }

            int instanceId = owner.GetInstanceID();
            if (currentHolderId == -1 || currentHolderId == instanceId)
            {
                currentHolderId = instanceId;
                return true;
            }

            return false;
        }

        public static void Release(EnemyStateMachine owner)
        {
            if (owner == null)
            {
                return;
            }

            int instanceId = owner.GetInstanceID();
            if (currentHolderId == instanceId)
            {
                currentHolderId = -1;
            }
        }

        public static bool IsHolder(EnemyStateMachine owner)
        {
            return owner != null && owner.GetInstanceID() == currentHolderId;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            currentHolderId = -1;
        }
    }
}
