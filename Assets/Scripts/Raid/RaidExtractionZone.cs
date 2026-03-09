using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class RaidExtractionZone : MonoBehaviour
{
    [SerializeField] private float holdTime = 10f;
    [SerializeField] private int targetSceneIndex = 1;
    [SerializeField] private RaidExtractionUI extractionUI;
    [SerializeField] private AudioClip extractionSound;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private GameObject backMenuUI;


    private bool isPlayerInZone = false;
    private bool isExtracting = false;
    private float currentHoldTime = 0f;

    // Update is called once per frame
    void Update()
    {
        if (isExtracting) return;
        if (backMenuUI != null && backMenuUI.activeSelf) return;

        if (isPlayerInZone)
        {
            if (InputSystem.actions.FindAction("Player/Interact").IsPressed())
            {
                currentHoldTime += Time.deltaTime;

                float remainingTime = Mathf.Max(0, holdTime - currentHoldTime);
                if (extractionUI != null) extractionUI.UpdateCountdown(remainingTime);

                if (currentHoldTime >= holdTime)
                {
                    if (audioSource != null && extractionSound != null)
                    {
                        audioSource.PlayOneShot(extractionSound);
                    }
                    StartCoroutine(DisplayFadeAndExitRaid());
                }
            }
            else
            {
                currentHoldTime = 0f;
                if (extractionUI != null) extractionUI.ResetText();
            }
        }
        else
        {
            currentHoldTime = 0f;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInZone = true;
            if (extractionUI != null) extractionUI.Show();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInZone = false;
            currentHoldTime = 0f;
            if (extractionUI != null) extractionUI.Hide();
        }
    }

    private IEnumerator DisplayFadeAndExitRaid()
    {
        isExtracting = true;

        if (extractionUI != null)
        {
            yield return extractionUI.FadeToBlack();
        }

        GameStateManager.Instance.SaveGameState();
        SceneManager.LoadScene(targetSceneIndex);
    }
}
