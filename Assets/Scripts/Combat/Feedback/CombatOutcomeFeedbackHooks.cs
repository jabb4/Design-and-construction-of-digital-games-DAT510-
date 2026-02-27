using System;
using System.Collections.Generic;
using UnityEngine;

namespace Combat
{
    [DisallowMultipleComponent]
    public sealed class CombatOutcomeFeedbackHooks : MonoBehaviour, ICombatOutcomeFeedbackHook
    {
        [Serializable]
        private struct AudioVariationSettings
        {
            [Range(0f, 2f)] public float minVolume;
            [Range(0f, 2f)] public float maxVolume;
            [Range(0.5f, 2f)] public float minPitch;
            [Range(0.5f, 2f)] public float maxPitch;

            public void Clamp()
            {
                minVolume = Mathf.Clamp(minVolume, 0f, 2f);
                maxVolume = Mathf.Clamp(maxVolume, 0f, 2f);
                minPitch = Mathf.Clamp(minPitch, 0.5f, 2f);
                maxPitch = Mathf.Clamp(maxPitch, 0.5f, 2f);

                if (maxVolume < minVolume)
                {
                    float temp = minVolume;
                    minVolume = maxVolume;
                    maxVolume = temp;
                }

                if (maxPitch < minPitch)
                {
                    float temp = minPitch;
                    minPitch = maxPitch;
                    maxPitch = temp;
                }
            }
        }

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

        [Header("SFX Variation")]
        [SerializeField, Range(1, 8)] private int audioVoiceCount = 5;
        [SerializeField] private AudioVariationSettings genericSfxVariation = new AudioVariationSettings
        {
            minVolume = 0.95f,
            maxVolume = 1f,
            minPitch = 0.98f,
            maxPitch = 1.03f
        };
        [SerializeField] private AudioVariationSettings blockedSfxVariation = new AudioVariationSettings
        {
            minVolume = 0.92f,
            maxVolume = 1f,
            minPitch = 0.94f,
            maxPitch = 1.01f
        };
        [SerializeField] private AudioVariationSettings parriedSfxVariation = new AudioVariationSettings
        {
            minVolume = 0.94f,
            maxVolume = 1f,
            minPitch = 0.98f,
            maxPitch = 1.08f
        };
        [SerializeField] private AudioVariationSettings endParrySfxVariation = new AudioVariationSettings
        {
            minVolume = 0.98f,
            maxVolume = 1f,
            minPitch = 1.05f,
            maxPitch = 1.14f
        };

        public event Action<CombatOutcomeFeedbackContext> OnIgnored;
        public event Action<CombatOutcomeFeedbackContext> OnFullHit;
        public event Action<CombatOutcomeFeedbackContext> OnBlocked;
        public event Action<CombatOutcomeFeedbackContext> OnParried;
        public event Action<CombatOutcomeFeedbackContext> OnEndParried;

        private readonly List<AudioSource> runtimeAudioVoices = new List<AudioSource>(8);
        private int nextAudioVoiceIndex;

        private void Awake()
        {
            ResolveOptionalReferences(allowRuntimeCreate: true);
            ClampSerializedFields();
            RebuildAudioVoices(allowRuntimeCreate: true);
        }

        private void OnValidate()
        {
            ClampSerializedFields();
            ResolveOptionalReferences(allowRuntimeCreate: false);
            RebuildAudioVoices(allowRuntimeCreate: false);
        }

        private void OnDisable()
        {
            if (impulseDriver != null)
            {
                impulseDriver.StopActiveImpulse();
            }

            for (int i = 0; i < runtimeAudioVoices.Count; i++)
            {
                runtimeAudioVoices[i]?.Stop();
            }
        }

        public void OnCombatOutcome(CombatOutcomeFeedbackContext context)
        {
            switch (context.Resolution.Outcome)
            {
                case DamageOutcome.Ignored:
                    TriggerVisualAndAudio(ignoredVfx, ignoredSfx, genericSfxVariation);
                    OnIgnored?.Invoke(context);
                    break;
                case DamageOutcome.FullHit:
                    TriggerVisualAndAudioAtPoint(
                        fullHitVfx,
                        fullHitSfx,
                        genericSfxVariation,
                        context.HitPoint,
                        context.HitNormal);
                    OnFullHit?.Invoke(context);
                    break;
                case DamageOutcome.Blocked:
                    TriggerVisualAndAudio(blockedVfx, blockedSfx, blockedSfxVariation);
                    OnBlocked?.Invoke(context);
                    StartRecoil(context.DefenderPushDirection, blockedPushDistance, blockedPushDuration);
                    break;
                case DamageOutcome.Parried:
                    ParticleSystem parryVfx = context.IsEndParry && endParryVfx != null ? endParryVfx : parriedVfx;
                    AudioClip parrySfx = context.IsEndParry && endParrySfx != null ? endParrySfx : parriedSfx;
                    if (context.IsEndParry)
                    {
                        TriggerVisualAndAudio(parryVfx, parrySfx, endParrySfxVariation);
                    }
                    else
                    {
                        TriggerVisualAndAudio(parryVfx, parrySfx, parriedSfxVariation);
                    }
                    OnParried?.Invoke(context);
                    if (context.IsEndParry)
                    {
                        OnEndParried?.Invoke(context);
                    }
                    StartRecoil(context.DefenderPushDirection, parriedPushDistance, parriedPushDuration);
                    break;
            }
        }

        private void TriggerVisualAndAudio(
            ParticleSystem vfx,
            AudioClip clip,
            AudioVariationSettings variation)
        {
            if (vfx != null)
            {
                vfx.Play(true);
            }

            PlayAudioVariant(clip, variation);
        }

        private void TriggerVisualAndAudioAtPoint(
            ParticleSystem vfx,
            AudioClip clip,
            AudioVariationSettings variation,
            Vector3 worldPos,
            Vector3 hitNormal = default)
        {
            if (vfx != null)
            {
                vfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                vfx.transform.position = worldPos;
                if (hitNormal != Vector3.zero)
                    vfx.transform.rotation = Quaternion.LookRotation(hitNormal);
                vfx.Play(true);
            }

            PlayAudioVariant(clip, variation);
        }

        private void StartRecoil(Vector3 inputDirection, float distance, float duration)
        {
            if (!isActiveAndEnabled || distance <= 0f || duration <= 0f || impulseDriver == null)
            {
                return;
            }

            impulseDriver.PlayImpulse(inputDirection, distance, duration, pushEndSpeedFraction);
        }

        private void PlayAudioVariant(AudioClip clip, AudioVariationSettings variation)
        {
            if (clip == null || audioSource == null)
            {
                return;
            }

            variation.Clamp();

            AudioSource voice = AcquireAudioVoice();
            if (voice == null)
            {
                return;
            }

            voice.pitch = UnityEngine.Random.Range(variation.minPitch, variation.maxPitch);
            voice.PlayOneShot(clip, UnityEngine.Random.Range(variation.minVolume, variation.maxVolume));
        }

        private AudioSource AcquireAudioVoice()
        {
            if (runtimeAudioVoices.Count == 0)
            {
                return audioSource;
            }

            for (int i = 0; i < runtimeAudioVoices.Count; i++)
            {
                int index = (nextAudioVoiceIndex + i) % runtimeAudioVoices.Count;
                AudioSource candidate = runtimeAudioVoices[index];
                if (candidate == null)
                {
                    continue;
                }

                if (!candidate.isPlaying)
                {
                    nextAudioVoiceIndex = (index + 1) % runtimeAudioVoices.Count;
                    return candidate;
                }
            }

            AudioSource fallback = runtimeAudioVoices[nextAudioVoiceIndex % runtimeAudioVoices.Count];
            nextAudioVoiceIndex = (nextAudioVoiceIndex + 1) % runtimeAudioVoices.Count;
            return fallback;
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

        private void RebuildAudioVoices(bool allowRuntimeCreate)
        {
            runtimeAudioVoices.Clear();
            nextAudioVoiceIndex = 0;

            if (audioSource == null)
            {
                return;
            }

            runtimeAudioVoices.Add(audioSource);
            if (!allowRuntimeCreate)
            {
                return;
            }

            int targetVoices = Mathf.Clamp(audioVoiceCount, 1, 8);
            for (int i = 1; i < targetVoices; i++)
            {
                AudioSource extraVoice = gameObject.AddComponent<AudioSource>();
                CopyAudioSourceSettings(audioSource, extraVoice);
                runtimeAudioVoices.Add(extraVoice);
            }
        }

        private static void CopyAudioSourceSettings(AudioSource from, AudioSource to)
        {
            if (from == null || to == null)
            {
                return;
            }

            to.outputAudioMixerGroup = from.outputAudioMixerGroup;
            to.mute = from.mute;
            to.bypassEffects = from.bypassEffects;
            to.bypassListenerEffects = from.bypassListenerEffects;
            to.bypassReverbZones = from.bypassReverbZones;
            to.playOnAwake = false;
            to.loop = false;
            to.priority = from.priority;
            to.volume = from.volume;
            to.pitch = from.pitch;
            to.panStereo = from.panStereo;
            to.spatialBlend = from.spatialBlend;
            to.reverbZoneMix = from.reverbZoneMix;
            to.dopplerLevel = from.dopplerLevel;
            to.spread = from.spread;
            to.minDistance = from.minDistance;
            to.maxDistance = from.maxDistance;
            to.rolloffMode = from.rolloffMode;
            to.velocityUpdateMode = from.velocityUpdateMode;
            to.ignoreListenerPause = from.ignoreListenerPause;
            to.ignoreListenerVolume = from.ignoreListenerVolume;
            to.spatialize = from.spatialize;
            to.spatializePostEffects = from.spatializePostEffects;
            to.SetCustomCurve(AudioSourceCurveType.CustomRolloff, from.GetCustomCurve(AudioSourceCurveType.CustomRolloff));
            to.SetCustomCurve(AudioSourceCurveType.Spread, from.GetCustomCurve(AudioSourceCurveType.Spread));
            to.SetCustomCurve(AudioSourceCurveType.ReverbZoneMix, from.GetCustomCurve(AudioSourceCurveType.ReverbZoneMix));
        }

        private void ClampSerializedFields()
        {
            blockedPushDistance = Mathf.Max(0f, blockedPushDistance);
            blockedPushDuration = Mathf.Max(0.01f, blockedPushDuration);
            parriedPushDistance = Mathf.Max(0f, parriedPushDistance);
            parriedPushDuration = Mathf.Max(0.01f, parriedPushDuration);
            pushEndSpeedFraction = Mathf.Clamp(pushEndSpeedFraction, 0.05f, 0.95f);
            audioVoiceCount = Mathf.Clamp(audioVoiceCount, 1, 8);

            genericSfxVariation.Clamp();
            blockedSfxVariation.Clamp();
            parriedSfxVariation.Clamp();
            endParrySfxVariation.Clamp();
        }
    }
}
