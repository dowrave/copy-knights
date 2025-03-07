using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StatisticsManager : MonoBehaviour
{
    public static StatisticsManager Instance { get; private set; }


    /// <summary>
    /// 스테이지에서의 오퍼레이터에 관한 통계 구조체.
    /// OperatorData.OperatorStats와 헷갈리지 않도록 유의!
    /// </summary>
    [System.Serializable]
    public struct OperatorStats
    {
        public OperatorData opData;
        public float damageDealt;
        public float damageTaken;
        public float healingDone;
    }

    private List<OperatorStats> allOperatorStats = new List<OperatorStats>();

    [SerializeField] private StatsPanel? statsPanel;

    public event System.Action<StatType> OnStatUpdated; // 일단 시각화할지 여부만 결정할 것이므로 StatType만 넣음

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

        if (statsPanel == null)
        {
            Debug.LogError("statsPanel이 할당되어 있지 않음!");
        }

    }


    public void UpdateDamageDealt(OperatorData op, float damage)
    {
        UpdateStat(op, damage, StatType.DamageDealt);
    }

    public void UpdateDamageTaken(OperatorData op, float damage)
    {
        UpdateStat(op, damage, StatType.DamageTaken);
    }

    public void UpdateHealingDone(OperatorData op, float healing)
    {
        UpdateStat(op, healing, StatType.HealingDone);
    }

    /// <summary>
    /// 특정 통계 유형에 대한 오퍼레이터의 값을 업데이트합니다.
    /// </summary>
    private void UpdateStat(OperatorData opData, float value, StatType statType)
    {
        var stat = allOperatorStats.Find(s => s.opData == opData);
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
            stat = new OperatorStats { opData = opData };
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

        OnStatUpdated?.Invoke(statType);
        //statsPanel.UpdateStatItems(statType);
    }


    // 특정 통계 유형에 대해 상위 오퍼레이터들을 반환합니다.
    public List<OperatorStats> GetTopOperators(StatType statType, int count)
    {
        return allOperatorStats
            .OrderByDescending(s => GetOperatorValueForStatType(s, statType))
            .Take(count)
            .ToList();
    }


    // OperatorStats 구조체 리스트를 반환합니다. 
    public List<OperatorStats> GetAllOperatorStats()
    {
        return new List<OperatorStats>(allOperatorStats);
    }

    // 특정 오퍼레이터의 특정 통계 유형에 대한 값을 반환합니다.
    public float GetOperatorValueForStatType(OperatorStats stat, StatType statType)
    {
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


    // 특정 통계 유형에 대한 모든 오퍼레이터의 값을 모두 더한 총합을 반환합니다.
    public float GetTotalValueForStatType(StatType statType)
    {
        return allOperatorStats.Sum(s => GetOperatorValueForStatType(s, statType));
    }

    // 특정 통계 유형에 대해, 내림차순으로 정렬된 오퍼레이터 통계를 반환합니다.
    public List<(OperatorData opData, float value)> GetSortedOperatorStats(StatType statType)
    {
        return allOperatorStats
            .Select(stat => (
                opData: stat.opData,
                value: GetOperatorValueForStatType(stat, statType)
            ))
            .OrderByDescending(x => x.value)
            .ToList();
    }

    public enum StatType
    {
        DamageDealt,
        DamageTaken,
        HealingDone
    }
}