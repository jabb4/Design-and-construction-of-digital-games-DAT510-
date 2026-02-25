using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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


  private void OnEnable()
    {
        HealingSystemScript.OnBandagesChanged += SetItemAmount;

        var healingSystem = FindObjectOfType<HealingSystemScript>();
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


        if (keybind != null)
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
