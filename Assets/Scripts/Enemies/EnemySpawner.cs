using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    [SerializeField] private GameObject enemyPrefab;  // Drag your Enemy.prefab here in the Inspector
    [SerializeField] private int numberOfEnemies = 5;
    [SerializeField] private float spawnRadius = 10f;
    [SerializeField] private Vector3 spawnCenter = Vector3.zero;

    private void Start()
    {
        SpawnEnemies();
    }

    private void SpawnEnemies()
    {
        for (int i = 0; i < numberOfEnemies; i++)
        {
            // Generate random position within spawn radius
            Vector3 randomPos = spawnCenter + Random.insideUnitSphere * spawnRadius;
            randomPos.y = 0; // Keep on ground level, adjust if needed

            // Instantiate from prefab
            GameObject enemyGO = Instantiate(enemyPrefab, randomPos, Quaternion.identity);

            // Name the enemy for clarity
            enemyGO.name = $"Enemy_{i + 1}";
        }
    }

    // Visualize spawn area in editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(spawnCenter, spawnRadius);
    }
}