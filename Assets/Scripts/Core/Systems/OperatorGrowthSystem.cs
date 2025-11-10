
using UnityEngine;

/// <summary>
/// 오퍼레이터 성장 시스템의 규칙 정의
/// 정예화 별 최대 레벨, 정예화 수, 레벨업에 필요한 경험치 등
/// </summary>
public static class OperatorGrowthSystem
{
    private static OperatorLevelData? levelData;
    public static readonly int Elite0MaxLevel = 50;
    public static readonly int Elite1MaxLevel = 60;

    public static void Initalize(OperatorLevelData data)
    {
        levelData = data;
    }

    public static int GetMaxLevel(OperatorElitePhase phase)
    {
        // C#의 패턴 매칭을 사용한 switch 표현식
        return phase switch
        {
            OperatorElitePhase.Elite0 => Elite0MaxLevel,
            OperatorElitePhase.Elite1 => Elite1MaxLevel,
            _ => 1
        };
    }


    // 다음 레벨로 넘어가기 위해 필요한 경험치량. 현재 경험치량에 관계 없다.
    public static int GetMaxExpForNextLevel(OperatorElitePhase phase, int currentLevel)
    {
        InstanceValidator.ValidateInstance(levelData);

        return levelData!.GetExpRequirement(phase, currentLevel);
    }


    // 현재 레벨에서 다음 레벨로 가기 위해 남은 경험치량을 계산

    public static int GetRemainingExpForNextLevel(OperatorElitePhase phase, int currentLevel, int currentExp)
        {
            // 정예화 가능 레벨이거나 최대 레벨이면 0 반환
            if ((phase == OperatorElitePhase.Elite0 && currentLevel >= 50) ||
                (phase == OperatorElitePhase.Elite1 && currentLevel >= 60))
            {
                return 0;
            }

            int requiredExp = GetMaxExpForNextLevel(phase, currentLevel);
            return Mathf.Max(0, requiredExp - currentExp);
        }

    // 같은 정예화에서 현재 상태에서 목표 레벨까지 도달하는데 필요한 "총 경험치"를 계산합니다.
    public static int GetTotalExpRequiredForLevel(OperatorElitePhase phase, int currentLevel, int targetLevel, int currentExp)
    {
        // 유효하지 않은 입력 검증
        if (currentLevel >= targetLevel) return 0;
        if (currentLevel < 1 || targetLevel < 1) return 0;
        if (phase == OperatorElitePhase.Elite0 && targetLevel > 50) return 0;
        if (phase == OperatorElitePhase.Elite1 && targetLevel > 60) return 0;

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

    // 주어진 경험치로 도달 가능한 최대 레벨을 계산합니다.
    // 현재의 정예화 단계 내에서만 계산합니다.
    public static (int reachableLevel, int remainingExp) CalculateReachableLevel(
        OperatorElitePhase phase, 
        int currentLevel, 
        int totalExp)
    {
        InstanceValidator.ValidateInstance(levelData);

        int remainingExp = totalExp;
        int level = currentLevel;
        int maxLevel = (phase == OperatorElitePhase.Elite0) ? 50 : 60;

        // 경험치를 소모하면서 도달 가능한 최대 레벨 계산
        while (level < maxLevel)
        {
            int requiredExp = levelData!.GetExpRequirement(phase, level);
            if (requiredExp == 0 || remainingExp < requiredExp) break;

            remainingExp -= requiredExp;
            level++;
        }

        return (level, remainingExp);
    }

    public static bool CanLevelUp(OperatorElitePhase currentPhase, int currentLevel)
    {
        int maxLevel = GetMaxLevel(currentPhase);
        if (currentLevel >= maxLevel) return false;
        return true;
    }


    /// 현재 레벨에서 정예화 승급이 가능한지 확인.
    public static bool CanPromote(OperatorElitePhase phase, int currentLevel)
    {
        return phase == OperatorElitePhase.Elite0 && currentLevel >= 50;
    }


    /// CanPromote의 래핑 함수
    public static bool CanPromote(OwnedOperator op)
    {
        return CanPromote(op.CurrentPhase, op.CurrentLevel) ;
    }

    public static int GetSafeExpAmount(int currentExp, int itemExp, int currentLevel, OperatorElitePhase currentPhase)
    {
        // 최대 레벨에서는 경험치 획득 불가능
        int maxLevel = GetMaxLevel(currentPhase);
        if (currentLevel >= maxLevel) return currentExp;

        int requiredExpForMaxLevel = GetTotalExpRequiredForLevel(currentPhase, currentLevel, maxLevel, currentExp);
        return Mathf.Min(currentExp + itemExp, requiredExpForMaxLevel); 
    }

    // 각 정예화 및 레벨에서의 스탯 계산. 기준은 0정예화 1레벨이다.
    public static OperatorStats CalculateStats(OwnedOperator op, int targetLevel, OperatorElitePhase targetPhase)
    {
        // 기반 스탯
        OperatorStats baseStats = op.OperatorData!.Stats; 
        OperatorData.OperatorLevelStats levelUpStats = op.OperatorData!.LevelStats!;

        // (사실상) 레벨 차이 계산하기
        int actualTargetLevel = CalculateActualLevel(targetPhase, targetLevel);
        int baseLevel = CalculateActualLevel(OperatorElitePhase.Elite0, 1);
        int levelDifference = actualTargetLevel - baseLevel;

        // 레벨 차이만큼 스탯 증가 적용
        return new OperatorStats
        {
            Health = baseStats.Health + (levelUpStats.healthPerLevel * levelDifference),
            AttackPower = baseStats.AttackPower + (levelUpStats.attackPowerPerLevel * levelDifference),
            Defense = baseStats.Defense + (levelUpStats.defensePerLevel * levelDifference),
            MagicResistance = baseStats.MagicResistance + (levelUpStats.magicResistancePerLevel * levelDifference),

            AttackSpeed = baseStats.AttackSpeed,
            DeploymentCost = baseStats.DeploymentCost,
            MaxBlockableEnemies = baseStats.MaxBlockableEnemies,
            RedeployTime = baseStats.RedeployTime,
            SPRecoveryRate = baseStats.SPRecoveryRate
        };
    }


    // 현재 정예화와 레벨을 기반으로 실질적인 레벨을 계산한다.
    // ex) 1정예화 60레벨은 실질적으로 0정예화 109레벨(0정예화 50레벨 = 1정예화 1레벨)로 계산됨.
    public static int CalculateActualLevel(OperatorElitePhase phase, int currentLevel)
    {
        return phase switch
        {
            OperatorElitePhase.Elite0 => currentLevel,
            OperatorElitePhase.Elite1 => Elite0MaxLevel + (currentLevel - 1),
            _ => currentLevel
        };
    }
}
