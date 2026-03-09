using System.Collections;
using UnityEngine;
using Player.StateMachine;

namespace Combat
{
    [DisallowMultipleComponent]
    public sealed class CombatHorizontalImpulseDriver : MonoBehaviour
    {
        [Header("Ground Behavior")]
        [SerializeField] private bool suppressAirtimeWhenGrounded = true;

        [Header("Optional References")]
        [SerializeField] private CharacterMotor characterMotor;
        [SerializeField] private Rigidbody fallbackRigidbody;

        private static readonly WaitForFixedUpdate FixedStepYield = new WaitForFixedUpdate();
        private Coroutine impulseRoutine;

        public bool IsImpulseActive => impulseRoutine != null;

        private void Awake()
        {
            ResolveOptionalReferences();
        }

        private void OnValidate()
        {
            ResolveOptionalReferences();
        }

        private void OnDisable()
        {
            StopActiveImpulse();
        }

        public bool PlayImpulse(Vector3 inputDirection, float distance, float duration, float endSpeedFraction)
        {
            if (!isActiveAndEnabled || distance <= 0f || duration <= 0f || !HasMovementBackend())
            {
                return false;
            }

            Vector3 direction = ResolveDirection(inputDirection);
            if (direction.sqrMagnitude < 0.0001f)
            {
                return false;
            }

            float clampedEndSpeed = Mathf.Clamp(endSpeedFraction, 0.05f, 0.95f);

            if (impulseRoutine != null)
            {
                StopCoroutine(impulseRoutine);
                impulseRoutine = null;
            }

            ClearHorizontalMotion();
            impulseRoutine = StartCoroutine(ApplyImpulse(direction, distance, duration, clampedEndSpeed));
            return true;
        }

        public void StopActiveImpulse()
        {
            if (impulseRoutine != null)
            {
                StopCoroutine(impulseRoutine);
                impulseRoutine = null;
            }

            ClearHorizontalMotion();
        }

        private IEnumerator ApplyImpulse(Vector3 direction, float distance, float duration, float endSpeedFraction)
        {
            float elapsed = 0f;
            float remainingDistance = distance;
            float decay = -Mathf.Log(endSpeedFraction) / duration;
            float initialSpeed;

            if (decay > 0f)
            {
                float denominator = 1f - Mathf.Exp(-decay * duration);
                initialSpeed = denominator <= 0f ? distance / duration : (distance * decay / denominator);
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
                float stepDistance;

                if (decay > 0f)
                {
                    float exp0 = Mathf.Exp(-decay * t0);
                    float exp1 = Mathf.Exp(-decay * t1);
                    stepDistance = (initialSpeed / decay) * (exp0 - exp1);
                }
                else
                {
                    float remainingTime = Mathf.Max(duration - t0, 0.0001f);
                    stepDistance = remainingDistance * (t1 - t0) / remainingTime;
                }

                stepDistance = Mathf.Min(stepDistance, remainingDistance);
                SetHorizontalMotion(direction * (stepDistance / dt));

                elapsed = t1;
                remainingDistance -= stepDistance;
            }

            ClearHorizontalMotion();
            impulseRoutine = null;
        }

        private void SetHorizontalMotion(Vector3 velocity)
        {
            if (characterMotor != null)
            {
                characterMotor.SetHorizontalVelocity(velocity, suppressAirtimeWhenGrounded);
                return;
            }

            if (fallbackRigidbody == null)
            {
                return;
            }

            if (fallbackRigidbody.isKinematic)
            {
                MoveKinematicBody(velocity);
                return;
            }

            Vector3 current = fallbackRigidbody.linearVelocity;
            current.x = velocity.x;
            current.z = velocity.z;
            fallbackRigidbody.linearVelocity = current;
        }

        private void MoveKinematicBody(Vector3 velocity)
        {
            float dt = Time.inFixedTimeStep ? Time.fixedDeltaTime : Time.deltaTime;
            if (dt <= 0f)
            {
                return;
            }

            Vector3 horizontalDisplacement = new Vector3(velocity.x, 0f, velocity.z) * dt;
            if (horizontalDisplacement.sqrMagnitude <= 0f)
            {
                return;
            }

            fallbackRigidbody.MovePosition(fallbackRigidbody.position + horizontalDisplacement);
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

        private Vector3 ResolveDirection(Vector3 inputDirection)
        {
            inputDirection.y = 0f;
            if (inputDirection.sqrMagnitude > 0.0001f)
            {
                return inputDirection.normalized;
            }

            Vector3 fallback = -transform.forward;
            fallback.y = 0f;
            return fallback.sqrMagnitude > 0.0001f ? fallback.normalized : Vector3.back;
        }
    }

}
