using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

    [SerializeField] private StatsPanel statsPanel;

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

        OnStatUpdated?.Invoke(statType);
        //statsPanel.UpdateStatItems(statType);
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
    /// OperatorStats 구조체 리스트를 반환합니다. 
    /// 마지막에
    /// </summary>
    public List<OperatorStats> GetAllOperatorStats()
    {
        return new List<OperatorStats>(allOperatorStats);
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