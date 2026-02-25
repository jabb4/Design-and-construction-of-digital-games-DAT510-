using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using Combat;

public class ItemUiController : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI visibleAmount;

    [SerializeField]
    private TextMeshProUGUI visibleKeybind;

    private int itemAmount;
    private string keybind;

    private void Start() 
    {
        var healAction = InputSystem.actions.FindAction("Player/Heal");
        if (healAction != null)
        {
            SetKeybind(healAction.GetBindingDisplayString(0));
        }
        else
        {
        SetKeybind("?");  
        Debug.LogWarning("Heal action not found in Input System!");
        }
    }

    private void OnEnable()
    {
        HealingSystemScript.OnBandagesChanged += SetItemAmount;

        //Could also use a direct variable for HealingSystemScript,
        // but should only ever exist one at a time, reduces manual coupling in editor
        var healingSystem = FindFirstObjectByType<HealingSystemScript>();
        if (healingSystem != null)
        {
            SetItemAmount(healingSystem.AmountBandages);
        }
    }

    private void OnDisable()
    {
        HealingSystemScript.OnBandagesChanged -= SetItemAmount;
    }

    public void SetKeybind(string Keybind)
    {
        keybind = Keybind;


        if (visibleKeybind != null)
            visibleKeybind.text = keybind;
        else
            visibleKeybind.text = "missing keybind error";

    }

    public void SetItemAmount(int ItemAmount)
    {
        itemAmount = ItemAmount;

        if (visibleAmount != null)
            visibleAmount.text = itemAmount.ToString();
        else
            visibleAmount.text = "missing amount error";
    }

}
