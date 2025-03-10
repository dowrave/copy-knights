using System.Collections;
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
    private bool showPercentage;

    public Operator Operator { get; private set; }
    private StatisticsManager.OperatorStats opStats; 
    private StatisticsManager.StatType currentStatType;
    private Image fillImage; // 게이지 길이를 나타내는 이미지

    /// <summary>
    /// StatItem을 초기화하고 필요한 이벤트를 구독합니다.
    /// </summary>
    public void Initialize(Operator op, StatisticsManager.StatType statType, bool showPercentage)
    {
        Operator = op;
        currentStatType = statType;

        opStats = StatisticsManager.Instance.GetAllOperatorStats().Find(s => s.op == op);
        fillImage = percentageBar.fillRect.GetComponent<Image>();

        SetOperatorIcon(op);
        SetBarColor(statType);
        SetDisplayMode(showPercentage); // 절대 수치 / 백분율 전환

        // 1프레임 대기 후에 업데이트 실행(percentage 실제 반영 문제 때문에 넣음)
        StartCoroutine(DelayedUpdate(statType, showPercentage));
    }

    private void SetOperatorIcon(Operator op)
    {
        // 아이콘이 있다면 아이콘 할당
        if (op.BaseData.Icon != null)
        {
            operatorIcon.sprite = op.BaseData.Icon;
        }
        // 없다면 머티리얼 색을 가져옴
        else
        {
            operatorIcon.color = op.BaseData.prefab.GetComponentInChildren<Renderer>().sharedMaterial.color;
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
    
    private IEnumerator DelayedUpdate(StatisticsManager.StatType statType, bool showPercentage)
    {
        // UI 레이아웃이 업데이트될 때까지 대기
        yield return new WaitForEndOfFrame();

        // 실제 값 업데이트
        UpdateDisplay(statType, showPercentage);
    }

    /// <summary>
    /// 통계 업데이트 이벤트에 대응하여 디스플레이를 업데이트합니다.
    /// </summary>
    private void OnStatUpdated(Operator op, StatisticsManager.StatType statType)
    {
        if (this.Operator == op && statType == currentStatType)
        {
            UpdateDisplay(statType, showPercentage);
        }
    }

    /// <summary>
    /// StatItem의 디스플레이를 업데이트합니다.
    /// </summary>
    public void UpdateDisplay(StatisticsManager.StatType statType, bool showPercentage)
    {
        float value = StatisticsManager.Instance.GetOperatorValueForStatType(opStats, statType);
        float totalValue = StatisticsManager.Instance.GetTotalValueForStatType(statType);
        float percentage = (totalValue > 0) ? (value / totalValue) : 0; // 전체 값 중 해당 오퍼레이터가 기여한 값

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
        valueText.text = value.ToString("N0"); // 숫자 포맷팅
    }

    private void SetPercentage(float percentage)
    {
        percentageBar.value = percentage;
        percentageText.text = (percentage * 100).ToString("F1") + "%";
    }
}