using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class MenuButton : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, IPointerExitHandler
{
    [SerializeField] private float hoverScale = 1.05f;
    [SerializeField] private float transitionSpeed = 20f;

    [Space]
    [SerializeField] private UnityEvent onClick;

    private Vector3 originalScale;
    private Vector3 targetScale;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
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

    public void OnPointerClick(PointerEventData eventData)
    {
        onClick?.Invoke();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = originalScale * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = originalScale;
    }
}
