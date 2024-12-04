
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

    
    /// <summary>
    /// 해당 정예화 단계의 최대 레벨 반환
    /// </summary>
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

    /// <summary>
    /// 다음 레벨까지 가기 위해 필요한 경험치 양 계산
    /// </summary>
    public static int GetRequiredExp(int currentLevel)
    {
        if (currentLevel < 1) return 0;

        // 정예화 반영 필요

        return Mathf.RoundToInt(BASE_EXP_FOR_LEVEL_UP + (EXP_INCREASE_PER_LEVEL * (currentLevel - 1)));
    }

    /// <summary>
    /// 현재 정예화에서 만렙만 아니면 true로 놓겠음
    /// </summary>
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

    /// <summary>
    /// 오버플로우 없이 안전하게 경험치 획득
    /// </summary>
    public static int GetSafeExpAmount(int currentExp, int expToAdd, int currentLevel, ElitePhase currentPhase)
    {
        int maxLevel = GetMaxLevel(currentPhase);
        if (currentLevel >= maxLevel) return currentExp; // 최대 레벨에서는 경험치 획득 불가능

        int requiredExpForMaxLevel = GetRequiredExp(maxLevel);
        return Mathf.Min(currentExp + expToAdd, requiredExpForMaxLevel); 
    }

    /// <summary>
    /// (일단은) 현재 보유한 오퍼레이터의 특정 레벨에서의 스탯값을 계산함
    /// </summary>
    public static OperatorStats CalculateStatsForLevel(OwnedOperator op, int targetLevel)
    {
        int levelDifference = targetLevel - op.currentLevel;
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
}
