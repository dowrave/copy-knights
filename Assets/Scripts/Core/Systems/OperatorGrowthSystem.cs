
using UnityEngine;

/// <summary>
/// 오퍼레이터 성장 시스템의 규칙 정의
/// 정예화 별 최대 레벨, 정예화 수, 레벨업에 필요한 경험치 등
/// </summary>
public static class OperatorGrowthSystem
{
    public enum ElitePhase
    {
        Elite0 = 0,  // 초기 상태
        Elite1 = 1   // 1차 정예화
    }

    public static readonly int Elite0MaxLevel = 50;
    public static readonly int Elite1MaxLevel = 60;

    // 경험치 관련 상수는 등차수열로 구현
    private const int BASE_EXP_FOR_LEVEL_UP = 100; // 오퍼레이터 1->2레벨로 가기 위해 필요한 경험치의 양
    private const float EXP_INCREASE_PER_LEVEL = 17f; 

    public static int GetMaxLevel(ElitePhase phase)
    {
        // 원래 swtich (phase) {case Elite0: return Elite0MaxLevel ...} 같은 서술이지만
        // 이거는 C#의 패턴 매칭을 사용한 switch 표현식임
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

        // 정예화 반영 필요

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
        if (currentLevel >= maxLevel) return currentExp; // 최대 레벨에서는 경험치 획득 불가능

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
    /// 현재 정예화와 레벨을 기반으로 실질적인 레벨을 계산한다.
    /// ex) 1정예화 60레벨은 실질적으로 0정예화 109레벨로 계산됨.
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
