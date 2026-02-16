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
    }


    public void SetCurrency(int currency)
    {
        if (gameData == null)
        {
            gameData = new Data();
        }

        gameData.currency = currency;
    }


    public Data GetGameData()
    {
        //TODO Decide if we want to return the whole Data class, or if we want to split the things up in this class
        return gameData;
    }


    public void LoadGameData()
    {
        using (StreamReader reader = new StreamReader(Path.Combine(Application.persistentDataPath, "GameData.json")))
        {
            string dataToLoad = reader.ReadToEnd();

            gameData = JsonUtility.FromJson<Data>(dataToLoad);
        }
    }

    public Data LoadAndGetGameData()
    {
        LoadGameData();
        //TODO Decide if we want to return the whole Data class, or if we want to split the things up in this class
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
        //TODO A list of player upgrades. Unsure of exactly how the upgrades work at this time
        //     Maybe just the sword upgrade if no others are implemented.

        
        
        
        //TODO Van storage (if implemented)
        //TODO Van fuel amount (if implemented)
        //TODO List of Van upgrades
    }
}
