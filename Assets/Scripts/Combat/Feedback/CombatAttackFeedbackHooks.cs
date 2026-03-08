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
        [SerializeField] private AudioSource audioSource;

        [Header("Optional SFX")]
        [SerializeField] private AudioClip slashSfx;

        [Header("SFX Variation")]
        [SerializeField, Range(0f, 2f)] private float minVolume = 0.92f;
        [SerializeField, Range(0f, 2f)] private float maxVolume = 1f;
        [SerializeField, Range(0.5f, 2f)] private float minPitch = 0.94f;
        [SerializeField, Range(0.5f, 2f)] private float maxPitch = 1.08f;

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
            ClampSfxFields();
            ResolveOptionalReferences(allowRuntimeCreate: false);
        }

        private void OnDisable()
        {
            if (impulseDriver != null)
            {
                impulseDriver.StopActiveImpulse();
            }

            if (audioSource != null)
            {
                audioSource.Stop();
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
                    PlaySlashSfx();
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

        private void PlaySlashSfx()
        {
            if (slashSfx == null || audioSource == null)
            {
                return;
            }

            audioSource.pitch = UnityEngine.Random.Range(minPitch, maxPitch);
            audioSource.PlayOneShot(slashSfx, UnityEngine.Random.Range(minVolume, maxVolume));
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

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            if (audioSource == null && allowRuntimeCreate)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        private void ClampSfxFields()
        {
            minVolume = Mathf.Clamp(minVolume, 0f, 2f);
            maxVolume = Mathf.Clamp(maxVolume, 0f, 2f);
            minPitch = Mathf.Clamp(minPitch, 0.5f, 2f);
            maxPitch = Mathf.Clamp(maxPitch, 0.5f, 2f);

            if (maxVolume < minVolume)
            {
                (minVolume, maxVolume) = (maxVolume, minVolume);
            }

            if (maxPitch < minPitch)
            {
                (minPitch, maxPitch) = (maxPitch, minPitch);
            }
        }
    }
}
