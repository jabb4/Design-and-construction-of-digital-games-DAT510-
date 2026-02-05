using UnityEngine;

public class HealthBarUi : MonoBehaviour
{
    public float MaxHealth, Width, Height, Health;

    [SerializeField]
    private RectTransform healthbar;

    public void setMaxHealth(float maxHealth)
    {
        MaxHealth = maxHealth;
    }

    public void setHealth(float health)
    {
        Health = health;

        float newWidth = Width * (Health/MaxHealth);

        healthbar.sizeDelta = new Vector2(newWidth, Height);
    }
}
