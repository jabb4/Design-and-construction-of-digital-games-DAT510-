using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class FuelSection : MonoBehaviour
{

    [SerializeField] private TextMeshProUGUI currentFuelText;
    [SerializeField] private TextMeshProUGUI reFuelText;
    [SerializeField] private TextMeshProUGUI upgradeTankText;
    [SerializeField] private int fuelCost = 30;
    [SerializeField] private int reFuelAmount = 10;
    [SerializeField] private int tankUpgradeSize = 10;
    [SerializeField] private int tankUpgradeCost = 100;


    private void OnEnable()
    {
        GameStateManager.OnFuelChanged += UpdateCurrentFuel;
        GameStateManager.OnMaxFuelChanged += UpdateTankSize;

        UpdateCurrentFuel(GameStateManager.Instance.GetFuelAmount());

        reFuelText.text = $"Buy {reFuelAmount}L of fuel for {fuelCost} coins";
        upgradeTankText.text = $"Upgrade you fuel tank to fit more gas! Increase capacity with {tankUpgradeSize}L for {tankUpgradeCost}$";

    }

    private void OnDisable()
    {
        GameStateManager.OnFuelChanged -= UpdateCurrentFuel;
        GameStateManager.OnMaxFuelChanged -= UpdateTankSize;
    }

    private void UpdateCurrentFuel(int newCurrentFuelAmount)
    {
        currentFuelText.text = "Current Fuel: " + newCurrentFuelAmount + "/" + GameStateManager.Instance.GetMaxFuelAmount();
    }

    private void UpdateTankSize(int newTankSize)
    {
        currentFuelText.text = "Current Fuel: " + GameStateManager.Instance.GetFuelAmount() + "/" + newTankSize;
    }

    public void ReFuel()
    {
        if (GameStateManager.Instance.GetFuelAmount() >= GameStateManager.Instance.GetMaxFuelAmount())
        {
            Debug.LogWarning("Your fuel tank is already full");
            return;
        }
        if (GameStateManager.Instance.GetCurrency() >= fuelCost) 
        {
            GameStateManager.Instance.AddCurrency(-fuelCost);
            GameStateManager.Instance.AddFuel(reFuelAmount);
            GameStateManager.Instance.SaveGameState();
        }
        else
        {
            Debug.LogWarning("Not enough currency to upgrade fuel");
        }
        
    }

    public void UpgradeFuelTank()
    {
        if (GameStateManager.Instance.GetCurrency() >= tankUpgradeCost) 
        {
            GameStateManager.Instance.AddCurrency(-tankUpgradeCost);
            GameStateManager.Instance.AddMaxFuelAmount(tankUpgradeSize);
            GameStateManager.Instance.SaveGameState();
        }
        else
        {
            Debug.LogWarning("Not enough currency to upgrade fuel");
        }
    }
}
