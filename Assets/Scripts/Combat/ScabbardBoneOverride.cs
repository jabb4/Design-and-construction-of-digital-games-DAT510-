using UnityEngine;

/// <summary>
/// Live-skeleton scabbard fix: snaps Weapon_l to Scabbard_Target01 every frame
/// after the Animator runs, overriding any animation that would move it elsewhere.
/// Ragdolls use a one-time hierarchy reparent instead (see EnemyDeathHandler /
/// PlayerDeathHandler) since they have no Animator driving bones.
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
