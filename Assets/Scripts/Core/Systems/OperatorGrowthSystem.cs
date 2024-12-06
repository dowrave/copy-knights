
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

    public static readonly int Elite0MaxLevel = 50;
    public static readonly int Elite1MaxLevel = 60;

    // ����ġ ���� ����� ���������� ����
    private const int BASE_EXP_FOR_LEVEL_UP = 100; // ���۷����� 1->2������ ���� ���� �ʿ��� ����ġ�� ��
    private const float EXP_INCREASE_PER_LEVEL = 17f; 

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

    public static int GetRequiredExp(int currentLevel)
    {
        if (currentLevel < 1) return 0;

        // ����ȭ �ݿ� �ʿ�

        return Mathf.RoundToInt(BASE_EXP_FOR_LEVEL_UP + (EXP_INCREASE_PER_LEVEL * (currentLevel - 1)));
    }

    public static bool CanLevelUp(int currentLevel, ElitePhase currentPhase, int currentExp)
    {
        int maxLevel = GetMaxLevel(currentPhase);
        if (currentLevel >= maxLevel) return false;
        return true;
    }

    public static bool CanPromote(int currentLevel, ElitePhase currentPhase)
    {
        return currentPhase == ElitePhase.Elite0 &&
            currentLevel >= GetMaxLevel(currentPhase);
    }

    public static bool CanPromote(OwnedOperator op)
    {
        return CanPromote(op.currentLevel, op.currentPhase);
    }

    public static int GetSafeExpAmount(int currentExp, int expToAdd, int currentLevel, ElitePhase currentPhase)
    {
        int maxLevel = GetMaxLevel(currentPhase);
        if (currentLevel >= maxLevel) return currentExp; // �ִ� ���������� ����ġ ȹ�� �Ұ���

        int requiredExpForMaxLevel = GetRequiredExp(maxLevel);
        return Mathf.Min(currentExp + expToAdd, requiredExpForMaxLevel); 
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
