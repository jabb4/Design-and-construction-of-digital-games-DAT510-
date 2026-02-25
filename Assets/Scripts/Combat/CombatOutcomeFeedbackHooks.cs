using System;
using UnityEngine;

namespace Combat
{
    [DisallowMultipleComponent]
    public sealed class CombatOutcomeFeedbackHooks : MonoBehaviour, ICombatOutcomeFeedbackHook
    {
        [Header("Pushback")]
        [SerializeField, Min(0f)] private float blockedPushDistance = 0.40f;
        [SerializeField, Min(0.01f)] private float blockedPushDuration = 0.14f;
        [SerializeField, Min(0f)] private float parriedPushDistance = 0.25f;
        [SerializeField, Min(0.01f)] private float parriedPushDuration = 0.12f;
        [SerializeField, Range(0.05f, 0.95f)] private float pushEndSpeedFraction = 0.20f;

        [Header("Optional References")]
        [SerializeField] private CombatHorizontalImpulseDriver impulseDriver;
        [SerializeField] private AudioSource audioSource;

        [Header("Optional VFX")]
        [SerializeField] private ParticleSystem ignoredVfx;
        [SerializeField] private ParticleSystem fullHitVfx;
        [SerializeField] private ParticleSystem blockedVfx;
        [SerializeField] private ParticleSystem parriedVfx;
        [SerializeField] private ParticleSystem endParryVfx;

        [Header("Optional SFX")]
        [SerializeField] private AudioClip ignoredSfx;
        [SerializeField] private AudioClip fullHitSfx;
        [SerializeField] private AudioClip blockedSfx;
        [SerializeField] private AudioClip parriedSfx;
        [SerializeField] private AudioClip endParrySfx;

        public event Action<CombatOutcomeFeedbackContext> OnIgnored;
        public event Action<CombatOutcomeFeedbackContext> OnFullHit;
        public event Action<CombatOutcomeFeedbackContext> OnBlocked;
        public event Action<CombatOutcomeFeedbackContext> OnParried;
        public event Action<CombatOutcomeFeedbackContext> OnEndParried;

        private void Awake()
        {
            ResolveOptionalReferences(allowRuntimeCreate: true);
        }

        private void OnValidate()
        {
            blockedPushDistance = Mathf.Max(0f, blockedPushDistance);
            blockedPushDuration = Mathf.Max(0.01f, blockedPushDuration);
            parriedPushDistance = Mathf.Max(0f, parriedPushDistance);
            parriedPushDuration = Mathf.Max(0.01f, parriedPushDuration);
            ResolveOptionalReferences(allowRuntimeCreate: false);
        }

        private void OnDisable()
        {
            if (impulseDriver != null)
            {
                impulseDriver.StopActiveImpulse();
            }
        }

        public void OnCombatOutcome(CombatOutcomeFeedbackContext context)
        {
            switch (context.Resolution.Outcome)
            {
                case DamageOutcome.Ignored:
                    TriggerVisualAndAudio(ignoredVfx, ignoredSfx);
                    OnIgnored?.Invoke(context);
                    break;
                case DamageOutcome.FullHit:
                    TriggerVisualAndAudio(fullHitVfx, fullHitSfx);
                    OnFullHit?.Invoke(context);
                    break;
                case DamageOutcome.Blocked:
                    TriggerVisualAndAudio(blockedVfx, blockedSfx);
                    OnBlocked?.Invoke(context);
                    StartRecoil(context.DefenderPushDirection, blockedPushDistance, blockedPushDuration);
                    break;
                case DamageOutcome.Parried:
                    ParticleSystem parryVfx = context.IsEndParry && endParryVfx != null ? endParryVfx : parriedVfx;
                    AudioClip parrySfx = context.IsEndParry && endParrySfx != null ? endParrySfx : parriedSfx;
                    TriggerVisualAndAudio(parryVfx, parrySfx);
                    OnParried?.Invoke(context);
                    if (context.IsEndParry)
                    {
                        OnEndParried?.Invoke(context);
                    }
                    StartRecoil(context.DefenderPushDirection, parriedPushDistance, parriedPushDuration);
                    break;
            }
        }

        private void TriggerVisualAndAudio(ParticleSystem vfx, AudioClip clip)
        {
            if (vfx != null)
            {
                vfx.Play(true);
            }

            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        private void StartRecoil(Vector3 inputDirection, float distance, float duration)
        {
            if (!isActiveAndEnabled || distance <= 0f || duration <= 0f || impulseDriver == null)
            {
                return;
            }

            impulseDriver.PlayImpulse(inputDirection, distance, duration, pushEndSpeedFraction);
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
