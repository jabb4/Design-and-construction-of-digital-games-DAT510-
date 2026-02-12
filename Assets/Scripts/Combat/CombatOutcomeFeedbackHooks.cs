using System;
using System.Collections;
using UnityEngine;
using Player.StateMachine;

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
        [SerializeField] private CharacterMotor characterMotor;
        [SerializeField] private Rigidbody fallbackRigidbody;
        [SerializeField] private AudioSource audioSource;

        [Header("Optional VFX")]
        [SerializeField] private ParticleSystem ignoredVfx;
        [SerializeField] private ParticleSystem fullHitVfx;
        [SerializeField] private ParticleSystem blockedVfx;
        [SerializeField] private ParticleSystem parriedVfx;

        [Header("Optional SFX")]
        [SerializeField] private AudioClip ignoredSfx;
        [SerializeField] private AudioClip fullHitSfx;
        [SerializeField] private AudioClip blockedSfx;
        [SerializeField] private AudioClip parriedSfx;

        public event Action<CombatOutcomeFeedbackContext> OnIgnored;
        public event Action<CombatOutcomeFeedbackContext> OnFullHit;
        public event Action<CombatOutcomeFeedbackContext> OnBlocked;
        public event Action<CombatOutcomeFeedbackContext> OnParried;

        private static readonly WaitForFixedUpdate FixedStepYield = new WaitForFixedUpdate();
        private Coroutine recoilRoutine;

        private void Awake()
        {
            ResolveOptionalReferences();
        }

        private void OnValidate()
        {
            blockedPushDistance = Mathf.Max(0f, blockedPushDistance);
            blockedPushDuration = Mathf.Max(0.01f, blockedPushDuration);
            parriedPushDistance = Mathf.Max(0f, parriedPushDistance);
            parriedPushDuration = Mathf.Max(0.01f, parriedPushDuration);
        }

        private void OnDisable()
        {
            if (recoilRoutine != null)
            {
                StopCoroutine(recoilRoutine);
                recoilRoutine = null;
            }

            ClearHorizontalMotion();
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
                    TriggerVisualAndAudio(parriedVfx, parriedSfx);
                    OnParried?.Invoke(context);
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
            if (!isActiveAndEnabled || !HasMovementBackend() || distance <= 0f || duration <= 0f)
            {
                return;
            }

            Vector3 direction = FlattenDirection(inputDirection);
            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            if (recoilRoutine != null)
            {
                StopCoroutine(recoilRoutine);
                recoilRoutine = null;
            }

            ClearHorizontalMotion();
            recoilRoutine = StartCoroutine(ApplyRecoil(direction, distance, duration));
        }

        private IEnumerator ApplyRecoil(Vector3 direction, float distance, float duration)
        {
            float elapsed = 0f;
            float remainingDistance = distance;
            float endFraction = Mathf.Clamp(pushEndSpeedFraction, 0.05f, 0.95f);
            float decay = -Mathf.Log(endFraction) / duration;
            float initialSpeed;

            if (decay > 0f)
            {
                float denom = 1f - Mathf.Exp(-decay * duration);
                initialSpeed = denom <= 0f ? distance / duration : (distance * decay / denom);
            }
            else
            {
                initialSpeed = distance / duration;
            }

            while (elapsed < duration && remainingDistance > 0.0001f)
            {
                yield return FixedStepYield;

                float dt = Time.fixedDeltaTime;
                if (dt <= 0f)
                {
                    continue;
                }

                float t0 = elapsed;
                float t1 = Mathf.Min(t0 + dt, duration);
                float distanceThisStep;

                if (decay > 0f)
                {
                    float exp0 = Mathf.Exp(-decay * t0);
                    float exp1 = Mathf.Exp(-decay * t1);
                    distanceThisStep = (initialSpeed / decay) * (exp0 - exp1);
                }
                else
                {
                    float remainingTime = Mathf.Max(duration - t0, 0.0001f);
                    distanceThisStep = remainingDistance * (t1 - t0) / remainingTime;
                }

                distanceThisStep = Mathf.Min(distanceThisStep, remainingDistance);
                float speed = distanceThisStep / dt;
                SetHorizontalMotion(direction * speed);

                elapsed = t1;
                remainingDistance -= distanceThisStep;
            }

            ClearHorizontalMotion();
            recoilRoutine = null;
        }

        private void SetHorizontalMotion(Vector3 velocity)
        {
            if (characterMotor != null)
            {
                characterMotor.SetHorizontalVelocity(velocity);
                return;
            }

            if (fallbackRigidbody == null)
            {
                return;
            }

            Vector3 current = fallbackRigidbody.linearVelocity;
            current.x = velocity.x;
            current.z = velocity.z;
            fallbackRigidbody.linearVelocity = current;
        }

        private void ClearHorizontalMotion()
        {
            SetHorizontalMotion(Vector3.zero);
        }

        private bool HasMovementBackend()
        {
            return characterMotor != null || fallbackRigidbody != null;
        }

        private void ResolveOptionalReferences()
        {
            if (characterMotor == null)
            {
                characterMotor = GetComponent<CharacterMotor>();
            }

            if (fallbackRigidbody == null)
            {
                fallbackRigidbody = GetComponent<Rigidbody>();
            }
        }

        private Vector3 FlattenDirection(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
            {
                return direction.normalized;
            }

            Vector3 fallback = -transform.forward;
            fallback.y = 0f;
            if (fallback.sqrMagnitude > 0.0001f)
            {
                return fallback.normalized;
            }

            return Vector3.zero;
        }
    }
}
