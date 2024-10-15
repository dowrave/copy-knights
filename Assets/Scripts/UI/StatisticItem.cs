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

    public Operator op { get; private set; }
    private StatisticsManager.StatType currentStatType;

    /// <summary>
    /// StatItem�� �ʱ�ȭ�ϰ� �ʿ��� �̺�Ʈ�� �����մϴ�.
    /// </summary>
    public void Initialize(Operator op, StatisticsManager.StatType statType)
    {
        this.op = op;
        currentStatType = statType;
        SetOperatorIcon(op);
        SetBarColor(statType);
        UpdateDisplay();
        StatisticsManager.OnStatUpdated += OnStatUpdated;
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

    private void SetBarColor(StatisticsManager.StatType statType)
    {
        Image fillImage = percentageBar.fillRect.GetComponent<Image>();
        
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
        if (this.op == op && statType == currentStatType)
        {
            UpdateDisplay();
        }
    }

    /// <summary>
    /// StatItem�� ���÷��̸� �ֽ� ������ ������Ʈ�մϴ�.
    /// </summary>
    public void UpdateDisplay()
    {
        float value = StatisticsManager.Instance.GetOperatorValueForStatType(this.op, currentStatType);
        float totalValue = StatisticsManager.Instance.GetTotalValueForStatType(currentStatType);
        float percentage = (totalValue > 0) ? (value / totalValue) : 0; // ��ü �� �� �ش� ���۷����Ͱ� �⿩�� ��

        SetValue(value);

        SetPercentage(percentage);
    }

    private void SetValue(float value)
    {
        valueText.text = value.ToString("F0");
    }

    private void SetPercentage(float percentage)
    {
        percentageBar.value = percentage;
        percentageText.text = percentage.ToString("F1") + "%";
    }

    private void OnDestroy()
    {
        StatisticsManager.OnStatUpdated -= OnStatUpdated;
    }
}