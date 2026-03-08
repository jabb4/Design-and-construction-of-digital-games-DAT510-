using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using Combat;

public class ItemUiController : MonoBehaviour
{
    [SerializeField] private HealingSystemScript healingSystem; 
    [SerializeField] private TextMeshProUGUI visibleAmount;

    [SerializeField] private TextMeshProUGUI visibleKeybind;
    [SerializeField] private Image greyout;
    [SerializeField] private TextMeshProUGUI cooldownText;

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
        HealingSystemScript.OnBandagesUsed += TriggerItemCooldown;
        
        if (healingSystem != null)
        {
            SetItemAmount(healingSystem.AmountBandages);
        }

        if (cooldownText != null)
            cooldownText.gameObject.SetActive(false);

        if (greyout != null)
            greyout.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        HealingSystemScript.OnBandagesChanged -= SetItemAmount;
        HealingSystemScript.OnBandagesUsed -= TriggerItemCooldown;

    }

    public void SetKeybind(string keybind)
    {
        if (visibleKeybind != null)
            visibleKeybind.text = keybind;
        else
            Debug.LogWarning("missing keybind error");

    }

    public void SetItemAmount(int itemAmount)
    {
        if (visibleAmount != null)
            visibleAmount.text = itemAmount.ToString();
        else
            Debug.LogWarning("missing amount error");
    }

    public void TriggerItemCooldown(float cooldown) 
    {
        StopAllCoroutines();
        StartCoroutine(CooldownCoroutine(cooldown));
    }

    private IEnumerator CooldownCoroutine(float cooldown)
    {
        if (greyout != null)
            greyout.gameObject.SetActive(true);
        if (cooldownText != null)
            cooldownText.gameObject.SetActive(true);

        float remaining = cooldown;
        while (remaining > 0f)
        {
            if (cooldownText != null)
                cooldownText.text = Mathf.CeilToInt(remaining).ToString();
            yield return null;
            remaining -= Time.deltaTime;
        }

        if (greyout != null)
            greyout.gameObject.SetActive(false);
        if (cooldownText != null)
        {
            cooldownText.gameObject.SetActive(false);
            cooldownText.text = "";
        }
    }
}
