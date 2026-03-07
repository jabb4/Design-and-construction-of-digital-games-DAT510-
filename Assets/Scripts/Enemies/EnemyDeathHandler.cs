using System.Collections.Generic;
using Combat;
using Enemies.AI;
using UnityEngine;

[RequireComponent(typeof(HealthComponent))]
public class EnemyDeathHandler : MonoBehaviour
{
    [Header("Ragdoll")]
    [Tooltip("A prefab containing only the skeleton with Rigidbodies, CharacterJoints, and Colliders. " +
             "No Animator, NavMeshAgent, or enemy scripts.")]
    [SerializeField] private GameObject ragdollPrefab;
    [SerializeField, Min(0f)] private float ragdollImpulseMagnitude = 8f;
    [SerializeField, Range(0f, 1f)] private float ragdollUpwardImpulseFraction = 0.4f;
    [SerializeField, Range(0f, 1f)] private float ragdollSpreadFraction = 0.5f;
    [SerializeField, Min(0f)] private float ragdollDestroyDelay = 4f;
    [SerializeField] private string hipsBoneName = "pelvis";

    [Header("Dissolve on Removal")]
    [Tooltip("After the ragdoll settles, dissolve it away with glowing edges instead of vanishing instantly.")]
    [SerializeField] private bool dissolveOnRemoval = true;
    [SerializeField, Min(0f)] private float dissolveDuration = 2f;
    [SerializeField] private Shader dissolveShader;

    [Header("VFX")]
    [SerializeField] private GameObject deathVfxPrefab;
    [SerializeField] private string vfxBoneName = "spine_03";

    [Header("SFX")]
    [SerializeField] private AudioSource deathAudioSource;
    [SerializeField] private AudioClip deathSfx;
    [SerializeField, Range(0f, 2f)] private float deathSfxVolume = 1f;
    [SerializeField, Range(0.5f, 2f)] private float deathSfxMinPitch = 0.96f;
    [SerializeField, Range(0.5f, 2f)] private float deathSfxMaxPitch = 1.04f;
    [SerializeField] private bool use3dDeathSfx = true;

    private HealthComponent health;
    private Enemy enemy;
    private Animator animator;
    private EnemyNavAgentBridge navBridge;
    private UnityEngine.AI.NavMeshAgent navMeshAgent;
    private Vector3 lastHitFromDirection = Vector3.back;

    private void Awake()
    {
        health = GetComponent<HealthComponent>();
        enemy = GetComponent<Enemy>();
        animator = GetComponent<Animator>();
        navBridge = GetComponent<EnemyNavAgentBridge>();
        navMeshAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();

        if (health != null)
        {
            health.OnDied += HandleDied;
        }

        if (enemy != null)
        {
            enemy.OnDamageResolved += HandleDamageResolved;
        }
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.OnDied -= HandleDied;
        }

        if (enemy != null)
        {
            enemy.OnDamageResolved -= HandleDamageResolved;
        }
    }

    private void HandleDamageResolved(AttackHitInfo hit, DamageResolution resolution)
    {
        if (hit.Attacker is Component attackerComponent)
        {
            Vector3 dir = transform.position - attackerComponent.transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
            {
                lastHitFromDirection = dir.normalized;
            }
        }
    }

    private void HandleDied()
    {
        enemy?.NotifyDeath();
        navBridge?.Stop();

        if (navMeshAgent != null) navMeshAgent.enabled = false;
        if (animator != null) animator.enabled = false;

        if (ragdollPrefab != null)
        {
            Transform hips = SpawnRagdoll(out Transform vfxBone);
            Transform followBone = vfxBone != null ? vfxBone : hips;
            PlayDeathSfx(followBone);
            SpawnDeathVfx(vfxBone != null ? vfxBone : hips);
            Destroy(gameObject);
        }
        else
        {
            PlayDeathSfx(null);
            SpawnDeathVfx(null);

            if (animator != null)
            {
                animator.CrossFadeInFixedTime("Death", 0.1f);
            }

            Destroy(gameObject, 3f);
        }
    }

    private Transform SpawnRagdoll(out Transform vfxAttachBone)
    {
        vfxAttachBone = null;

        // Build a name → transform lookup from the live skeleton in O(n).
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

        // Copy the animated pose into the ragdoll so it starts in the correct position.
        foreach (Transform ragdollBone in ragdollInstance.GetComponentsInChildren<Transform>(true))
        {
            if (boneMap.TryGetValue(ragdollBone.name, out Transform sourceBone))
            {
                ragdollBone.position = sourceBone.position;
                ragdollBone.rotation = sourceBone.rotation;
            }
        }

        // Reparent Weapon_l to Scabbard_Target01 so the scabbard stays at the hip
        // instead of following hand_l during ragdoll physics.
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

        if (dissolveOnRemoval && dissolveShader != null)
        {
            var dissolve = ragdollInstance.AddComponent<RagdollDissolveEffect>();
            dissolve.Init(ragdollDestroyDelay, dissolveDuration, dissolveShader);
        }
        else
        {
            Destroy(ragdollInstance, ragdollDestroyDelay);
        }

        Rigidbody hipsRb = hipsBody != null ? hipsBody : ragdollInstance.GetComponentInChildren<Rigidbody>();
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

    private void PlayDeathSfx(Transform followTarget)
    {
        if (deathSfx == null)
        {
            return;
        }

        float minPitch = Mathf.Clamp(deathSfxMinPitch, 0.5f, 2f);
        float maxPitch = Mathf.Clamp(deathSfxMaxPitch, 0.5f, 2f);
        if (maxPitch < minPitch)
        {
            float temp = minPitch;
            minPitch = maxPitch;
            maxPitch = temp;
        }

        float pitch = Random.Range(minPitch, maxPitch);
        float volume = Mathf.Clamp(deathSfxVolume, 0f, 2f);

        AudioSource sourceToUse = deathAudioSource;
        bool destroyEmitterAfterPlayback = false;

        // If the configured source lives on this enemy hierarchy, it will be destroyed
        // with the enemy during ragdoll death and the one-shot gets cut off.
        if (sourceToUse == null || IsSourceOnThisEnemyHierarchy(sourceToUse))
        {
            sourceToUse = CreateTemporaryDeathAudioEmitter(followTarget, deathAudioSource);
            destroyEmitterAfterPlayback = sourceToUse != null;
        }

        if (sourceToUse == null)
        {
            return;
        }

        sourceToUse.pitch = pitch;
        sourceToUse.PlayOneShot(deathSfx, volume);

        float lifeTime = deathSfx.length / Mathf.Max(0.01f, pitch);
        if (destroyEmitterAfterPlayback)
        {
            Destroy(sourceToUse.gameObject, lifeTime + 0.1f);
        }
    }

    private bool IsSourceOnThisEnemyHierarchy(AudioSource source)
    {
        if (source == null)
        {
            return false;
        }

        Transform sourceTransform = source.transform;
        return sourceTransform == transform || sourceTransform.IsChildOf(transform);
    }

    private AudioSource CreateTemporaryDeathAudioEmitter(Transform followTarget, AudioSource template)
    {
        Transform anchor = followTarget != null ? followTarget : transform;
        GameObject emitterObject = new GameObject("EnemyDeathSfx");
        emitterObject.transform.position = anchor.position;
        if (followTarget != null)
        {
            emitterObject.transform.SetParent(followTarget, true);
        }

        AudioSource source = emitterObject.AddComponent<AudioSource>();
        source.playOnAwake = false;

        if (template != null)
        {
            source.outputAudioMixerGroup = template.outputAudioMixerGroup;
            source.mute = template.mute;
            source.bypassEffects = template.bypassEffects;
            source.bypassListenerEffects = template.bypassListenerEffects;
            source.bypassReverbZones = template.bypassReverbZones;
            source.priority = template.priority;
            source.volume = template.volume;
            source.panStereo = template.panStereo;
            source.spatialBlend = template.spatialBlend;
            source.reverbZoneMix = template.reverbZoneMix;
            source.dopplerLevel = template.dopplerLevel;
            source.spread = template.spread;
            source.minDistance = template.minDistance;
            source.maxDistance = template.maxDistance;
            source.rolloffMode = template.rolloffMode;
            source.velocityUpdateMode = template.velocityUpdateMode;
            source.ignoreListenerPause = template.ignoreListenerPause;
            source.ignoreListenerVolume = template.ignoreListenerVolume;
            source.spatialize = template.spatialize;
            source.spatializePostEffects = template.spatializePostEffects;
            source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, template.GetCustomCurve(AudioSourceCurveType.CustomRolloff));
            source.SetCustomCurve(AudioSourceCurveType.Spread, template.GetCustomCurve(AudioSourceCurveType.Spread));
            source.SetCustomCurve(AudioSourceCurveType.ReverbZoneMix, template.GetCustomCurve(AudioSourceCurveType.ReverbZoneMix));
        }
        else
        {
            source.spatialBlend = use3dDeathSfx ? 1f : 0f;
        }

        return source;
    }
}
