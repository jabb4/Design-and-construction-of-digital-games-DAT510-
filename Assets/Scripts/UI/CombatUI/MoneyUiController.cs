using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MoneyUiController : MonoBehaviour
{

    [SerializeField]
    private TextMeshProUGUI moneyText;

    public float moneyAmount;

    public void SetMoneyAmount(float MoneyAmount)
    {
        moneyAmount = MoneyAmount;

        moneyText.text = (moneyAmount.ToString() + " $");
    }
    

}
