
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

    private static OperatorLevelData levelData;

    public static readonly int Elite0MaxLevel = 50;
    public static readonly int Elite1MaxLevel = 60;

    public static void Initalize(OperatorLevelData data)
    {
        levelData = data;
    }

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
    /// 다음 레벨로 넘어가기 위해 필요한 경험치량. 현재 경험치량에 관계 없다.
    /// </summary>
    public static int GetMaxExpForNextLevel(ElitePhase phase, int currentLevel)
    {
        return levelData.GetExpRequirement(phase, currentLevel);
    }

    /// <summary>
    /// 현재 레벨에서 다음 레벨로 가기 위해 남은 경험치량을 계산합니다.
    /// 이미 최대 레벨이거나 정예화 가능한 상태라면 0을 반환합니다.
    /// </summary>
    public static int GetRemainingExpForNextLevel(ElitePhase phase, int currentLevel, int currentExp)
        {
            // 정예화 가능 레벨이거나 최대 레벨이면 0 반환
            if ((phase == ElitePhase.Elite0 && currentLevel >= 50) ||
                (phase == ElitePhase.Elite1 && currentLevel >= 60))
            {
                return 0;
            }

            int requiredExp = GetMaxExpForNextLevel(phase, currentLevel);
            return Mathf.Max(0, requiredExp - currentExp);
        }

    /// <summary>
    /// 현재 상태에서 목표 레벨까지 도달하는데 필요한 "총 경험치"를 계산합니다.
    /// 같은 정예화 내에서의 레벨업만 계산 가능합니다.
    /// </summary>
    public static int GetTotalExpRequiredForLevel(ElitePhase phase, int currentLevel, int targetLevel, int currentExp)
    {
        // 유효하지 않은 입력 검증
        if (currentLevel >= targetLevel) return 0;
        if (currentLevel < 1 || targetLevel < 1) return 0;
        if (phase == ElitePhase.Elite0 && targetLevel > 50) return 0;
        if (phase == ElitePhase.Elite1 && targetLevel > 60) return 0;

        int totalRequired = 0;

        // 1. 현재 레벨에서 다음 레벨로 가기 위해 필요한 남은 경험치
        totalRequired += GetRemainingExpForNextLevel(phase, currentLevel, currentExp);

        // 2. 그 다음 레벨부터 목표 레벨 전까지 필요한 모든 경험치
        for (int level = currentLevel + 1; level < targetLevel; level++)
        {
            totalRequired += GetMaxExpForNextLevel(phase, level);
        }

        return totalRequired;
    }

    /// <summary>
    /// 주어진 경험치로 도달 가능한 최대 레벨을 계산합니다.
    /// 현재의 정예화 단계 내에서만 계산합니다.
    /// </summary>
    public static (int reachableLevel, int remainingExp) CalculateReachableLevel(
        ElitePhase phase, int currentLevel, int totalExp)
    {
        int remainingExp = totalExp;
        int level = currentLevel;
        int maxLevel = (phase == ElitePhase.Elite0) ? 50 : 60;

        // 경험치를 소모하면서 도달 가능한 최대 레벨 계산
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
    /// 현재 레벨에서 정예화 승급이 가능한지 확인합니다.
    /// </summary>
    public static bool CanPromote(ElitePhase phase, int currentLevel)
    {
        return phase == ElitePhase.Elite0 && currentLevel >= 50;
    }

    /// <summary>
    /// CanPromote의 래핑 함수
    /// </summary>
    public static bool CanPromote(OwnedOperator op)
    {
        return CanPromote(op.currentPhase, op.currentLevel);
    }

    public static int GetSafeExpAmount(int currentExp, int itemExp, int currentLevel, ElitePhase currentPhase)
    {
        // 최대 레벨에서는 경험치 획득 불가능
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
