using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatisticItem : MonoBehaviour
{
    [SerializeField] private Image operatorIcon;
    [SerializeField] private TextMeshProUGUI valueText;
    [SerializeField] private Slider percentageBar; // 하위 오브젝트에 Fill 이 있음
    [SerializeField] private TextMeshProUGUI percentageText;
    [SerializeField] private Color damageDealtColor;
    [SerializeField] private Color damageTakenColor;
    [SerializeField] private Color healingDoneColor;

    public Operator op { get; private set; }
    private StatisticsManager.StatType currentStatType;

    /// <summary>
    /// StatItem을 초기화하고 필요한 이벤트를 구독합니다.
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
        // 아이콘이 있다면 아이콘 할당
        if (op.Data.Icon != null)
        {
            operatorIcon.sprite = op.Data.Icon;
        }
        // 없다면 머티리얼 색을 가져옴
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
    /// 통계 업데이트 이벤트에 대응하여 디스플레이를 업데이트합니다.
    /// </summary>
    private void OnStatUpdated(Operator op, StatisticsManager.StatType statType)
    {
        if (this.op == op && statType == currentStatType)
        {
            UpdateDisplay();
        }
    }

    /// <summary>
    /// StatItem의 디스플레이를 최신 값으로 업데이트합니다.
    /// </summary>
    public void UpdateDisplay()
    {
        float value = StatisticsManager.Instance.GetOperatorValueForStatType(this.op, currentStatType);
        float totalValue = StatisticsManager.Instance.GetTotalValueForStatType(currentStatType);
        float percentage = (totalValue > 0) ? (value / totalValue) : 0; // 전체 값 중 해당 오퍼레이터가 기여한 값

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