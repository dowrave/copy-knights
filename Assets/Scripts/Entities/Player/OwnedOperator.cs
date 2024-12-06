using System.Collections.Generic;
using UnityEngine;

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
    public OperatorStats currentStats;
    public List<Vector2Int> currentAttackableTiles;

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
                _baseData = GameManagement.Instance.PlayerDataManager.GetOperatorData(operatorName);  // PlayerDataManager에서 operatorID에 해당하는 OperatorData를 가져옴
            }
            return _baseData;
        }
    }

    // 생성자
    public OwnedOperator(OperatorData opData)
    {
        operatorName = opData.entityName;
        currentAttackableTiles = new List<Vector2Int>(opData.attackableTiles);
        currentLevel = 1;
        currentPhase = OperatorGrowthSystem.ElitePhase.Elite0;
        currentExp = 0;
        selectedSkillIndex = 0;

        _baseData = opData;

        // 초기 스탯 설정
        currentStats = new OperatorStats
        {
            AttackPower = opData.stats.AttackPower,
            Health = opData.stats.Health,
            Defense = opData.stats.Defense,
            MagicResistance = opData.stats.MagicResistance,
            AttackSpeed = opData.stats.AttackSpeed,
            DeploymentCost = opData.stats.DeploymentCost,
            MaxBlockableEnemies = opData.stats.MaxBlockableEnemies,
            RedeployTime = opData.stats.RedeployTime,
            SPRecoveryRate = opData.stats.SPRecoveryRate,
            StartSP = opData.stats.StartSP
        };
    }

    public OperatorStats GetOperatorStats() => currentStats;
    public List<Vector2Int> GetCurrentAttackableTiles() => currentAttackableTiles;

    // 레벨업 처리
    public bool LevelUp(int targetLevel)
    {
        if (targetLevel <= currentLevel) return false;
        if (targetLevel > OperatorGrowthSystem.GetMaxLevel(currentPhase)) return false;

        OperatorStats newStats = OperatorGrowthSystem.CalculateStats(this, targetLevel, currentPhase);

        // 실제 적용
        currentLevel = targetLevel;
        currentStats = newStats;
        currentExp = 0;

        return true; 
    }

    // 정예화 처리
    public bool Promote()
    {
        if (!OperatorGrowthSystem.CanPromote(this)) return false;

        OperatorStats newStats = OperatorGrowthSystem.CalculateStats(this, 1, OperatorGrowthSystem.ElitePhase.Elite1);

        currentPhase = OperatorGrowthSystem.ElitePhase.Elite1;
        currentLevel = 1;
        currentStats = newStats;
        currentExp = 0;

        ApplyElitePhaseUnlocks();

        return true;
    }

    /// <summary>
    /// 정예화 시에 해금되는 요소 적용
    /// </summary>
    public void ApplyElitePhaseUnlocks()
    {
        if (currentPhase >= OperatorGrowthSystem.ElitePhase.Elite1)
        {
            // 공격 범위 추가
            currentAttackableTiles.AddRange(BaseData.elite1Unlocks.additionalAttackTiles);

            // 새로운 스킬 추가
            if (BaseData.elite1Unlocks.unlockedSkill != null && !BaseData.skills.Contains(BaseData.elite1Unlocks.unlockedSkill))
            {
                BaseData.skills.Add(BaseData.elite1Unlocks.unlockedSkill);
            }
        }
    }

}