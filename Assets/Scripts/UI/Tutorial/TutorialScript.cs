using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;

public class TutorialScript : MonoBehaviour
{
    [Header("Slide Data")]
    [SerializeField] private TutorialSlideData[] slides;

    [Header("UI References")]
    [SerializeField] private GameObject slidePanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private TextMeshProUGUI pageIndicator;
    [SerializeField] private List<GameObject> buttons;

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;

    public bool IsOpen { get; private set; }
    private int currentSlide;

    private InputAction navigateAction;
    private InputAction submitAction;
    private InputAction cancelAction;
    private bool navigatedThisFrame;

    void Awake()
    {
        var uiMap = inputActions.FindActionMap("UI");
        navigateAction = uiMap.FindAction("Navigate");
        submitAction = uiMap.FindAction("Submit");
        cancelAction = uiMap.FindAction("Cancel");

        slidePanel.SetActive(false);
        foreach (GameObject go in buttons)
            go.SetActive(false);
    }

    void Update()
    {
        if (!IsOpen) return;

        if (cancelAction.WasPressedThisFrame())
        {
            CloseSlides();
            return;
        }

        if (submitAction.WasPressedThisFrame())
        {
            var selected = EventSystem.current != null
                ? EventSystem.current.currentSelectedGameObject
                : null;
            if (selected == null || !buttons.Contains(selected))
            {
                NextSlide();
                return;
            }
        }

        var nav = navigateAction.ReadValue<Vector2>();
        if (!navigatedThisFrame && nav.x > 0.5f)
        {
            navigatedThisFrame = true;
            NextSlide();
        }
        else if (!navigatedThisFrame && nav.x < -0.5f)
        {
            navigatedThisFrame = true;
            PreviousSlide();
        }
        else if (Mathf.Abs(nav.x) < 0.3f)
        {
            navigatedThisFrame = false;
        }
    }

    public bool ShouldShow()
    {
        return SaveManager.Instance != null
            && !SaveManager.Instance.GetHasSeenTutorial();
    }

    public void OpenSlide()
    {
        currentSlide = 0;
        IsOpen = true;

        transform.SetAsLastSibling();

        slidePanel.SetActive(true);
        foreach (GameObject go in buttons)
            go.SetActive(true);

        DisplaySlide();

        navigateAction.Enable();
        submitAction.Enable();
        cancelAction.Enable();
    }

    public void CloseSlides()
    {
        IsOpen = false;

        slidePanel.SetActive(false);
        foreach (GameObject go in buttons)
            go.SetActive(false);

        if (navigateAction != null) navigateAction.Disable();
        if (submitAction != null) submitAction.Disable();
        if (cancelAction != null) cancelAction.Disable();

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SetHasSeenTutorial(true);
            SaveManager.Instance.SaveGameData();
        }
    }

    public void NextSlide()
    {
        if (currentSlide < slides.Length - 1)
        {
            currentSlide++;
            DisplaySlide();
        }
        else
        {
            CloseSlides();
        }
    }

    public void PreviousSlide()
    {
        if (currentSlide > 0)
        {
            currentSlide--;
            DisplaySlide();
        }
    }

    private void DisplaySlide()
    {
        var data = slides[currentSlide];
        titleText.text = data.title;
        bodyText.text = data.body;

        if (pageIndicator != null)
            pageIndicator.text = $"{currentSlide + 1} / {slides.Length}";
    }
}
