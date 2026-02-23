using System;
using UnityEngine;

namespace Combat
{
    public class HealthComponent : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float maxHealth = 100f;
        [SerializeField, Min(0f)] private float currentHealth = 100f;
        [SerializeField] private bool initializeCurrentToMaxOnAwake = true;

        private bool hasDied;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public bool IsDead => hasDied || currentHealth <= 0f;

        public event Action<float, float, float> OnDamaged;
        public event Action OnDied;

        private void Awake()
        {
            if (initializeCurrentToMaxOnAwake || currentHealth <= 0f)
            {
                currentHealth = maxHealth;
            }
            else
            {
                currentHealth = Mathf.Min(currentHealth, maxHealth);
            }

            hasDied = currentHealth <= 0f;
        }

        private void OnValidate()
        {
            maxHealth = Mathf.Max(1f, maxHealth);
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        }

        public float ApplyDamage(float rawDamage)
        {
            if (IsDead)
            {
                return 0f;
            }

            float clampedDamage = Mathf.Max(0f, rawDamage);
            float appliedDamage = Mathf.Min(clampedDamage, currentHealth);
            if (appliedDamage <= 0f)
            {
                return 0f;
            }

            currentHealth -= appliedDamage;
            OnDamaged?.Invoke(appliedDamage, currentHealth, maxHealth);

            if (currentHealth <= 0f && !hasDied)
            {
                hasDied = true;
                currentHealth = 0f;
                OnDied?.Invoke();
            }

            return appliedDamage;
        }

        public float Heal(float rawAmount)
        {
            if (IsDead)
            {
                return 0f;
            }

            float clampedAmount = Mathf.Max(0f, rawAmount);
            if (clampedAmount <= 0f)
            {
                return 0f;
            }

            float previous = currentHealth;
            currentHealth = Mathf.Min(maxHealth, currentHealth + clampedAmount);
            return currentHealth - previous;
        }

        public void ResetToMaxHealth()
        {
            currentHealth = maxHealth;
            hasDied = false;
        }
    }
}
