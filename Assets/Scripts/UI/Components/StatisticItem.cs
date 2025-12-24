using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatisticItem : MonoBehaviour
{
    [SerializeField] private Image operatorIcon = default!;
    [SerializeField] private Image classIcon = default!;
    [SerializeField] private TextMeshProUGUI valueText = default!;
    [SerializeField] private Slider percentageBar = default!; // 하위 오브젝트에 Fill 이 있음
    [SerializeField] private TextMeshProUGUI percentageText = default!;
    [SerializeField] private Color damageDealtColor;
    [SerializeField] private Color damageTakenColor;
    [SerializeField] private Color healingDoneColor;
    private bool showPercentage;

    //public Operator Operator { get; private set; }
    public OperatorData OpData { get; private set; } = default!;
    private StatisticsManager.OperatorStats opStats; 
    private StatisticsManager.StatType currentStatType;
    private Image fillImage = default!; // 게이지 길이를 나타내는 이미지

    /// <summary>
    /// StatItem을 초기화하고 필요한 이벤트를 구독합니다.
    /// </summary>
    public void Initialize(OperatorData opData, StatisticsManager.StatType statType, bool showPercentage)
    {
        OpData = opData;
        currentStatType = statType;

        opStats = StatisticsManager.Instance!.GetAllOperatorStats().Find(s => s.opData == opData);
        fillImage = percentageBar.fillRect.GetComponent<Image>();

        SetIcons(opData);
        SetBarColor(statType);
        SetDisplayMode(showPercentage); // 절대 수치 / 백분율 전환

        // 1프레임 대기 후에 업데이트 실행(percentage 실제 반영 문제 때문에 넣음)
        StartCoroutine(DelayedUpdate(statType, showPercentage));
    }

    // 오퍼레이터와 클래스 아이콘을 할당함
    private void SetIcons(OperatorData opData)
    {
        // 오퍼레이터 고유 아이콘 할당, 없다면 모델 머티리얼의 색을 가져옴
        if (opData.Icon != null)
        {
            operatorIcon.sprite = opData.Icon;
        }
        else
        {
            operatorIcon.color = opData.PrimaryColor;
        }

        // 클래스 아이콘 할당, 없다면 검은색으로 표시
        Sprite? classSprite = GameManagement.Instance.ResourceManager.IconData.GetClassIcon(opData.OperatorClass);
        if (classSprite != null)
        {
            classIcon.sprite = classSprite;
        }
        else
        {
            classIcon.color = new Color(0, 0, 0, 0);
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
    private void OnStatUpdated(OperatorData opData, StatisticsManager.StatType statType)
    {
        if (this.OpData == opData && statType == currentStatType)
        {
            UpdateDisplay(statType, showPercentage);
        }
    }

    /// <summary>
    /// StatItem의 디스플레이를 업데이트합니다.
    /// </summary>
    public void UpdateDisplay(StatisticsManager.StatType statType, bool showPercentage)
    {
        float value = StatisticsManager.Instance!.GetOperatorValueForStatType(opStats, statType);
        float totalValue = StatisticsManager.Instance!.GetTotalValueForStatType(statType);

        float percentage = (totalValue > 0) ? (value / totalValue) : 0; // 전체 값 중 해당 오퍼레이터가 기여한 값

        percentageBar.normalizedValue = percentage;

        // 값이 0이면 숨기기
        if (value == 0) 
        {
            gameObject.SetActive(false);
            return;
        } 

        gameObject.SetActive(true);

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