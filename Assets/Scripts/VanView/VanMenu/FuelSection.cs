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

    private int currentFuelAmount;
    private int tankSize;

    private void OnEnable()
    {
        GameStateManager.OnFuelChanged += UpdateCurrentFuel;
        GameStateManager.OnMaxFuelChanged += UpdateTankSize;

        currentFuelAmount = GameStateManager.Instance.GetFuelAmount();
        tankSize = GameStateManager.Instance.GetMaxFuelAmount();

        UpdateCurrentFuel(currentFuelAmount);
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
        currentFuelText.text = "Current Fuel: " + newCurrentFuelAmount + "/" + tankSize;
    }

    private void UpdateTankSize(int newTankSize)
    {
        currentFuelText.text = "Current Fuel: " + currentFuelAmount + "/" + newTankSize;
    }
}
