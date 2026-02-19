namespace Enemies.AI
{
    using Player.StateMachine;
    using UnityEngine;

    [CreateAssetMenu(fileName = "EnemyCombatProfile", menuName = "Combat/Enemy Combat Profile")]
    public sealed class EnemyCombatProfile : ScriptableObject
    {
        [Header("Shared Combo")]
        [SerializeField] private AttackComboAsset sharedCombo;

        [Header("Attack Chain")]
        [SerializeField, Min(1)] private int minAttackChain = 2;
        [SerializeField, Min(1)] private int maxAttackChain = 5;

        [Header("Parry Counter")]
        [SerializeField, Min(1)] private int minParriesBeforeCounter = 1;
        [SerializeField, Min(1)] private int maxParriesBeforeCounter = 5;

        [Header("Defense Timing")]
        [SerializeField, Min(0.05f)] private float minDefenseDuration = 0.9f;
        [SerializeField, Min(0.05f)] private float maxDefenseDuration = 1.8f;
        [SerializeField, Min(0.01f)] private float parryWindowDuration = 0.2f;
        [SerializeField, Min(0.01f)] private float parryAttemptCooldown = 0.35f;
        [SerializeField, Min(0f)] private float parryThreatMemoryDuration = 0.15f;
        [SerializeField, Min(0.1f)] private float parryTriggerRange = 3f;
        [SerializeField, Min(0f)] private float counterPrepDelay = 0.15f;

        [Header("Spacing")]
        [SerializeField, Min(0.1f)] private float engageRange = 7f;
        [SerializeField, Min(0.1f)] private float orbitRadius = 2.75f;
        [SerializeField, Min(0.1f)] private float attackRange = 2.5f;

        public AttackComboAsset SharedCombo => sharedCombo;
        public int MinAttackChain => minAttackChain;
        public int MaxAttackChain => maxAttackChain;
        public int MinParriesBeforeCounter => minParriesBeforeCounter;
        public int MaxParriesBeforeCounter => maxParriesBeforeCounter;
        public float MinDefenseDuration => minDefenseDuration;
        public float MaxDefenseDuration => maxDefenseDuration;
        public float ParryWindowDuration => parryWindowDuration;
        public float ParryAttemptCooldown => parryAttemptCooldown;
        public float ParryThreatMemoryDuration => parryThreatMemoryDuration;
        public float ParryTriggerRange => parryTriggerRange;
        public float CounterPrepDelay => counterPrepDelay;
        public float EngageRange => engageRange;
        public float OrbitRadius => orbitRadius;
        public float AttackRange => attackRange;

        private void OnValidate()
        {
            minAttackChain = Mathf.Max(1, minAttackChain);
            maxAttackChain = Mathf.Max(minAttackChain, maxAttackChain);
            minParriesBeforeCounter = Mathf.Max(1, minParriesBeforeCounter);
            maxParriesBeforeCounter = Mathf.Max(minParriesBeforeCounter, maxParriesBeforeCounter);
            minDefenseDuration = Mathf.Max(0.05f, minDefenseDuration);
            maxDefenseDuration = Mathf.Max(minDefenseDuration, maxDefenseDuration);
            parryWindowDuration = Mathf.Max(0.01f, parryWindowDuration);
            parryAttemptCooldown = Mathf.Max(0.01f, parryAttemptCooldown);
            parryThreatMemoryDuration = Mathf.Max(0f, parryThreatMemoryDuration);
            parryTriggerRange = Mathf.Max(0.1f, parryTriggerRange);
            counterPrepDelay = Mathf.Max(0f, counterPrepDelay);
            engageRange = Mathf.Max(0.1f, engageRange);
            orbitRadius = Mathf.Max(0.1f, orbitRadius);
            attackRange = Mathf.Max(0.1f, attackRange);
        }
    }
}
