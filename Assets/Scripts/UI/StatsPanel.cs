using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class StatsPanel : MonoBehaviour
{
    [SerializeField] private GameObject statisticsContainer;
    [SerializeField] private Button statisticsToggleButton;
    TextMeshProUGUI toggleButtonText;
    [SerializeField] private Transform statsItemContainer;
    [SerializeField] private GameObject statItemPrefab;

    [SerializeField] private StatisticItem firstStatItem;
    [SerializeField] private StatisticItem secondStatItem;
    [SerializeField] private StatisticItem thirdStatItem;

    [SerializeField] private Button damageDealtTab;
    [SerializeField] private Button damageTakenTab;
    [SerializeField] private Button healingDoneTab;

    // 수정할 수도 있으니 이렇게 달아둠
    private RectTransform containerRect;
    private Vector2 inactivePosition;
    private Vector2 activePosition;

    private List<StatisticItem> activeStatItems = new List<StatisticItem>();
    private StatisticsManager.StatType currentDisplayedStatType;

    private Color defaultTabColor;
    [SerializeField] private Color selectedTabColor = new Color(100, 100, 100);

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
        statisticsToggleButton.onClick.AddListener(ToggleStats);
        damageDealtTab.onClick.AddListener(() => ShowStats(StatisticsManager.StatType.DamageDealt));
        damageTakenTab.onClick.AddListener(() => ShowStats(StatisticsManager.StatType.DamageTaken));
        healingDoneTab.onClick.AddListener(() => ShowStats(StatisticsManager.StatType.HealingDone));

        // 버튼 초기 색상 설정 : 이미 있는 걸 가져옴
        defaultTabColor = damageDealtTab.colors.normalColor;
    }
    private void Start()
    {
        StatisticsManager.Instance.OnStatUpdated += ShowStatsByEvent;

        // 최초에는 모두 비활성화
        firstStatItem.gameObject.SetActive(false);
        secondStatItem.gameObject.SetActive(false);
        thirdStatItem.gameObject.SetActive(false);

        statisticsContainer.SetActive(false);

    }


    /// <summary>
    /// 통계 패널 표시 전환
    /// </summary>
    private void ToggleStats()
    {
        // 바뀔 상태
        bool willBeActive = !statisticsContainer.activeSelf;

        // 바뀔 상태가 활성인 경우
        if (willBeActive)
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
        if (currentDisplayedStatType != statType) return;
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
        colors.normalColor = isSelected ? selectedTabColor : defaultTabColor;
        tab.colors = colors;
    }

    /// <summary>
    /// 현재 표시 중인 통계 유형에 대한 StatItem들을 업데이트합니다.
    /// </summary>
    public void UpdateStatItems(StatisticsManager.StatType statType)
    {
        var topOperators = StatisticsManager.Instance.GetTopOperators(statType, 3);

        // 상위 3개의 statItem 반영 (topOperator 갯수에 따른 작동 여부는 메서드 내에 있음)
        UpdateStatItem(firstStatItem, topOperators, 0, statType);
        UpdateStatItem(secondStatItem, topOperators, 1, statType);
        UpdateStatItem(thirdStatItem, topOperators, 2, statType);

    }

    private void UpdateStatItem(StatisticItem item, List<StatisticsManager.OperatorStats> stats, int index, StatisticsManager.StatType statType)
    {

        if (index < stats.Count)
        {
            Operator op = stats[index].op;
            float value = StatisticsManager.Instance.GetOperatorValueForStatType(op, statType);

            if (value > 0) // 기여한 값이 0이라면 나타나지 않게 함
            {
                item.gameObject.SetActive(true);
                item.Initialize(stats[index].op, statType);
                item.UpdateDisplay();
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

    /// <summary>
    /// 새로운 StatItem을 생성합니다.
    /// </summary>
    private void CreateStatItem(Operator op, StatisticsManager.StatType statType)
    {
        GameObject item = Instantiate(statItemPrefab, statsItemContainer);
        StatisticItem statItem = item.GetComponent<StatisticItem>();
        if (statItem != null)
        {
            statItem.Initialize(op, statType);
            activeStatItems.Add(statItem);
        }
    }

    /// <summary>
    /// StatItem들의 순서를 재정렬합니다.
    /// </summary>
    private void ReorganizeStatItems()
    {
        for (int i = 0; i < activeStatItems.Count; i++)
        {
            activeStatItems[i].transform.SetSiblingIndex(i);
        }
    }
}
