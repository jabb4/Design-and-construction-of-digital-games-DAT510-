using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class RaidExtractionUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private Image fadeOverlay;
    [SerializeField] private float fadeDuration = 1.6f;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void Start()
    {
        Hide();
        if (fadeOverlay != null)
        {
            fadeOverlay.color = new Color(0, 0, 0, 0);
            fadeOverlay.gameObject.SetActive(false);
        }
    }

    public void Show()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }
        else
        {
            gameObject.SetActive(true);
        }
        ResetText();
    }

    public void Hide()
    {
        // Don't hide if we are fading out
        if (fadeOverlay != null && fadeOverlay.gameObject.activeSelf && fadeOverlay.color.a > 0) return;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    public void UpdateCountdown(float remainingTime)
    {
        countdownText.text = $"Extracting in: {remainingTime:F1}s";
    }

    public void ResetText()
    {
        countdownText.text = "Hold 'E' to Extract";
    }

    public IEnumerator FadeToBlack()
    {
        if (fadeOverlay != null)
        {
            fadeOverlay.gameObject.SetActive(true);
            float timer = 0f;
            Color startColor = fadeOverlay.color;
            Color endColor = new Color(0, 0, 0, 1);

            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                fadeOverlay.color = Color.Lerp(startColor, endColor, timer / fadeDuration);
                yield return null;
            }
            fadeOverlay.color = endColor;
        }
        else
        {
            // Fallback if no overlay assigned
            yield return new WaitForSeconds(fadeDuration);
        }
    }
}
