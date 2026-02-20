using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemUiController : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI visibleAmount;

    [SerializeField]
    private TextMeshProUGUI visibleKeybind;

    private float itemAmount;
    private string keybind;

    public void SetKeybind(string Keybind)
    {
        keybind = Keybind;


        if (keybind != null)
            visibleKeybind.text = keybind;
        else
            visibleKeybind.text = "missing keybind error";

    }

    public void SetItemAmount(float ItemAmount)
    {
        itemAmount = ItemAmount;

        if (visibleAmount != null)
            visibleAmount.text = itemAmount.ToString();
        else
            visibleAmount.text = "missing amount error";
    }

}
