using UnityEngine;
using UnityEngine.UI;
using Combat;

public class BarUiController : MonoBehaviour
{
    [SerializeField] private Image bar;
    [SerializeField] private HealthComponent health;

    private Material _barMaterial;

    private void Awake()
    {
        // Create an instance so we don't modify the shared material
        if (bar != null) {
            _barMaterial = new Material(bar.material);
            bar.material = _barMaterial;
        }
    }

    private void Start() {
        //Called in start instead of awake to let HealthComponent initialize,
        //otherwise CurrentHealth == 1;
        //Could be done using Script Execution Order, but this could cause
        //conflicts within git. Might be worth changing later
        if(health != null)
            UpdateHealthDisplay(health.CurrentHealth, health.MaxHealth);
        else
            Debug.LogWarning("Not receiving Health!");
    }

    private void OnEnable()
    {    
        if (health != null)
        {
            health.OnHealthChanged += UpdateHealthDisplay;
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.OnHealthChanged -= UpdateHealthDisplay;
        }
    }

    private void OnDestroy()
    {
        if (_barMaterial != null)
            Destroy(_barMaterial);
    }

    private void UpdateHealthDisplay(float currentHealth, float maxHealth)
    {
        if (maxHealth != 0 && _barMaterial != null)
            _barMaterial.SetFloat("_FillAmount", currentHealth / maxHealth);
        else
            Debug.LogWarning("Either _barMaterial is null, or maxHealth is 0");
    }
}
