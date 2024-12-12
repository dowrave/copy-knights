
using UnityEngine;

/// <summary>
/// ���۷����� ���� �ý����� ��Ģ ����
/// ����ȭ �� �ִ� ����, ����ȭ ��, �������� �ʿ��� ����ġ ��
/// </summary>
public static class OperatorGrowthSystem
{
    public enum ElitePhase
    {
        Elite0 = 0,  // �ʱ� ����
        Elite1 = 1   // 1�� ����ȭ
    }

    private static OperatorLevelData levelData;

    public static readonly int Elite0MaxLevel = 50;
    public static readonly int Elite1MaxLevel = 60;

    public static void Initalize(OperatorLevelData data)
    {
        levelData = data;
    }

    public static int GetMaxLevel(ElitePhase phase)
    {
        // ���� swtich (phase) {case Elite0: return Elite0MaxLevel ...} ���� ����������
        // �̰Ŵ� C#�� ���� ��Ī�� ����� switch ǥ������
        return phase switch
        {
            ElitePhase.Elite0 => Elite0MaxLevel,
            ElitePhase.Elite1 => Elite1MaxLevel,
            _ => 1
        };
    }

    /// <summary>
    /// ���� ������ �Ѿ�� ���� �ʿ��� ����ġ��. ���� ����ġ���� ���� ����.
    /// </summary>
    public static int GetMaxExpForNextLevel(ElitePhase phase, int currentLevel)
    {
        return levelData.GetExpRequirement(phase, currentLevel);
    }

    /// <summary>
    /// ���� �������� ���� ������ ���� ���� ���� ����ġ���� ����մϴ�.
    /// �̹� �ִ� �����̰ų� ����ȭ ������ ���¶�� 0�� ��ȯ�մϴ�.
    /// </summary>
    public static int GetRemainingExpForNextLevel(ElitePhase phase, int currentLevel, int currentExp)
        {
            // ����ȭ ���� �����̰ų� �ִ� �����̸� 0 ��ȯ
            if ((phase == ElitePhase.Elite0 && currentLevel >= 50) ||
                (phase == ElitePhase.Elite1 && currentLevel >= 60))
            {
                return 0;
            }

            int requiredExp = GetMaxExpForNextLevel(phase, currentLevel);
            return Mathf.Max(0, requiredExp - currentExp);
        }

    /// <summary>
    /// ���� ���¿��� ��ǥ �������� �����ϴµ� �ʿ��� "�� ����ġ"�� ����մϴ�.
    /// ���� ����ȭ �������� �������� ��� �����մϴ�.
    /// </summary>
    public static int GetTotalExpRequiredForLevel(ElitePhase phase, int currentLevel, int targetLevel, int currentExp)
    {
        // ��ȿ���� ���� �Է� ����
        if (currentLevel >= targetLevel) return 0;
        if (currentLevel < 1 || targetLevel < 1) return 0;
        if (phase == ElitePhase.Elite0 && targetLevel > 50) return 0;
        if (phase == ElitePhase.Elite1 && targetLevel > 60) return 0;

        int totalRequired = 0;

        // 1. ���� �������� ���� ������ ���� ���� �ʿ��� ���� ����ġ
        totalRequired += GetRemainingExpForNextLevel(phase, currentLevel, currentExp);

        // 2. �� ���� �������� ��ǥ ���� ������ �ʿ��� ��� ����ġ
        for (int level = currentLevel + 1; level < targetLevel; level++)
        {
            totalRequired += GetMaxExpForNextLevel(phase, level);
        }

        return totalRequired;
    }

    /// <summary>
    /// �־��� ����ġ�� ���� ������ �ִ� ������ ����մϴ�.
    /// ������ ����ȭ �ܰ� �������� ����մϴ�.
    /// </summary>
    public static (int reachableLevel, int remainingExp) CalculateReachableLevel(
        ElitePhase phase, int currentLevel, int totalExp)
    {
        int remainingExp = totalExp;
        int level = currentLevel;
        int maxLevel = (phase == ElitePhase.Elite0) ? 50 : 60;

        // ����ġ�� �Ҹ��ϸ鼭 ���� ������ �ִ� ���� ���
        while (level < maxLevel)
        {
            int requiredExp = levelData.GetExpRequirement(phase, level);
            if (requiredExp == 0 || remainingExp < requiredExp) break;

            remainingExp -= requiredExp;
            level++;
        }

        return (level, remainingExp);
    }

    public static bool CanLevelUp(ElitePhase currentPhase, int currentLevel)
    {
        int maxLevel = GetMaxLevel(currentPhase);
        if (currentLevel >= maxLevel) return false;
        return true;
    }

    /// <summary>
    /// ���� �������� ����ȭ �±��� �������� Ȯ���մϴ�.
    /// </summary>
    public static bool CanPromote(ElitePhase phase, int currentLevel)
    {
        return phase == ElitePhase.Elite0 && currentLevel >= 50;
    }

    /// <summary>
    /// CanPromote�� ���� �Լ�
    /// </summary>
    public static bool CanPromote(OwnedOperator op)
    {
        return CanPromote(op.currentPhase, op.currentLevel);
    }

    public static int GetSafeExpAmount(int currentExp, int itemExp, int currentLevel, ElitePhase currentPhase)
    {
        // �ִ� ���������� ����ġ ȹ�� �Ұ���
        int maxLevel = GetMaxLevel(currentPhase);
        if (currentLevel >= maxLevel) return currentExp;

        int requiredExpForMaxLevel = GetTotalExpRequiredForLevel(currentPhase, currentLevel, maxLevel, currentExp);
        return Mathf.Min(currentExp + itemExp, requiredExpForMaxLevel); 
    }

    public static OperatorStats CalculateStats(OwnedOperator op, int targetLevel, ElitePhase targetPhase)
    {
        int actualTargetLevel = CalculateActualLevel(targetPhase, targetLevel);
        int levelDifference = actualTargetLevel - op.currentLevel;

        OperatorData.OperatorLevelStats levelUpStats = op.BaseData.levelStats;

        return new OperatorStats
        {
            AttackPower = op.currentStats.AttackPower + levelUpStats.attackPowerPerLevel * levelDifference,
            Health = op.currentStats.Health + levelUpStats.healthPerLevel * levelDifference,
            Defense = op.currentStats.AttackPower + levelUpStats.defensePerLevel * levelDifference,
            MagicResistance = op.currentStats.MagicResistance + levelUpStats.magicResistancePerLevel * levelDifference,

            AttackSpeed = op.currentStats.AttackSpeed,
            DeploymentCost = op.currentStats.DeploymentCost,
            MaxBlockableEnemies = op.currentStats.MaxBlockableEnemies,
            RedeployTime = op.currentStats.RedeployTime,
            SPRecoveryRate = op.currentStats.SPRecoveryRate,
            StartSP = op.currentStats.StartSP
        };
    }

    /// <summary>
    /// ���� ����ȭ�� ������ ������� �������� ������ ����Ѵ�.
    /// ex) 1����ȭ 60������ ���������� 0����ȭ 109������ ����.
    /// </summary>
    public static int CalculateActualLevel(ElitePhase phase, int currentLevel)
    {
        return phase switch
        {
            ElitePhase.Elite0 => currentLevel,
            ElitePhase.Elite1 => Elite0MaxLevel + (currentLevel - 1),
            _ => currentLevel
        };
    }
}
