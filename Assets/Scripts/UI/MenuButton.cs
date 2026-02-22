using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class MenuButton : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, IPointerExitHandler
{
    [SerializeField] private float hoverScale = 1.05f;
    [SerializeField] private float transitionSpeed = 20f;
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip clickSound;
    private AudioSource audioSource;

    [Space]
    [SerializeField] private UnityEvent onClick;

    private Vector3 originalScale;
    private Vector3 targetScale;

    private bool isHovered = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
        audioSource = GetComponentInParent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        // Optimization: Only update if the scale is different
        if (transform.localScale != targetScale)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * transitionSpeed);

            // Snap to target if very close to avoid infinite tiny calculations
            if (Vector3.Distance(transform.localScale, targetScale) < 0.001f)
            {
                transform.localScale = targetScale;
            }
        }
    }

    void OnDisable()
    {
        // Reset scale immediately when disabled so it's ready for next time
        if (originalScale != Vector3.zero)
        {
            transform.localScale = originalScale;
            targetScale = originalScale;
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
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        if (CustomCursor.Instance != null)
        {
            CustomCursor.Instance.SetDefaultCursor();
        }

        targetScale = originalScale;
    }
}
