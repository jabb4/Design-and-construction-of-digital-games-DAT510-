using UnityEngine;
using System;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    private int currency;
    private int fuelAmount;
    private int maxFuelAmount;

    public static event Action<int> OnCurrencyChanged;
    public static event Action<int> OnFuelChanged;
    public static event Action<int> OnMaxFuelChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        LoadGameState();
    }

    public int GetCurrency()
    {
        return currency;
    }

    public void SetCurrency(int value)
    {
        currency = value;
        OnCurrencyChanged?.Invoke(currency);
    }

    public void AddCurrency(int amount)
    {
        currency += amount;
        OnCurrencyChanged?.Invoke(currency);
    }

    public void RemoveCurrency(int amount)
    {
        currency -= amount;
        OnCurrencyChanged?.Invoke(currency);
    }

    public int GetFuelAmount()
    {
        return fuelAmount;
    }

    public void SetFuelAmount(int value)
    {
        fuelAmount = Mathf.Clamp(value, 0, maxFuelAmount);
        OnFuelChanged?.Invoke(fuelAmount);
    }

    public void AddFuel(int amount)
    {
        fuelAmount = Mathf.Clamp(fuelAmount + amount, 0, maxFuelAmount);
        OnFuelChanged?.Invoke(fuelAmount);
    }

    public void RemoveFuel(int amount)
    {
        fuelAmount = Mathf.Max(0, fuelAmount - amount);
        OnFuelChanged?.Invoke(fuelAmount);
    }

    public int GetMaxFuelAmount()
    {
        return maxFuelAmount;
    }

    public void SetMaxFuelAmount(int value)
    {
        maxFuelAmount = value;
        fuelAmount = Mathf.Min(fuelAmount, maxFuelAmount);
        OnMaxFuelChanged?.Invoke(maxFuelAmount);
    }

    public void AddMaxFuelAmount(int value)
    {
        maxFuelAmount += value;
        fuelAmount = Mathf.Min(fuelAmount, maxFuelAmount);
        OnMaxFuelChanged?.Invoke(maxFuelAmount);
    }

    public void RemoveMaxFuelAmount(int value)
    {
        maxFuelAmount -= value;
        fuelAmount = Mathf.Min(fuelAmount, maxFuelAmount);
        OnMaxFuelChanged?.Invoke(maxFuelAmount);
    }

    public void SaveGameState()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SetCurrency(currency);
            SaveManager.Instance.SetFuelAmount(fuelAmount);
            SaveManager.Instance.SetMaxFuelAmount(maxFuelAmount);
            SaveManager.Instance.SaveGameData();
        }
    }

    public void LoadGameState()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.LoadGameData();
            currency = SaveManager.Instance.GetCurrency();
            fuelAmount = SaveManager.Instance.GetFuelAmount();
            maxFuelAmount = SaveManager.Instance.GetMaxFuelAmount();

            OnCurrencyChanged?.Invoke(currency);
        }
    }
}
