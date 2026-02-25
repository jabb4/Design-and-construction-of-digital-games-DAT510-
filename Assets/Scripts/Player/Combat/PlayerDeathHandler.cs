using System;
using Combat;
using Player.StateMachine;
using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(HealthComponent))]
public class PlayerDeathHandler : MonoBehaviour, ICombatOutcomeFeedbackHook
{
    [Header("Ragdoll")]
    [Tooltip("A prefab containing only the skeleton with Rigidbodies, CharacterJoints, and Colliders. " +
             "No Animator or player scripts.")]
    [SerializeField] private GameObject ragdollPrefab;
    [SerializeField, Min(0f)] private float ragdollImpulseMagnitude = 8f;
    [SerializeField, Range(0f, 1f)] private float ragdollUpwardImpulseFraction = 0.4f;
    [SerializeField, Range(0f, 1f)] private float ragdollSpreadFraction = 0.5f;
    [SerializeField, Min(0f)] private float ragdollDestroyDelay = 8f;
    [SerializeField] private string hipsBoneName = "pelvis";

    [Header("Camera")]
    [SerializeField] private CameraController cameraController;

    [Header("VFX")]
    [SerializeField] private GameObject deathVfxPrefab;
    [SerializeField] private string vfxBoneName = "spine_03";

    [Header("Screen Fade")]
    [SerializeField, Min(0f)] private float fadeDelay = 1f;
    [SerializeField, Min(0.01f)] private float fadeDuration = 6f;
    [SerializeField] private string targetScene = "VanView";
    [SerializeField, Min(0.01f)] private float fadeOutDuration = 1f;

    public static Transform ActiveRagdoll { get; private set; }

    public event Action OnPlayerDied;

    private HealthComponent health;
    private PlayerInputHandler input;
    private PlayerStateMachine stateMachine;
    private Vector3 lastHitFromDirection = Vector3.back;

    private void Awake()
    {
        health = GetComponent<HealthComponent>();
        input = GetComponent<PlayerInputHandler>();
        stateMachine = GetComponent<PlayerStateMachine>();

        if (health != null)
        {
            health.OnDied += HandleDied;
        }
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.OnDied -= HandleDied;
        }
    }

    public void OnCombatOutcome(CombatOutcomeFeedbackContext context)
    {
        if (context.Resolution.Outcome == DamageOutcome.FullHit)
        {
            Vector3 dir = context.DefenderPushDirection;
            if (dir.sqrMagnitude > 0.0001f)
            {
                lastHitFromDirection = dir.normalized;
            }
        }
    }

    private void HandleDied()
    {
        if (input != null)
        {
            input.enabled = false;
        }

        if (stateMachine != null)
        {
            stateMachine.enabled = false;
        }

        if (ragdollPrefab != null)
        {
            Transform hips = SpawnRagdoll(out Transform vfxBone);
            Transform followBone = vfxBone != null ? vfxBone : hips;
            SpawnDeathVfx(followBone);

            if (cameraController != null)
            {
                cameraController.SetFollowTarget(followBone);
            }

            SpawnDeathFade();
            OnPlayerDied?.Invoke();
            Destroy(gameObject);
        }
        else
        {
            SpawnDeathVfx(null);
            SpawnDeathFade();
            OnPlayerDied?.Invoke();
            Destroy(gameObject, ragdollDestroyDelay);
        }
    }

    private Transform SpawnRagdoll(out Transform vfxAttachBone)
    {
        vfxAttachBone = null;

        Transform[] sourceBones = GetComponentsInChildren<Transform>(true);
        var boneMap = new Dictionary<string, Transform>(sourceBones.Length);
        foreach (Transform bone in sourceBones)
        {
            if (!boneMap.ContainsKey(bone.name))
            {
                boneMap[bone.name] = bone;
            }
        }

        GameObject ragdollInstance = Instantiate(ragdollPrefab, transform.position, transform.rotation);

        foreach (Transform ragdollBone in ragdollInstance.GetComponentsInChildren<Transform>(true))
        {
            if (boneMap.TryGetValue(ragdollBone.name, out Transform sourceBone))
            {
                ragdollBone.position = sourceBone.position;
                ragdollBone.rotation = sourceBone.rotation;
            }
        }

        // Ragdoll-side scabbard fix: reparent Weapon_l under Scabbard_Target01 so the
        // scabbard stays at the hip during physics. The live skeleton uses
        // ScabbardBoneOverride (LateUpdate snap) for the same fix, but ragdolls have
        // no Animator so we solve it with a one-time hierarchy reparent instead.
        Transform ragdollWeaponL = null;
        Transform ragdollScabbardTarget = null;
        foreach (Transform t in ragdollInstance.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == "Weapon_l") ragdollWeaponL = t;
            else if (t.name == "Scabbard_Target01") ragdollScabbardTarget = t;
            else if (t.name == vfxBoneName) vfxAttachBone = t;
        }
        if (ragdollWeaponL != null && ragdollScabbardTarget != null)
        {
            ragdollWeaponL.SetParent(ragdollScabbardTarget, true);
        }

        // Apply an impulse to all ragdoll bodies to sell the direction of the killing blow.
        Vector3 baseImpulse = lastHitFromDirection * ragdollImpulseMagnitude;
        baseImpulse.y += ragdollImpulseMagnitude * ragdollUpwardImpulseFraction;

        Rigidbody hipsBody = FindHipsRigidbody(ragdollInstance);
        foreach (Rigidbody rb in ragdollInstance.GetComponentsInChildren<Rigidbody>())
        {
            if (rb == hipsBody)
            {
                rb.AddForce(baseImpulse, ForceMode.Impulse);
            }
            else
            {
                rb.AddForce(baseImpulse * ragdollSpreadFraction, ForceMode.Impulse);
            }
        }

        Destroy(ragdollInstance, ragdollDestroyDelay);

        Rigidbody hipsRb = hipsBody != null ? hipsBody : ragdollInstance.GetComponentInChildren<Rigidbody>();
        ActiveRagdoll = hipsRb != null ? hipsRb.transform : ragdollInstance.transform;
        return hipsRb != null ? hipsRb.transform : ragdollInstance.transform;
    }

    private Rigidbody FindHipsRigidbody(GameObject ragdollInstance)
    {
        foreach (Transform child in ragdollInstance.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == hipsBoneName)
            {
                Rigidbody rb = child.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    return rb;
                }
            }
        }

        return ragdollInstance.GetComponentInChildren<Rigidbody>();
    }

    private void SpawnDeathVfx(Transform followTarget)
    {
        if (deathVfxPrefab == null)
        {
            return;
        }

        Vector3 spawnPos = followTarget != null
            ? followTarget.position
            : transform.position;
        Quaternion spawnRot = lastHitFromDirection.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(lastHitFromDirection)
            : Quaternion.identity;
        GameObject vfxInstance = Instantiate(deathVfxPrefab, spawnPos, spawnRot);

        if (followTarget != null)
        {
            vfxInstance.transform.SetParent(followTarget, true);
        }

        ParticleSystem ps = vfxInstance.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            var main = ps.main;
            Destroy(vfxInstance, main.duration + main.startLifetime.constantMax);
        }
        else
        {
            Destroy(vfxInstance, ragdollDestroyDelay + 1f);
        }
    }

    private void SpawnDeathFade()
    {
        ScreenFade.Create(fadeDelay, fadeDuration, targetScene, fadeOutDuration);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        ActiveRagdoll = null;
    }
}
