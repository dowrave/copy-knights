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

    // ������ ���� ������ �̷��� �޾Ƶ�
    private RectTransform containerRect;
    private Vector2 inactivePosition;
    private Vector2 activePosition;

    private List<StatisticItem> activeStatItems = new List<StatisticItem>();
    private StatisticsManager.StatType currentDisplayedStatType;

    private void Awake()
    {
        // Ȱ��ȭ ���ο� ���� �����̳��� ��ġ ����
        containerRect = GetComponent<RectTransform>();
        float panelWidth = statisticsPanel.GetComponent<RectTransform>().rect.width; 
        inactivePosition = containerRect.anchoredPosition;
        activePosition = new Vector2(inactivePosition.x + panelWidth, inactivePosition.y);

        // ��� ��ư �ؽ�Ʈ
        toggleButtonText = statisticsToggleButton.GetComponentInChildren<TextMeshProUGUI>();

        // �̺�Ʈ ������
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
    /// ��� �г� ǥ�� ��ȯ
    /// </summary>
    private void ToggleStats()
    {
        // �ٲ� ����
        bool willBeActive = !statisticsPanel.activeSelf;

        // �ٲ� ���°� Ȱ���� ���
        if (willBeActive)
        {
            statisticsPanel.SetActive(true);

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
                    statisticsPanel.SetActive(false);
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
    }

    /// <summary>
    /// ���� ǥ�� ���� ��� ������ ���� StatItem���� ������Ʈ�մϴ�.
    /// </summary>
    public void UpdateStatItems(StatisticsManager.StatType statType)
    {
        if (statType != currentDisplayedStatType) return;

        var topOperators = StatisticsManager.Instance.GetTopOperators(statType, 3);

        // ���� StatItem ������Ʈ �Ǵ� ����
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

        // ���ο� StatItem �߰�
        for (int i = activeStatItems.Count; i < topOperators.Count; i++)
        {
            CreateStatItem(topOperators[i].op, statType);
        }

        ReorganizeStatItems();
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
