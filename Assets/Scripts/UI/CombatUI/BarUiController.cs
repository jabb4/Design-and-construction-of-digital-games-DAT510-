using UnityEngine;
using Combat;

public class BarUiController : MonoBehaviour
{
    [SerializeField] private RectTransform bar;
    [SerializeField] private HealthComponent playerHealth; 
    [SerializeField] private float width = 140f;
    [SerializeField] private float height = 21f;

    private void Start() {
        //Called in start instead of awake to let HealthComponent initialize,
        //otherwise CurrentHealth == 1;
        //Could be done using Script Execution Order, but this could cause
        //conflicts within git. Might be worth changing later
        UpdateHealthDisplay(playerHealth.CurrentHealth, playerHealth.MaxHealth);
    }

    private void OnEnable()
    {    
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += UpdateHealthDisplay;
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= UpdateHealthDisplay;
        }
    }

    private void UpdateHealthDisplay(float currentHealth, float maxHealth)
    {
        float newWidth = width * (currentHealth / maxHealth);
        bar.sizeDelta = new Vector2(newWidth, height);
    }
}
