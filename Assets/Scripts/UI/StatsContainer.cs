using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class StatsContainer : MonoBehaviour
{
    [SerializeField] private GameObject statisticsPanel;
    [SerializeField] private Button statisticsToggleButton;
    TextMeshProUGUI toggleButtonText;
    [SerializeField] private Transform statsItemContainer;
    [SerializeField] private GameObject statItemPrefab;

    [SerializeField] private Button damageDealtTab;
    [SerializeField] private Button damageTakenTab;
    [SerializeField] private Button healingDoneTab;

    // 수정할 수도 있으니 이렇게 달아둠
    private RectTransform containerRect;
    private Vector2 inactivePosition;
    private Vector2 activePosition;

    private List<StatisticItem> activeStatItems = new List<StatisticItem>();
    private StatisticsManager.StatType currentDisplayedStatType;

    private void Awake()
    {
        // 활성화 여부에 따른 컨테이너의 위치 설정
        containerRect = GetComponent<RectTransform>();
        float panelWidth = statisticsPanel.GetComponent<RectTransform>().rect.width; 
        inactivePosition = containerRect.anchoredPosition;
        activePosition = new Vector2(inactivePosition.x + panelWidth, inactivePosition.y);

        // 토글 버튼 텍스트
        toggleButtonText = statisticsToggleButton.GetComponentInChildren<TextMeshProUGUI>();

        // 이벤트 리스너
        statisticsToggleButton.onClick.AddListener(ToggleStats);
        damageDealtTab.onClick.AddListener(() => ShowStats(StatisticsManager.StatType.DamageDealt));
        damageTakenTab.onClick.AddListener(() => ShowStats(StatisticsManager.StatType.DamageTaken));
        healingDoneTab.onClick.AddListener(() => ShowStats(StatisticsManager.StatType.HealingDone));
    }
    private void Start()
    { 
        statisticsPanel.SetActive(false);
    }


    /// <summary>
    /// 통계 패널 표시 전환
    /// </summary>
    private void ToggleStats()
    {
        // 바뀔 상태
        bool willBeActive = !statisticsPanel.activeSelf;

        // 바뀔 상태가 활성인 경우
        if (willBeActive)
        {
            statisticsPanel.SetActive(true);

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
                    statisticsPanel.SetActive(false);
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
    }

    /// <summary>
    /// 현재 표시 중인 통계 유형에 대한 StatItem들을 업데이트합니다.
    /// </summary>
    public void UpdateStatItems(StatisticsManager.StatType statType)
    {
        if (statType != currentDisplayedStatType) return;

        var topOperators = StatisticsManager.Instance.GetTopOperators(statType, 3);

        // 기존 StatItem 업데이트 또는 제거
        for (int i = activeStatItems.Count - 1; i >= 0; i--)
        {
            if (i < topOperators.Count && topOperators[i].op == activeStatItems[i].op)
            {
                activeStatItems[i].UpdateDisplay();
            }
            else
            {
                Destroy(activeStatItems[i].gameObject);
                activeStatItems.RemoveAt(i);
            }
        }

        // 새로운 StatItem 추가
        for (int i = activeStatItems.Count; i < topOperators.Count; i++)
        {
            CreateStatItem(topOperators[i].op, statType);
        }

        ReorganizeStatItems();
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
