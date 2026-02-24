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
    [SerializeField, Min(0f)] private float ragdollImpulseMagnitude = 4f;
    [SerializeField, Range(0f, 1f)] private float ragdollUpwardImpulseFraction = 0.25f;
    [SerializeField, Min(0f)] private float ragdollDestroyDelay = 4f;
    [SerializeField] private string hipsBoneName = "pelvis";

    [Header("VFX")]
    [SerializeField] private GameObject deathVfxPrefab;
    [SerializeField] private Vector3 vfxSpawnOffset = new Vector3(0f, 1f, 0f);

    private HealthComponent health;
    private Enemy enemy;
    private Animator animator;
    private EnemyNavAgentBridge navBridge;
    private Vector3 lastHitFromDirection = Vector3.back;

    private void Awake()
    {
        health = GetComponent<HealthComponent>();
        enemy = GetComponent<Enemy>();
        animator = GetComponent<Animator>();
        navBridge = GetComponent<EnemyNavAgentBridge>();

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
        SpawnDeathVfx();

        if (ragdollPrefab != null)
        {
            SpawnRagdoll();
            Destroy(gameObject);
        }
        else
        {
            if (animator != null)
            {
                animator.CrossFadeInFixedTime("Death", 0.1f);
            }

            Destroy(gameObject, 3f);
        }
    }

    private void SpawnRagdoll()
    {
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

        // Apply an impulse to the hips to sell the direction of the killing blow.
        Rigidbody hipsBody = FindHipsRigidbody(ragdollInstance);
        if (hipsBody != null)
        {
            Vector3 impulse = lastHitFromDirection * ragdollImpulseMagnitude;
            impulse.y += ragdollImpulseMagnitude * ragdollUpwardImpulseFraction;
            hipsBody.AddForce(impulse, ForceMode.Impulse);
        }

        Destroy(ragdollInstance, ragdollDestroyDelay);
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

    private void SpawnDeathVfx()
    {
        if (deathVfxPrefab == null)
        {
            return;
        }

        Vector3 spawnPos = transform.position + vfxSpawnOffset;
        GameObject vfxInstance = Instantiate(deathVfxPrefab, spawnPos, Quaternion.identity);

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
}
