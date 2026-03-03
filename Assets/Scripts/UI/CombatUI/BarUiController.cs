using UnityEngine;
using UnityEngine.UI;
using Combat;

public class BarUiController : MonoBehaviour
{
    [SerializeField] private Image bar;
    [SerializeField] private HealthComponent playerHealth;

    private Material _barMaterial;

    private void Awake()
    {
        // Create an instance so we don't modify the shared material
        _barMaterial = new Material(bar.material);
        bar.material = _barMaterial;
    }

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
        _barMaterial.SetFloat("_FillAmount", currentHealth / maxHealth);
    }
}
