using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class RaidExtractionZone : MonoBehaviour
{
    [SerializeField] private float holdTime = 10f;
    [SerializeField] private int targetSceneIndex = 2;
    [SerializeField] private Key activationKey = Key.E;
    [SerializeField] private RaidExtractionUI extractionUI;
    [SerializeField] private AudioClip extractionSound;
    [SerializeField] private AudioSource audioSource;


    private bool isPlayerInZone = false;
    private bool isExtracting = false;
    private float currentHoldTime = 0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (isExtracting) return;

        if (isPlayerInZone)
        {
            if (Keyboard.current != null && Keyboard.current[activationKey].isPressed)
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

        // Save data before exiting raid?? With GameStateManager?
        SceneManager.LoadScene(targetSceneIndex);
    }
}
