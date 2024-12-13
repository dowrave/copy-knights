using System.Collections.Generic;
using UnityEngine;
using Skills.Base;

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
    public int currentExp = 0;
    public OperatorStats currentStats;
    public List<Vector2Int> currentAttackableTiles;

    public List<Skill> unlockedSkills = new List<Skill>();
    public Skill selectedSkill;
    public int selectedSkillIndex = 0;

    public bool CanLevelUp => OperatorGrowthSystem.CanLevelUp(currentPhase, currentLevel);
    public bool CanPromote => OperatorGrowthSystem.CanPromote(currentPhase, currentLevel);

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
        currentStats = opData.stats;

        unlockedSkills.Add(opData.elite0Skill);
    }

    public OperatorStats GetOperatorStats() => currentStats;
    public List<Vector2Int> GetCurrentAttackableTiles() => currentAttackableTiles;

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
            if (BaseData.elite1Unlocks.unlockedSkill != null && !unlockedSkills.Contains(BaseData.elite1Unlocks.unlockedSkill))
            {
                unlockedSkills.Add(BaseData.elite1Unlocks.unlockedSkill);
            }
        }
    }

}