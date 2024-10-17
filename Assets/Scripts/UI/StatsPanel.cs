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

    // ������ ���� ������ �̷��� �޾Ƶ�
    private RectTransform containerRect;
    private Vector2 inactivePosition;
    private Vector2 activePosition;

    private List<StatisticItem> activeStatItems = new List<StatisticItem>();
    private StatisticsManager.StatType currentDisplayedStatType;

    private Color defaultTabColor;
    [SerializeField] private Color selectedTabColor = new Color(100, 100, 100);

    private void Awake()
    {
        // Ȱ��ȭ ���ο� ���� �����̳��� ��ġ ����
        containerRect = GetComponent<RectTransform>();
        float panelWidth = statisticsContainer.GetComponent<RectTransform>().rect.width; 
        inactivePosition = containerRect.anchoredPosition;
        activePosition = new Vector2(inactivePosition.x + panelWidth, inactivePosition.y);

        // ��� ��ư �ؽ�Ʈ
        toggleButtonText = statisticsToggleButton.GetComponentInChildren<TextMeshProUGUI>();

        // �̺�Ʈ ������
        statisticsToggleButton.onClick.AddListener(ToggleStats);
        damageDealtTab.onClick.AddListener(() => ShowStats(StatisticsManager.StatType.DamageDealt));
        damageTakenTab.onClick.AddListener(() => ShowStats(StatisticsManager.StatType.DamageTaken));
        healingDoneTab.onClick.AddListener(() => ShowStats(StatisticsManager.StatType.HealingDone));

        // ��ư �ʱ� ���� ���� : �̹� �ִ� �� ������
        defaultTabColor = damageDealtTab.colors.normalColor;
    }
    private void Start()
    {
        StatisticsManager.Instance.OnStatUpdated += ShowStatsByEvent;

        // ���ʿ��� ��� ��Ȱ��ȭ
        firstStatItem.gameObject.SetActive(false);
        secondStatItem.gameObject.SetActive(false);
        thirdStatItem.gameObject.SetActive(false);

        statisticsContainer.SetActive(false);

    }


    /// <summary>
    /// ��� �г� ǥ�� ��ȯ
    /// </summary>
    private void ToggleStats()
    {
        // �ٲ� ����
        bool willBeActive = !statisticsContainer.activeSelf;

        // �ٲ� ���°� Ȱ���� ���
        if (willBeActive)
        {
            statisticsContainer.SetActive(true);

            // Ȱ�� ��ġ�� �ִϸ��̼�
            containerRect.DOAnchorPos(activePosition, 0.3f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    toggleButtonText.text = "<";
                    ShowStats(StatisticsManager.StatType.DamageDealt);
                });

        }
        // �ٲ� ���°� ��Ȱ��
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
    /// Ư�� ��� ������ ���� ������ ǥ���մϴ�.
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
    /// ���õ� ���� ������ �����մϴ�.
    /// </summary>
    private void SetTabColors(StatisticsManager.StatType selectedType)
    {
        SetTabColor(damageDealtTab, selectedType == StatisticsManager.StatType.DamageDealt);
        SetTabColor(damageTakenTab, selectedType == StatisticsManager.StatType.DamageTaken);
        SetTabColor(healingDoneTab, selectedType == StatisticsManager.StatType.HealingDone);
    }

    /// <summary>
    /// ���� �� ��ư�� ������ �����մϴ�.
    /// </summary>
    private void SetTabColor(Button tab, bool isSelected)
    {
        var colors = tab.colors;
        colors.normalColor = isSelected ? selectedTabColor : defaultTabColor;
        tab.colors = colors;
    }

    /// <summary>
    /// ���� ǥ�� ���� ��� ������ ���� StatItem���� ������Ʈ�մϴ�.
    /// </summary>
    public void UpdateStatItems(StatisticsManager.StatType statType)
    {
        var topOperators = StatisticsManager.Instance.GetTopOperators(statType, 3);

        // ���� 3���� statItem �ݿ� (topOperator ������ ���� �۵� ���δ� �޼��� ���� ����)
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

            if (value > 0) // �⿩�� ���� 0�̶�� ��Ÿ���� �ʰ� ��
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
    /// ���ο� StatItem�� �����մϴ�.
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
    /// StatItem���� ������ �������մϴ�.
    /// </summary>
    private void ReorganizeStatItems()
    {
        for (int i = 0; i < activeStatItems.Count; i++)
        {
            activeStatItems[i].transform.SetSiblingIndex(i);
        }
    }
}
