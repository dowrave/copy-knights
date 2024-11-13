using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatisticItem : MonoBehaviour
{
    [SerializeField] private Image operatorIcon;
    [SerializeField] private TextMeshProUGUI valueText;
    [SerializeField] private Slider percentageBar; // ���� ������Ʈ�� Fill �� ����
    [SerializeField] private TextMeshProUGUI percentageText;
    [SerializeField] private Color damageDealtColor;
    [SerializeField] private Color damageTakenColor;
    [SerializeField] private Color healingDoneColor;
    private bool showPercentage;

    public Operator Operator { get; private set; }
    private StatisticsManager.OperatorStats opStats; 
    private StatisticsManager.StatType currentStatType;
    private Image fillImage; // ������ ���̸� ��Ÿ���� �̹���

    /// <summary>
    /// StatItem�� �ʱ�ȭ�ϰ� �ʿ��� �̺�Ʈ�� �����մϴ�.
    /// </summary>
    public void Initialize(Operator op, StatisticsManager.StatType statType, bool showPercentage)
    {
        Operator = op;
        currentStatType = statType;

        opStats = StatisticsManager.Instance.GetAllOperatorStats().Find(s => s.op == op);
        fillImage = percentageBar.fillRect.GetComponent<Image>();

        SetOperatorIcon(op);
        SetBarColor(statType);
        SetDisplayMode(showPercentage); // ���� ��ġ / ����� 
        UpdateDisplay(statType, showPercentage);
    }

    private void SetOperatorIcon(Operator op)
    {
        // �������� �ִٸ� ������ �Ҵ�
        if (op.Data.Icon != null)
        {
            operatorIcon.sprite = op.Data.Icon;
        }
        // ���ٸ� ��Ƽ���� ���� ������
        else
        {
            operatorIcon.color = op.Data.prefab.GetComponentInChildren<Renderer>().sharedMaterial.color;
        }
    }

    private void SetDisplayMode(bool showPercentage)
    {
        valueText.gameObject.SetActive(!showPercentage);
        percentageText.gameObject.SetActive(showPercentage);
    }

    private void SetBarColor(StatisticsManager.StatType statType)
    {
        if (fillImage != null)
        {
            switch (statType)
            {
                case StatisticsManager.StatType.DamageDealt:
                    fillImage.color = damageDealtColor;
                    break;
                case StatisticsManager.StatType.DamageTaken:
                    fillImage.color = damageTakenColor;
                    break;
                case StatisticsManager.StatType.HealingDone:
                    fillImage.color = healingDoneColor;
                    break;
            }
        }
    }

    /// <summary>
    /// ��� ������Ʈ �̺�Ʈ�� �����Ͽ� ���÷��̸� ������Ʈ�մϴ�.
    /// </summary>
    private void OnStatUpdated(Operator op, StatisticsManager.StatType statType)
    {
        if (this.Operator == op && statType == currentStatType)
        {
            UpdateDisplay(statType, showPercentage);
        }
    }

    /// <summary>
    /// StatItem�� ���÷��̸� ������Ʈ�մϴ�.
    /// </summary>
    public void UpdateDisplay(StatisticsManager.StatType statType, bool showPercentage)
    {
        float value = StatisticsManager.Instance.GetOperatorValueForStatType(opStats, statType);
        float totalValue = StatisticsManager.Instance.GetTotalValueForStatType(statType);
        float percentage = (totalValue > 0) ? (value / totalValue) : 0; // ��ü �� �� �ش� ���۷����Ͱ� �⿩�� ��

        fillImage.fillAmount = percentage; 

        if (showPercentage)
        {
            SetPercentage(percentage);
        }
        else
        {
            SetValue(value);
        }

        SetDisplayMode(showPercentage);
        SetBarColor(statType);
    }

    private void SetValue(float value)
    {
        valueText.text = value.ToString("N0"); // ���� ������
    }

    private void SetPercentage(float percentage)
    {
        percentageBar.value = percentage;
        percentageText.text = (percentage * 100).ToString("F1") + "%";
    }
}