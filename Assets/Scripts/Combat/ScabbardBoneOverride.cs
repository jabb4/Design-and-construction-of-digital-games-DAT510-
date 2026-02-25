using UnityEngine;

/// <summary>
/// Snaps a bone to a target transform every frame after the Animator runs.
/// Used to keep the scabbard (Weapon_l) pinned to Scabbard_Target01 at the hip,
/// overriding any animation that would move it elsewhere.
/// </summary>
public class ScabbardBoneOverride : MonoBehaviour
{
    [SerializeField] private string boneName = "Weapon_l";
    [SerializeField] private string targetName = "Scabbard_Target01";

    private Transform bone;
    private Transform target;

    private void Start()
    {
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
        {
            if (t.name == boneName) bone = t;
            else if (t.name == targetName) target = t;
        }

        if (bone == null || target == null)
        {
            enabled = false;
        }
    }

    private void LateUpdate()
    {
        bone.position = target.position;
        bone.rotation = target.rotation;
    }
}
