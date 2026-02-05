using UnityEngine;

public class PlayerScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public float health, maxHealth;

    [SerializeField]
    private HealthBarUi healthbar;

    void Start()
    {
        healthbar.setMaxHealth(maxHealth);
        healthbar.setHealth(health);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
