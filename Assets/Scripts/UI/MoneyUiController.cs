using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MoneyUiController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI moneyText;

    private void OnEnable()
    {
        GameStateManager.OnCurrencyChanged += UpdateCurrencyDisplay;

        if (GameStateManager.Instance != null) {
            UpdateCurrencyDisplay(GameStateManager.Instance.GetCurrency(), 0);
        }
    }

    private void OnDisable()
    {
        GameStateManager.OnCurrencyChanged -= UpdateCurrencyDisplay;
    }

    public void UpdateCurrencyDisplay(int newAmount, int delta)
    {
        moneyText.text = (newAmount.ToString());
    }
    

}
