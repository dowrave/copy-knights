using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class StatsPanel : MonoBehaviour
{
    [SerializeField] private GameObject statisticsContainer;
    [SerializeField] private Button statisticsToggleButton;
    public bool IsActive { get; private set; }
    TextMeshProUGUI toggleButtonText;

    [Header("StatisticsItem")]
    [SerializeField] private Transform statsItemContainer;
    [SerializeField] private GameObject statItemPrefab;
    [SerializeField] private Button statsItemContainerButton; // 절대 수치 <-> 퍼센티지 변환 버튼, StatsItemContainer 그 자체
    [SerializeField] private StatisticItem[] statItems;

    [SerializeField] private Button damageDealtTab;
    [SerializeField] private Button damageTakenTab;
    [SerializeField] private Button healingDoneTab;

    private bool showPercentage = false;

    // 수정할 수도 있으니 이렇게 달아둠
    private RectTransform containerRect;
    private Vector2 inactivePosition;
    private Vector2 activePosition;

    private List<StatisticItem> activeStatItems = new List<StatisticItem>();
    private StatisticsManager.StatType currentDisplayedStatType;

    [SerializeField] private Color defaultTabColor;
    [SerializeField] private Color selectedTabColor;

    private void Awake()
    {
        // 활성화 여부에 따른 컨테이너의 위치 설정
        containerRect = GetComponent<RectTransform>();
        float panelWidth = statisticsContainer.GetComponent<RectTransform>().rect.width; 
        inactivePosition = containerRect.anchoredPosition;
        activePosition = new Vector2(inactivePosition.x + panelWidth, inactivePosition.y);

        // 토글 버튼 텍스트
        toggleButtonText = statisticsToggleButton.GetComponentInChildren<TextMeshProUGUI>();

        // 이벤트 리스너
        statisticsToggleButton.onClick.AddListener(ToggleStats); // 패널 띄우는 온오프 버튼
        statsItemContainerButton.onClick.AddListener(ToggleDisplayMode); // 수치 변환 버튼
        damageDealtTab.onClick.AddListener(() => ShowStats(StatisticsManager.StatType.DamageDealt));
        damageTakenTab.onClick.AddListener(() => ShowStats(StatisticsManager.StatType.DamageTaken));
        healingDoneTab.onClick.AddListener(() => ShowStats(StatisticsManager.StatType.HealingDone));

        // 버튼 초기 색상 설정 : 이미 있는 걸 가져옴
        defaultTabColor = damageDealtTab.colors.normalColor;
    }

    private void Start()
    {
        // 최초에는 모두 비활성화
        foreach (StatisticItem statItem in statItems)
        {
            statItem.gameObject.SetActive(false);
        }

        StatisticsManager.Instance.OnStatUpdated += ShowStatsByEvent;
        statisticsContainer.SetActive(false);
    }


    /// <summary>
    /// 통계 패널 표시 전환
    /// </summary>
    private void ToggleStats()
    {
        // 바뀔 상태
        IsActive = !statisticsContainer.activeSelf;

        // 바뀔 상태가 활성인 경우
        if (IsActive)
        {
            statisticsContainer.SetActive(true);

            // 활성 위치로 애니메이션
            containerRect.DOAnchorPos(activePosition, 0.3f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    toggleButtonText.text = "<";
                    ShowStats(StatisticsManager.StatType.DamageDealt);
                });

        }
        // 바뀔 상태가 비활성
        else
        {
            containerRect.DOAnchorPos(inactivePosition, 0.3f)
                .SetEase(Ease.InQuad)
                .OnComplete(() =>
                {
                    statisticsContainer.SetActive(false);
                    toggleButtonText.text = ">";
                });
        }
    }

    /// <summary>
    /// 특정 통계 유형에 대한 정보를 표시합니다.
    /// </summary>
    private void ShowStats(StatisticsManager.StatType statType)
    {
        currentDisplayedStatType = statType;
        UpdateStatItems(statType);
        SetTabColors(statType);
    }

    private void ShowStatsByEvent(StatisticsManager.StatType statType)
    {
        if (!IsActive) return; // 패널이 활성화되지 않았다면 실행할 필요 X
        if (currentDisplayedStatType != statType) return; // 현재 패널에 띄운 타입의 업데이트가 아닐 경우도 실행 X
        ShowStats(statType);
    }


    /// <summary>
    /// 선택된 탭의 색상을 변경합니다.
    /// </summary>
    private void SetTabColors(StatisticsManager.StatType selectedType)
    {
        SetTabColor(damageDealtTab, selectedType == StatisticsManager.StatType.DamageDealt);
        SetTabColor(damageTakenTab, selectedType == StatisticsManager.StatType.DamageTaken);
        SetTabColor(healingDoneTab, selectedType == StatisticsManager.StatType.HealingDone);
    }

    /// <summary>
    /// 개별 탭 버튼의 색상을 설정합니다.
    /// </summary>
    private void SetTabColor(Button tab, bool isSelected)
    {
        var colors = tab.colors;
        Color targetColor = isSelected ? selectedTabColor : defaultTabColor;

        colors.normalColor = targetColor;
        colors.highlightedColor = targetColor * 1.1f;
        colors.pressedColor = targetColor * 0.9f;
        colors.selectedColor = targetColor;
        colors.disabledColor = targetColor * 0.5f;

        tab.colors = colors;
    }

    /// <summary>
    /// 현재 표시 중인 통계 유형에 대한 StatItem들을 업데이트합니다.
    /// </summary>
    public void UpdateStatItems(StatisticsManager.StatType statType)
    {
        // 스탯에 따른 상위 3개의 (오퍼레이터 + 스탯) 반환
        var topOperators = StatisticsManager.Instance.GetTopOperators(statType, 3);

        for (int i=0; i < statItems.Count(); i++)
        {
            UpdateStatItem(statItems[i], topOperators, i, statType);
        }
    }

    /// <summary>
    /// 각 StatItem 들을 업데이트하는 로직
    /// </summary>
    private void UpdateStatItem(StatisticItem item, List<StatisticsManager.OperatorStats> stats, int index, StatisticsManager.StatType statType)
    {

        if (index < stats.Count)
        {
            Operator op = stats[index].op;
            StatisticsManager.OperatorStats stat = stats[index];
            float value = StatisticsManager.Instance.GetOperatorValueForStatType(stat, statType);

            if (value > 0) // 기여한 값이 0이라면 나타나지 않게 함
            {
                item.gameObject.SetActive(true);
                item.Initialize(stats[index].op, statType, showPercentage);
                item.UpdateDisplay(statType, showPercentage);
            }
            else
            {
                item.gameObject.SetActive(false);
            }
        }
        else
        {
            item.gameObject.SetActive(false);
        }
    }

    private void ToggleDisplayMode()
    {
        showPercentage = !showPercentage;
        UpdateStatItems(currentDisplayedStatType);
    }

}
