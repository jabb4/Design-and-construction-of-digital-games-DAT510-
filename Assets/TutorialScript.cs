using System.Collections.Generic;
using UnityEngine;

public class TutorialScript : MonoBehaviour
{
    [SerializeField]
    public List<GameObject> slides;

    [SerializeField]
    public List<GameObject> buttons;

    [SerializeField] private int slide;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        slide = 0;
        CloseSlides();
    }
    public void CloseSlides()
    {
        foreach(GameObject gameObject in slides)
        {
            gameObject.SetActive(false);
        }
        foreach(GameObject gameObject in buttons)
        {
            gameObject.SetActive(false);
        }
    }
    public void OpenSlide()
    {
        foreach(GameObject gameObject in buttons)
        {
            gameObject.SetActive(true);
        }
        slide = 0;
        slides[slide].SetActive(true);
    }
    public void NextSlide()
    {
        slides[slide].SetActive(false);
        if(slide < slides.Count -1)
        {
            slide++;
            slides[slide].SetActive(true);
        }
        else
        {
            CloseSlides();
        }
    }
}
