using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class BackMenuUI : MonoBehaviour
{
    public GameObject warningText;
    public bool isInGame = false;
    private float lastClickTime = -1f;
    private const float DOUBLE_CLICK_TIME = 0.5f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Resume();
    }

    void OnEnable()
    {
        if (isInGame) warningText.gameObject.SetActive(true);
        else warningText.gameObject.SetActive(false);
    }

    public void Resume()
    {
        gameObject.SetActive(false);
    }

    public void Exit()
    {
        if (isInGame)
        {
            // If the time since the last click is greater than the threshold, 
            // treat this as the first click of a double-click.
            if (Time.unscaledTime - lastClickTime > DOUBLE_CLICK_TIME)
            {
                lastClickTime = Time.unscaledTime;
                return;
            }
        }
        SceneManager.LoadSceneAsync(1);
    }


}
