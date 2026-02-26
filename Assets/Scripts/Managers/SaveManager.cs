using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private Data gameData;
    private const string SAVE_FILE_NAME = "GameData.json";
    private string SavePath => Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetCurrency(int currency)
    {
        gameData.currency = currency;
    }

    public void SetFuelAmount(int fuel)
    {
        gameData.fuelAmount = fuel;
    }

    public void SetMaxFuelAmount(int maxFuel)
    {
        gameData.maxFuelAmount = maxFuel;
    }

    public Data GetGameDataObject()
    {
        return gameData;
    }

    public int GetCurrency()
    {
        return gameData.currency;
    }

    public int GetFuelAmount()
    {
        return gameData.fuelAmount;
    }

    public int GetMaxFuelAmount()
    {
        return gameData.maxFuelAmount;
    }

    public bool gameDataSaveExists()
    {
        return File.Exists(SavePath);
    }

    public void LoadGameData()
    {
        if (!File.Exists(SavePath))
        {
            gameData = new Data();
            return;
        }

        try
        {
            string dataToLoad = File.ReadAllText(SavePath);
            gameData = JsonUtility.FromJson<Data>(dataToLoad);

            if (gameData == null) gameData = new Data();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load data: {e.Message}");
            gameData = new Data();
        }
    }

    public Data LoadAndGetGameDataObject()
    {
        LoadGameData();
        return gameData;
    }

    public void SaveGameData()
    {
        try
        {
            string dataToWrite = JsonUtility.ToJson(gameData, true);
            File.WriteAllText(SavePath, dataToWrite);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save data: {e.Message}");
        }
    }

    [System.Serializable]
    public class Data
    {
        public int currency;
        public int fuelAmount;
        public int maxFuelAmount;
    }
}
