using UnityEngine;

public class ExManagerScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public float health, maxHealth, stamina, maxStamina, itemAmount, moneyAmount;
    public string healKeybind;

    [SerializeField]
    private BarUiController healthbar;

    [SerializeField]
    private BarUiController staminabar;

    [SerializeField]
    private ItemUiController healItem;

    [SerializeField]
    private MoneyUiController moneyText;

    // Update is called once per frame
    void Update()
    {
        healthbar.SetMaxResource(maxHealth);
        healthbar.SetCurrentResource(health);

        staminabar.SetMaxResource(maxStamina);
        staminabar.SetCurrentResource(stamina);

        healItem.SetKeybind(healKeybind);
        healItem.SetItemAmount(itemAmount);

        moneyText.SetMoneyAmount(moneyAmount);  
    }
}
