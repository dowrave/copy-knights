

using System.Collections.Generic;

/// <summary>
/// 실제로 보유한 오퍼레이터는 이런 식으로 저장된다.
/// </summary>
[System.Serializable]
public class OwnedOperator
{
    // 직렬화해서 저장하는 부분 - PlayerPrefs에 저장됨
    public string operatorName; // OperatorData의 entityName과 매칭
    public int currentLevel = 1;
    public OperatorGrowthSystem.ElitePhase currentPhase = OperatorGrowthSystem.ElitePhase.Elite0;
    public int selectedSkillIndex = 0;
    public int currentExp = 0; 

    public bool CanLevelUp => OperatorGrowthSystem.CanLevelUp(currentLevel, currentPhase, currentExp);
    public bool CanPromote => OperatorGrowthSystem.CanPromote(currentLevel, currentPhase);

    // 게임 시작 시에 로드해서 직렬화하지 않는 정보
    [System.NonSerialized]
    private OperatorData _baseData; 
    public OperatorData BaseData
    {
        get
        {
            // 최초 접근 시
            if (_baseData == null) 
            {
                // Lazy Loading에서 이런 식으로 게터임에도 필드를 할당하는 건 잘 확립된 방식임
                _baseData = PlayerDataManager.Instance.GetOperatorData(operatorName);  // PlayerDataManager에서 operatorID에 해당하는 OperatorData를 가져옴
            }

            return _baseData;
        }
    }

    /// <summary>
    /// 레벨이 반영된 현재 스탯 계산
    /// </summary>
    public OperatorStats GetCurrentStats()
    {
        OperatorStats baseStats = BaseData.stats;
        OperatorData.OperatorLevelStats levelUpStats = BaseData.levelStats;
        int levelDifference = currentLevel - 1;

        OperatorStats currentStats = new OperatorStats
        {
            AttackPower = baseStats.AttackPower * (levelUpStats.attackPowerPerLevel * levelDifference),
            Health = baseStats.Health * (levelUpStats.healthPerLevel * levelDifference),
            Defense = baseStats.Defense * (levelUpStats.defensePerLevel * levelDifference),
            MagicResistance = baseStats.MagicResistance * (levelUpStats.magicResistancePerLevel * levelDifference),
            AttackSpeed = baseStats.AttackSpeed,
            DeploymentCost = baseStats.DeploymentCost,
            MaxBlockableEnemies = baseStats.MaxBlockableEnemies,
            RedeployTime = baseStats.RedeployTime,
            SPRecoveryRate = baseStats.SPRecoveryRate,
            StartSP = baseStats.StartSP
        };

        return currentStats; 
    }

    /// <summary>
    /// 정예화 시에 해금되는 요소 적용
    /// </summary>
    public void ApplyElitePhaseUnlocks()
    {
        if (currentPhase >= OperatorGrowthSystem.ElitePhase.Elite1)
        {
            // 공격 범위 변경
            var newAttackTiles = BaseData.attackableTiles;
            newAttackTiles.AddRange(BaseData.elite1Unlocks.additionalAttackTiles);
            BaseData.attackableTiles = newAttackTiles;

            // 새로운 스킬 추가
            if (BaseData.elite1Unlocks.unlockedSkill != null && !BaseData.skills.Contains(BaseData.elite1Unlocks.unlockedSkill))
            {
                BaseData.skills.Add(BaseData.elite1Unlocks.unlockedSkill);
            }
        }
    }
}