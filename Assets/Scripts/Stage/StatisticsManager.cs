

using System.Collections.Generic;
using System.Data;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;


public class StatisticsManager: MonoBehaviour
{
    // �̱���
    public static StatisticsManager Instance { get; private set; }

    // ��� ������ ����ü
    [System.Serializable]
    public struct OperatorStats
    {
        public Operator opName; 
        public float damageDealt; // ���� �����
        public float damageTaken; // ���� �����
        public float healingDone; // ȸ���� ��
    }

    private List<OperatorStats> allOperatorStats = new List<OperatorStats>();

    [SerializeField] private GameObject statisticsPanel;
    [SerializeField] private Button toggleButton;
    [SerializeField] private Transform statsContent;
    [SerializeField] private GameObject statItemPrefab; // ���� ���۷����� �� �⿩�� ǥ��

    // �� ��ư��
    [SerializeField] private Button damageDealtTab;
    [SerializeField] private Button damageTakenTab;
    [SerializeField] private Button healingDoneTab;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        // ��� ��ư �̺�Ʈ ������
        toggleButton.onClick.AddListener(ToggleStatisticsPanel);
        damageDealtTab.onClick.AddListener(() => ShowStats(StatType.DamageDealt));
        damageTakenTab.onClick.AddListener(() => ShowStats(StatType.DamageTaken));
        healingDoneTab.onClick.AddListener(() => ShowStats(StatType.HealingDone));

    }

    private void Start()
    {
        statisticsPanel.SetActive(false); // ��� ��ư �����߸� Ȱ��ȭ
    }

    // ��� ������Ʈ �޼���
    public void UpdateDamageDealt(Operator op, float damage)
    {
        UpdateStat(op, damage, StatType.DamageDealt);
    }
    public void UpdateDamageTaken(Operator op, float damage)
    {
        UpdateStat(op, damage, StatType.DamageTaken);
    }
    public void UpdateHealingDone(Operator op, float damage)
    {
        UpdateStat(op, damage, StatType.HealingDone);
    }

    private void UpdateStat(Operator op, float value, StatType statType)
    {
        var stat = allOperatorStats.Find(s => s.opName == op);
        int index = allOperatorStats.IndexOf(stat);

        if (index != -1)
        {
            switch (statType)
            {
                case StatType.DamageDealt:
                    stat.damageDealt += value;
                    break;
                case StatType.DamageTaken:
                    stat.damageTaken += value;
                    break;
                case StatType.HealingDone:
                    stat.healingDone += value;
                    break;
            }
            allOperatorStats[index] = stat;
        }
        else
        {
            stat = new OperatorStats { opName = op };
            switch (statType)
            {
                case StatType.DamageDealt:
                    stat.damageDealt = value;
                    break;
                case StatType.DamageTaken:
                    stat.damageTaken = value;
                    break;
                case StatType.HealingDone:
                    stat.healingDone = value;
                    break;
            }
            allOperatorStats.Add(stat);
        }
    }

    private void ToggleStatisticsPanel()
    {
        statisticsPanel.SetActive(!statisticsPanel.activeSelf);
        if (statisticsPanel.activeSelf)
        {
            ShowStats(StatType.DamageDealt); // �⺻ ǥ��
        }
    }

    private void ShowStats(StatType statType)
    {
        // ���� ��� �׸� ����
        foreach (Transform child in statsContent)
        {
            Destroy(child.gameObject);
        }

        var sortedStats = SortAndFilterStats(statType);
        Debug.Log($"���� ����");
        // ���� 3�� �׸� ǥ��
        for (int i = 0; i < Mathf.Min(3, sortedStats.Count); i++)
        {
            CreateStatItem(sortedStats[i], statType, GetTotalValueForStatType(statType));
        }
    }

    private List<OperatorStats> SortAndFilterStats(StatType statType)
    {
        return allOperatorStats
            .Where(s => GetValueForStatType(s, statType) > 0)
            .OrderByDescending(s => GetValueForStatType(s, statType))
            .ToList();
    }

    private float GetValueForStatType(OperatorStats stats, StatType statType)
    {
        switch (statType)
        {
            case StatType.DamageDealt:
                return stats.damageDealt;
            case StatType.DamageTaken:
                return stats.damageTaken;
            case StatType.HealingDone:
                return stats.healingDone;
            default:
                return 0;
        }
    }
    
    private float GetTotalValueForStatType(StatType statType)
    {
        return allOperatorStats.Sum(s => GetValueForStatType(s, statType));
    }

    private void CreateStatItem(OperatorStats stat, StatType statType, float totalValue)
    {
        GameObject item = Instantiate(statItemPrefab, statsContent);
        StatisticItem statItem = item.GetComponent<StatisticItem>();
        Debug.Log("statItem �ʱ�ȭ");

        if (statItem != null)
        {
            float value = GetValueForStatType(stat, statType);
            float percentage = (totalValue > 0) ? (value / totalValue) * 100 : 0;

            Debug.Log("statItem �ʱ�ȭ �۵�");

            statItem.Initialize(stat.opName, value, percentage, statType);
        }
    }

    public enum StatType
    {
        DamageDealt,
        DamageTaken,
        HealingDone
    }
}