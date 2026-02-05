using UnityEngine;

public class PlayerScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public float health, maxHealth, stamina, maxStamina;

    [SerializeField]
    private BarUi healthbar;

    [SerializeField]
    private BarUi staminabar;


    void Start()
    {
        healthbar.setMaxResource(maxHealth);
        healthbar.setCurrentResource(health);

        staminabar.setMaxResource(maxStamina);
        staminabar.setCurrentResource(stamina);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
