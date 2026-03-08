using UnityEngine;
using UnityEngine.Video;

[CreateAssetMenu(fileName = "TutorialSlide", menuName = "Data/Tutorial Slide")]
public class TutorialSlideData : ScriptableObject
{
    public string title;
    [TextArea(3, 10)] public string body;
    public VideoClip clip;
}
