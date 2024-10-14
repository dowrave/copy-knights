using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// StatisticsManager에 들어갈 각 항목
/// </summary>
public class StatisticItem : MonoBehaviour
{
    [SerializeField] private Image operatorIcon;
    [SerializeField] private TextMeshProUGUI valueText;
    [SerializeField] private Image percentageBar;
    [SerializeField] private TextMeshProUGUI percentageText;

    [SerializeField] private Color damageDealtColor;
    [SerializeField] private Color damageTakenColor;
    [SerializeField] private Color healingDoneColor;

    private float currentValue;
    private float currentPercentage;

    public void Initialize(Operator op, float value, float percentage, StatisticsManager.StatType statType)
    {
        Debug.Log($"{op} 스탯 아이템 초기화 시작");
        SetOperatorIcon(op);
        SetValue(value);
        SetPercentage(percentage);
        SetBarColor(statType);
        Debug.Log($"{op} 스탯 아이템 초기화 완료");

    }

    private void SetOperatorIcon(Operator op)
    {
        if (op.Icon != null)
        {
            operatorIcon.sprite = op.Icon;
        }
        else
        {
            operatorIcon.color = op.Data.prefab.GetComponentInChildren<Renderer>().sharedMaterial.color; 
        }
    }

    private void SetValue(float value)
    {
        currentValue = value;
        valueText.text = value.ToString("F0");
    }

    private void SetPercentage(float percentage)
    {
        currentPercentage = percentage;
        percentageBar.fillAmount = percentage / 100f;
        percentageText.text = percentage.ToString("F1") + "%";
    }

    private void SetBarColor(StatisticsManager.StatType statType)
    {
        switch (statType)
        {
            case StatisticsManager.StatType.DamageDealt:
                percentageBar.color = damageDealtColor;
                break; 
            case StatisticsManager.StatType.DamageTaken:
                percentageBar.color = damageTakenColor;
                break;
            case StatisticsManager.StatType.HealingDone:
                percentageBar.color = healingDoneColor;
                break;
        }
    }

    public void ToggleValueDisplay()
    {
        if (valueText.text.EndsWith("%"))
        {
            SetValue(currentValue);
        }
        else
        {
            SetPercentage(currentPercentage);
        }
    }
}