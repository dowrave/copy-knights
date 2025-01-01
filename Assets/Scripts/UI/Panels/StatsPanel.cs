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
    [SerializeField] private Button statsItemContainerButton; // ���� ��ġ <-> �ۼ�Ƽ�� ��ȯ ��ư, StatsItemContainer �� ��ü
    [SerializeField] private StatisticItem[] statItems;

    [SerializeField] private Button damageDealtTab;
    [SerializeField] private Button damageTakenTab;
    [SerializeField] private Button healingDoneTab;

    private bool showPercentage = false;

    // ������ ���� ������ �̷��� �޾Ƶ�
    private RectTransform containerRect;
    private Vector2 inactivePosition;
    private Vector2 activePosition;

    private List<StatisticItem> activeStatItems = new List<StatisticItem>();
    private StatisticsManager.StatType currentDisplayedStatType;

    [SerializeField] private Color defaultTabColor;
    [SerializeField] private Color selectedTabColor;

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
        statisticsToggleButton.onClick.AddListener(ToggleStats); // �г� ���� �¿��� ��ư
        statsItemContainerButton.onClick.AddListener(ToggleDisplayMode); // ��ġ ��ȯ ��ư
        damageDealtTab.onClick.AddListener(() => ShowStats(StatisticsManager.StatType.DamageDealt));
        damageTakenTab.onClick.AddListener(() => ShowStats(StatisticsManager.StatType.DamageTaken));
        healingDoneTab.onClick.AddListener(() => ShowStats(StatisticsManager.StatType.HealingDone));

        // ��ư �ʱ� ���� ���� : �̹� �ִ� �� ������
        defaultTabColor = damageDealtTab.colors.normalColor;
    }

    private void Start()
    {
        // ���ʿ��� ��� ��Ȱ��ȭ
        foreach (StatisticItem statItem in statItems)
        {
            statItem.gameObject.SetActive(false);
        }

        StatisticsManager.Instance.OnStatUpdated += ShowStatsByEvent;
        statisticsContainer.SetActive(false);
    }


    /// <summary>
    /// ��� �г� ǥ�� ��ȯ
    /// </summary>
    private void ToggleStats()
    {
        // �ٲ� ����
        IsActive = !statisticsContainer.activeSelf;

        // �ٲ� ���°� Ȱ���� ���
        if (IsActive)
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
        if (!IsActive) return; // �г��� Ȱ��ȭ���� �ʾҴٸ� ������ �ʿ� X
        if (currentDisplayedStatType != statType) return; // ���� �гο� ��� Ÿ���� ������Ʈ�� �ƴ� ��쵵 ���� X
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
        Color targetColor = isSelected ? selectedTabColor : defaultTabColor;

        colors.normalColor = targetColor;
        colors.highlightedColor = targetColor * 1.1f;
        colors.pressedColor = targetColor * 0.9f;
        colors.selectedColor = targetColor;
        colors.disabledColor = targetColor * 0.5f;

        tab.colors = colors;
    }

    /// <summary>
    /// ���� ǥ�� ���� ��� ������ ���� StatItem���� ������Ʈ�մϴ�.
    /// </summary>
    public void UpdateStatItems(StatisticsManager.StatType statType)
    {
        // ���ȿ� ���� ���� 3���� (���۷����� + ����) ��ȯ
        var topOperators = StatisticsManager.Instance.GetTopOperators(statType, 3);

        for (int i=0; i < statItems.Count(); i++)
        {
            UpdateStatItem(statItems[i], topOperators, i, statType);
        }
    }

    /// <summary>
    /// �� StatItem ���� ������Ʈ�ϴ� ����
    /// </summary>
    private void UpdateStatItem(StatisticItem item, List<StatisticsManager.OperatorStats> stats, int index, StatisticsManager.StatType statType)
    {

        if (index < stats.Count)
        {
            Operator op = stats[index].op;
            StatisticsManager.OperatorStats stat = stats[index];
            float value = StatisticsManager.Instance.GetOperatorValueForStatType(stat, statType);

            if (value > 0) // �⿩�� ���� 0�̶�� ��Ÿ���� �ʰ� ��
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
