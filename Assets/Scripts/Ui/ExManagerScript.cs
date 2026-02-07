using UnityEngine;

public class ExManagerScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public float health, maxHealth, stamina, maxStamina, itemAmount;
    public string healKeybind;

    [SerializeField]
    private BarUiController healthbar;

    [SerializeField]
    private BarUiController staminabar;

    [SerializeField]
    private ItemUiController healItem;

    // Update is called once per frame
    void Update()
    {
        healthbar.setMaxResource(maxHealth);
        healthbar.setCurrentResource(health);

        staminabar.setMaxResource(maxStamina);
        staminabar.setCurrentResource(stamina);

        healItem.setKeybind(healKeybind);
        healItem.setItemAmount(itemAmount);
    }
}
