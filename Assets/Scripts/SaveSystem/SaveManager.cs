using Player.StateMachine.States;
using System.IO;
using TMPro.Examples;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    private Data gameData;

    public SaveManager ()
    {
        gameData = new Data();
        LoadGameData();
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


    public void LoadGameData()
    {
        using (StreamReader reader = new StreamReader(Path.Combine(Application.persistentDataPath, "GameData.json")))
        {
            string dataToLoad = reader.ReadToEnd();

            gameData = JsonUtility.FromJson<Data>(dataToLoad);
        }
    }

    public Data LoadAndGetGameDataObject()
    {
        LoadGameData();
        return gameData;
    }


    public void SaveGameData()
    {
        using (StreamWriter writer = new StreamWriter(Path.Combine(Application.persistentDataPath, "GameData.json")))
        {
            string dataToWrite = JsonUtility.ToJson(gameData);

            writer.Write(dataToWrite);
        }
    }


    //Data class used to decide what data to store. This will be converted to and from JSON
    [System.Serializable]
    public class Data
    {
        public int currency;
        public int fuelAmount;
        public int maxFuelAmount;
    }
}
