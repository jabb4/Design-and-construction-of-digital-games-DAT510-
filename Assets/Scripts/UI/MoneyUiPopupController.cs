using System.Collections;
using UnityEngine;
using TMPro;

public class MoneyUiPopupController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI popupText;
    [SerializeField] private float displayDuration = 1f;
    [SerializeField] private float floatDistance = 0f;   // pixels to float upward
    [SerializeField] private float fadeDelay = 0.4f;      // how long before fading begins

    private Coroutine _animCoroutine;
    private RectTransform _rectTransform;
    private Vector2 _originPosition;

    private void Awake()
    {
        _rectTransform = popupText.GetComponent<RectTransform>();
        _originPosition = _rectTransform.anchoredPosition;
    }

    private void OnEnable()
    {
        GameStateManager.OnCurrencyChanged += ShowPopup;
        popupText.enabled = false;
    }

    private void OnDisable()
    {
        GameStateManager.OnCurrencyChanged -= ShowPopup;
    }

    private void ShowPopup(int newAmount, int delta)
    {
        if (delta == 0) return;

        popupText.text = delta > 0 ? "+" + delta : delta.ToString();
        popupText.enabled = true;

        if (_animCoroutine != null)
            StopCoroutine(_animCoroutine);
        _animCoroutine = StartCoroutine(AnimatePopup());
    }

    private IEnumerator AnimatePopup()
    {
        float elapsed = 0f;

        while (elapsed < displayDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / displayDuration;

            // Float upward
            _rectTransform.anchoredPosition = _originPosition + Vector2.up * (floatDistance * t);

            // Fade only after fadeDelay
            float fadeT = Mathf.InverseLerp(fadeDelay, displayDuration, elapsed);
            Color c = popupText.color;
            c.a = 1f - fadeT;
            popupText.color = c;

            yield return null;
        }

        // Reset
        popupText.enabled = false;
        _rectTransform.anchoredPosition = _originPosition;
        Color reset = popupText.color;
        reset.a = 1f;
        popupText.color = reset;
        _animCoroutine = null;
    }
}