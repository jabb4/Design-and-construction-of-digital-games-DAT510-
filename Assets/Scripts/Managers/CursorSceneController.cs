using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class CursorSceneController : MonoBehaviour
{
    [SerializeField] private List<string> hideCursorInScenes = new List<string>()
    {
        "Alleyway",
        "Citymap",
        "Street",
        "SampleScene"
    };

    private readonly HashSet<string> hiddenSceneLookup = new HashSet<string>();

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
        bool hideCursor = hiddenSceneLookup.Contains(sceneName);
        Cursor.visible = !hideCursor;
        Cursor.lockState = hideCursor ? CursorLockMode.Locked : CursorLockMode.None;
    }

    private void RebuildSceneLookup()
    {
        hiddenSceneLookup.Clear();
        for (int i = 0; i < hideCursorInScenes.Count; i++)
        {
            string sceneName = hideCursorInScenes[i];
            if (!string.IsNullOrWhiteSpace(sceneName))
            {
                hiddenSceneLookup.Add(sceneName);
            }
        }
    }
}
