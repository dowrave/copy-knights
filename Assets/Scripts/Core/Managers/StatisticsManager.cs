using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StatisticsManager : MonoBehaviour
{
    public static StatisticsManager Instance { get; private set; }


    /// <summary>
    /// �������������� ���۷����Ϳ� ���� ��� ����ü.
    /// OperatorData.OperatorStats�� �򰥸��� �ʵ��� ����!
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

    public event System.Action<StatType> OnStatUpdated; // �ϴ� �ð�ȭ���� ���θ� ������ ���̹Ƿ� StatType�� ����

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
            Debug.LogError("statsPanel�� �Ҵ�Ǿ� ���� ����!");
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
    /// Ư�� ��� ������ ���� ���۷������� ���� ������Ʈ�մϴ�.
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


    // Ư�� ��� ������ ���� ���� ���۷����͵��� ��ȯ�մϴ�.
    public List<OperatorStats> GetTopOperators(StatType statType, int count)
    {
        return allOperatorStats
            .OrderByDescending(s => GetOperatorValueForStatType(s, statType))
            .Take(count)
            .ToList();
    }


    // OperatorStats ����ü ����Ʈ�� ��ȯ�մϴ�. 
    public List<OperatorStats> GetAllOperatorStats()
    {
        return new List<OperatorStats>(allOperatorStats);
    }

    // Ư�� ���۷������� Ư�� ��� ������ ���� ���� ��ȯ�մϴ�.
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


    // Ư�� ��� ������ ���� ��� ���۷������� ���� ��� ���� ������ ��ȯ�մϴ�.
    public float GetTotalValueForStatType(StatType statType)
    {
        return allOperatorStats.Sum(s => GetOperatorValueForStatType(s, statType));
    }

    // Ư�� ��� ������ ����, ������������ ���ĵ� ���۷����� ��踦 ��ȯ�մϴ�.
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