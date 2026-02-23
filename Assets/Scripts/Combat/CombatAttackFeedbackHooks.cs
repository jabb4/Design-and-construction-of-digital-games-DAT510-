using System;
using UnityEngine;

namespace Combat
{
    [DisallowMultipleComponent]
    public sealed class CombatAttackFeedbackHooks : MonoBehaviour, ICombatAttackFeedbackHook
    {
        [Header("Lunge")]
        [SerializeField] private bool enableSlashLunge = true;
        [SerializeField, Min(0f)] private float lungeDistance = 0.75f;
        [SerializeField, Min(0.01f)] private float lungeDuration = 0.12f;
        [SerializeField, Range(0.05f, 0.95f)] private float lungeEndSpeedFraction = 0.20f;

        [Header("Optional References")]
        [SerializeField] private CombatHorizontalImpulseDriver impulseDriver;

        public event Action<CombatAttackFeedbackContext> OnWindup;
        public event Action<CombatAttackFeedbackContext> OnSlash;
        public event Action<CombatAttackFeedbackContext> OnRecovery;

        private void Awake()
        {
            ResolveOptionalReferences(allowRuntimeCreate: true);
        }

        private void OnValidate()
        {
            lungeEndSpeedFraction = Mathf.Clamp(lungeEndSpeedFraction, 0.05f, 0.95f);
            lungeDistance = Mathf.Max(0f, lungeDistance);
            lungeDuration = Mathf.Max(0.01f, lungeDuration);
            ResolveOptionalReferences(allowRuntimeCreate: false);
        }

        private void OnDisable()
        {
            if (impulseDriver != null)
            {
                impulseDriver.StopActiveImpulse();
            }
        }

        public void OnCombatAttackPhase(CombatAttackFeedbackContext context)
        {
            switch (context.Phase)
            {
                case CombatAttackPhase.Windup:
                    OnWindup?.Invoke(context);
                    break;
                case CombatAttackPhase.Slash:
                    OnSlash?.Invoke(context);
                    StartLunge(context);
                    break;
                case CombatAttackPhase.Recovery:
                    OnRecovery?.Invoke(context);
                    if (impulseDriver != null)
                    {
                        impulseDriver.StopActiveImpulse();
                    }
                    break;
            }
        }

        private void StartLunge(CombatAttackFeedbackContext context)
        {
            if (impulseDriver == null || !isActiveAndEnabled)
            {
                return;
            }

            if (!enableSlashLunge || lungeDistance <= 0f || lungeDuration <= 0f)
            {
                return;
            }

            impulseDriver.PlayImpulse(context.AttackDirection, lungeDistance, lungeDuration, lungeEndSpeedFraction);
        }

        private void ResolveOptionalReferences(bool allowRuntimeCreate)
        {
            if (impulseDriver == null)
            {
                impulseDriver = GetComponent<CombatHorizontalImpulseDriver>();
            }

            if (impulseDriver == null && allowRuntimeCreate)
            {
                impulseDriver = gameObject.AddComponent<CombatHorizontalImpulseDriver>();
            }
        }
    }
}
