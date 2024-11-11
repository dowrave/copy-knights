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
    /// Ư�� ��� ������ ���� ���۷������� ���� ������Ʈ�մϴ�.
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
    /// Ư�� ��� ������ ���� ���� ���۷����͵��� ��ȯ�մϴ�.
    /// </summary>
    public List<OperatorStats> GetTopOperators(StatType statType, int count)
    {
        return allOperatorStats
            .OrderByDescending(s => GetOperatorValueForStatType(s.op, statType))
            .Take(count)
            .ToList();
    }

    /// <summary>
    /// OperatorStats ����ü ����Ʈ�� ��ȯ�մϴ�. 
    /// ��������
    /// </summary>
    public List<OperatorStats> GetAllOperatorStats()
    {
        return new List<OperatorStats>(allOperatorStats);
    }

    /// <summary>
    /// Ư�� ���۷������� Ư�� ��� ������ ���� ���� ��ȯ�մϴ�.
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
    /// Ư�� ��� ������ ���� ��� ���۷������� �� ���� ��ȯ�մϴ�.
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