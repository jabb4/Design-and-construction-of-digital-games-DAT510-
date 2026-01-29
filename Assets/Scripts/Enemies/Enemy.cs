using UnityEngine;

public class Enemy : MonoBehaviour
{
    // Placeholder enemy script
    // Add enemy logic here later (AI, health, etc.)

    private void Start()
    {
        // Ensure the GameObject has a capsule collider if not already
        if (GetComponent<CapsuleCollider>() == null)
        {
            gameObject.AddComponent<CapsuleCollider>();
        }

        // Set layer to Enemy for camera lock-on detection
        gameObject.layer = LayerMask.NameToLayer("Enemy");

        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.red;
        }
    }
}