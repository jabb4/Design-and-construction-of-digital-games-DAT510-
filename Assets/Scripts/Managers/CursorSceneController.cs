using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class CursorSceneController : MonoBehaviour
{
    [Tooltip("Cursor will be hidden and locked when these scenes load.")]
    [SerializeField]
    private List<string> hideCursorInScenes = new List<string>()
    {
        "Alleyway",
        "Citymap",
        "Street",
        "SampleScene"
    };

    [Tooltip("Cursor will be visible and unlocked when these scenes load.")]
    [SerializeField]
    private List<string> showCursorInScenes = new List<string>()
    {
        "Start Screen",
        "VanView"
    };

    private readonly HashSet<string> hiddenSceneLookup = new HashSet<string>();
    private readonly HashSet<string> shownSceneLookup = new HashSet<string>();

    private void Awake()
    {
        RebuildSceneLookup();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        ApplyForScene(SceneManager.GetActiveScene().name);
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void OnValidate()
    {
        RebuildSceneLookup();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyForScene(scene.name);
    }

    private void ApplyForScene(string sceneName)
    {
        if (hiddenSceneLookup.Contains(sceneName))
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        else if (shownSceneLookup.Contains(sceneName))
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        // Scene not in either list — leave cursor state unchanged.
    }

    private void RebuildSceneLookup()
    {
        hiddenSceneLookup.Clear();
        foreach (string sceneName in hideCursorInScenes)
        {
            if (!string.IsNullOrWhiteSpace(sceneName))
                hiddenSceneLookup.Add(sceneName);
        }

        shownSceneLookup.Clear();
        foreach (string sceneName in showCursorInScenes)
        {
            if (!string.IsNullOrWhiteSpace(sceneName))
                shownSceneLookup.Add(sceneName);
        }
    }
}
