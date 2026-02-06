using UnityEngine;

public class Enemy : MonoBehaviour
{
    // Placeholder enemy script
    // Add enemy logic here later (AI, health, etc.)

    private void Start()
    {
        // Set layer to Enemy for camera lock-on detection (if not set already)
        gameObject.layer = LayerMask.NameToLayer("Enemy");
    }
}