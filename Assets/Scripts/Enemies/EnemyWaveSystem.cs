using UnityEngine;
using System.Collections;
using System.Collections.Generic;


/// <summary>
/// Manages enemy waves: spawning, tracking, and progression.
/// Usage:
/// 1. Create empty GameObjects in the scene to act as spawn locations. Put them where you want the enemies to be able to spawn.
/// 2. Assign the Enemy Prefab and drag the spawn location GameObjects into the Spawn Points list.
/// </summary>
public class EnemyWaveSystem : MonoBehaviour
{
    [Header("Spawner Settings")]
    [Tooltip("The enemy prefab to spawn.")]
    [SerializeField] private GameObject enemyPrefab;
    [Tooltip("List of transform positions where enemies can spawn. Drag the empty GameObjects that act as spawn location in to here.")]
    [SerializeField] private List<Transform> spawnPoints;

    [Header("Mini-Boss Settings")]
    [Tooltip("The mini-boss prefab to spawn on qualifying waves. Leave empty to disable.")]
    [SerializeField] private GameObject miniBossPrefab;
    [Tooltip("First wave that can spawn a mini-boss.")]
    [SerializeField, Min(1)] private int miniBossStartWave = 3;
    [Tooltip("Spawn a mini-boss every N waves after the start wave.")]
    [SerializeField, Min(1)] private int miniBossEveryNWaves = 3;
    [Tooltip("Number of mini-bosses on the first qualifying wave.")]
    [SerializeField, Min(1)] private int miniBossInitialCount = 1;
    [Tooltip("Additional mini-bosses per subsequent qualifying wave.")]
    [SerializeField, Min(0)] private int miniBossIncreaseAmount = 1;

    [Header("Wave Settings")]
    [Tooltip("Number of enemies in the first wave.")]
    [SerializeField] private int initialEnemies = 1;
    [Tooltip("How many additional enemies to add per subsequent wave.")]
    [SerializeField] private int enemyIncreaseAmount = 1;
    [Tooltip("Time in seconds to wait between waves.")]
    [SerializeField] private float timeBetweenWaves = 30f;
    [Tooltip("Sound to play when new wave spawns")]
    [SerializeField] private AudioClip waveSpawnSound;

    private List<GameObject> activeEnemies = new List<GameObject>();
    private int currentWave = 0;
    private AudioSource audioSource;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        StartCoroutine(WaveRoutine());
    }

    private IEnumerator WaveRoutine()
    {
        // Initial delay before the first wave
        yield return new WaitForSeconds(timeBetweenWaves);

        while (true)
        {
            currentWave++;
            int enemiesToSpawn = initialEnemies + (enemyIncreaseAmount * (currentWave - 1));

            SpawnWave(enemiesToSpawn);

            // Wait until all enemies are dead
            // We verify by checking if the list elements are null (destroyed)
            yield return new WaitUntil(() =>
            {
                activeEnemies.RemoveAll(e => e == null);
                return activeEnemies.Count == 0;
            });

            Debug.Log("All enemies dead, waiting...");

            // Wait for next wave delay
            yield return new WaitForSeconds(timeBetweenWaves);
        }
    }

    private void SpawnWave(int count)
    {
        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            Debug.LogError("No spawn points configured! Can not spawn enemies.");
            return;
        }

        // Ensure we don't try to spawn more than we have points for (unique location rule)
        int actualSpawnCount = Mathf.Min(count, spawnPoints.Count);
        if (count > actualSpawnCount) Debug.LogWarning("We are trying to spawn more enemies that there are spawn points. Add more spawn points if you want to spawn more enemies.");

        // Shuffle indices
        List<int> indices = new List<int>();
        for (int i = 0; i < spawnPoints.Count; i++) indices.Add(i);

        for (int i = 0; i < indices.Count; i++)
        {
            int temp = indices[i];
            int rnd = Random.Range(i, indices.Count);
            indices[i] = indices[rnd];
            indices[rnd] = temp;
        }

        bool isMiniBossWave = miniBossPrefab != null
                              && currentWave >= miniBossStartWave
                              && (currentWave - miniBossStartWave) % miniBossEveryNWaves == 0;
        int miniBossOccurrence = isMiniBossWave
            ? (currentWave - miniBossStartWave) / miniBossEveryNWaves
            : 0;
        int bossCount = isMiniBossWave
            ? Mathf.Min(miniBossInitialCount + (miniBossIncreaseAmount * miniBossOccurrence), actualSpawnCount)
            : 0;
        int normalCount = actualSpawnCount - bossCount;

        int spawnIndex = 0;
        for (int i = 0; i < normalCount; i++, spawnIndex++)
        {
            Transform point = spawnPoints[indices[spawnIndex]];
            if (point != null)
            {
                GameObject enemy = Instantiate(enemyPrefab, point.position, point.rotation);
                enemy.name = $"Enemy_Wave{currentWave}_{spawnIndex + 1}";
                activeEnemies.Add(enemy);
            }
        }

        for (int i = 0; i < bossCount; i++, spawnIndex++)
        {
            Transform point = spawnPoints[indices[spawnIndex]];
            if (point != null)
            {
                GameObject boss = Instantiate(miniBossPrefab, point.position, point.rotation);
                boss.name = $"Kensei_Wave{currentWave}_{i + 1}";
                activeEnemies.Add(boss);
            }
        }
        
        if (audioSource != null && waveSpawnSound != null)
        {
            audioSource.PlayOneShot(waveSpawnSound);
        } else Debug.LogWarning("No spawn sound is being played. Make sure you have added an audio clip and source.");

        Debug.Log("Enemy wave spawned!");
    }

    private void OnDrawGizmosSelected()
    {
        if (spawnPoints == null) return;
        Gizmos.color = Color.red;
        foreach (var point in spawnPoints)
        {
            if (point != null) Gizmos.DrawWireSphere(point.position, 0.5f);
        }
    }
}