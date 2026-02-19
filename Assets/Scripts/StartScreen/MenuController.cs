using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    public void NewGame()
    {
        // Needs to impement some logic to create new save files
        SceneManager.LoadSceneAsync(2);
    }

    public void LoadGame()
    {
        // Make sure we load the saved values
        Debug.Log("Loading new game");
        SceneManager.LoadSceneAsync(2);
    }

    public void Exit()
    {
        Application.Quit();
    }
}
