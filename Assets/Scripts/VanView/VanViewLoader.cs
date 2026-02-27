using UnityEngine;

public class VanViewLoader : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameStateManager.Instance.LoadGameState();
    }
}
