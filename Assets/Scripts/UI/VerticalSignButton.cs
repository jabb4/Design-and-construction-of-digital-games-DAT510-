using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using TMPro;

public class VerticalSignButton : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, IPointerExitHandler
{
    [SerializeField] private float hoverScale = 1.05f;
    [SerializeField] private float transitionSpeed = 20f;
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip clickSound;
    private AudioSource audioSource;

    [Space]
    [SerializeField] private TMP_Text buttonText; // Target the text specifically
    [SerializeField] private Color hoverColor = Color.white; // Color to change to

    [Space]
    [SerializeField] private UnityEvent onClick;

    private Vector3 originalScale;
    private Vector3 targetScale;
    private Color originalColor;

    private bool isHovered = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Find the AudioSource on the parent Canvas (or any parent) so sound continues if button is disabled
        audioSource = GetComponentInParent<AudioSource>();

        // If not assigned manually, try to find it in children
        if (buttonText == null)
            buttonText = GetComponentInChildren<TMP_Text>();

        if (buttonText != null)
        {
            originalScale = buttonText.transform.localScale;
            targetScale = originalScale;
            originalColor = buttonText.color;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (buttonText == null) return;

        // Optimization: Only update if the scale is different
        if (buttonText.transform.localScale != targetScale)
        {
            buttonText.transform.localScale = Vector3.Lerp(buttonText.transform.localScale, targetScale, Time.deltaTime * transitionSpeed);

            // Snap to target if very close to avoid infinite tiny calculations
            if (Vector3.Distance(buttonText.transform.localScale, targetScale) < 0.001f)
            {
                buttonText.transform.localScale = targetScale;
            }
        }
    }

    void OnDisable()
    {
        // Reset scale immediately when disabled so it's ready for next time
        if (buttonText != null && originalScale != Vector3.zero)
        {
            buttonText.transform.localScale = originalScale;
            targetScale = originalScale;
            buttonText.color = originalColor;
        }

        // Only reset the cursor if we were the one holding the hover state
        if (isHovered && CustomCursor.Instance != null)
        {
            CustomCursor.Instance.SetDefaultCursor();
            isHovered = false;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (audioSource != null && clickSound != null)
        {
            audioSource.PlayOneShot(clickSound);
        }



        onClick?.Invoke();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        if (audioSource != null && hoverSound != null)
        {
            audioSource.PlayOneShot(hoverSound);
        }

        if (CustomCursor.Instance != null)
        {
            CustomCursor.Instance.SetHoverCursor();
        }

        targetScale = originalScale * hoverScale;

        if (buttonText != null)
            buttonText.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        if (CustomCursor.Instance != null)
        {
            CustomCursor.Instance.SetDefaultCursor();
        }

        targetScale = originalScale;

        if (buttonText != null)
            buttonText.color = originalColor;
    }
}
