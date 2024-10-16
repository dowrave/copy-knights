using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class StatisticsManager : MonoBehaviour
{
    public static StatisticsManager Instance { get; private set; }

    [System.Serializable]
    public struct OperatorStats
    {
        public Operator op;
        public float damageDealt;
        public float damageTaken;
        public float healingDone;
    }

    private List<OperatorStats> allOperatorStats = new List<OperatorStats>();

    [SerializeField] private GameObject statisticsPanel;
    [SerializeField] private Button toggleButton;
    [SerializeField] private Transform statsContainer;
    [SerializeField] private GameObject statItemPrefab;

    [SerializeField] private Button damageDealtTab;
    [SerializeField] private Button damageTakenTab;
    [SerializeField] private Button healingDoneTab;

    public static event System.Action<Operator, StatType> OnStatUpdated;

    private List<StatisticItem> activeStatItems = new List<StatisticItem>();
    private StatType currentDisplayedStatType;

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

        toggleButton.onClick.AddListener(ToggleStatisticsPanel);
        damageDealtTab.onClick.AddListener(() => ShowStats(StatType.DamageDealt));
        damageTakenTab.onClick.AddListener(() => ShowStats(StatType.DamageTaken));
        healingDoneTab.onClick.AddListener(() => ShowStats(StatType.HealingDone));
    }

    private void Start()
    {
        statisticsPanel.SetActive(false);
    }

    public void UpdateDamageDealt(Operator op, float damage)
    {
        UpdateStat(op, damage, StatType.DamageDealt);
    }

    public void UpdateDamageTaken(Operator op, float damage)
    {
        UpdateStat(op, damage, StatType.DamageTaken);
    }

    public void UpdateHealingDone(Operator op, float healing)
    {
        UpdateStat(op, healing, StatType.HealingDone);
    }

    /// <summary>
    /// 특정 통계 유형에 대한 오퍼레이터의 값을 업데이트합니다.
    /// </summary>
    private void UpdateStat(Operator op, float value, StatType statType)
    {
        var stat = allOperatorStats.Find(s => s.op == op);
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
            stat = new OperatorStats { op = op };
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

        OnStatUpdated?.Invoke(op, statType);
        UpdateStatItems(statType);
    }

    /// <summary>
    /// 통계 패널의 표시 여부를 전환합니다.
    /// </summary>
    private void ToggleStatisticsPanel()
    {
        statisticsPanel.SetActive(!statisticsPanel.activeSelf);

        if (statisticsPanel.activeSelf)
        {
            ShowStats(StatType.DamageDealt);
        }
    }

    /// <summary>
    /// 특정 통계 유형에 대한 정보를 표시합니다.
    /// </summary>
    private void ShowStats(StatType statType)
    {
        currentDisplayedStatType = statType;
        UpdateStatItems(statType);
    }

    /// <summary>
    /// 현재 표시 중인 통계 유형에 대한 StatItem들을 업데이트합니다.
    /// </summary>
    private void UpdateStatItems(StatType statType)
    {
        if (statType != currentDisplayedStatType) return;

        var topOperators = GetTopOperators(statType, 3);

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
    private void CreateStatItem(Operator op, StatType statType)
    {
        GameObject item = Instantiate(statItemPrefab, statsContainer);
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

    /// <summary>
    /// 특정 통계 유형에 대해 상위 오퍼레이터들을 반환합니다.
    /// </summary>
    public List<OperatorStats> GetTopOperators(StatType statType, int count)
    {
        return allOperatorStats
            .OrderByDescending(s => GetOperatorValueForStatType(s.op, statType))
            .Take(count)
            .ToList();
    }

    /// <summary>
    /// 특정 오퍼레이터의 특정 통계 유형에 대한 값을 반환합니다.
    /// </summary>
    public float GetOperatorValueForStatType(Operator op, StatType statType)
    {
        var stat = allOperatorStats.Find(s => s.op == op);
        switch (statType)
        {
            case StatType.DamageDealt:
                return stat.damageDealt;
            case StatType.DamageTaken:
                return stat.damageTaken;
            case StatType.HealingDone:
                return stat.healingDone;
            default:
                return 0;
        }
    }

    /// <summary>
    /// 특정 통계 유형에 대한 모든 오퍼레이터의 총 값을 반환합니다.
    /// </summary>
    public float GetTotalValueForStatType(StatType statType)
    {
        return allOperatorStats.Sum(s => GetOperatorValueForStatType(s.op, statType));
    }

    public enum StatType
    {
        DamageDealt,
        DamageTaken,
        HealingDone
    }
}