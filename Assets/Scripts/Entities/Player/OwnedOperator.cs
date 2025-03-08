using System.Collections.Generic;
using UnityEngine;
using Skills.Base;


// 실제로 보유한 오퍼레이터의 저장
[System.Serializable]
public class OwnedOperator
{
    // 저장되는 핵심 데이터 : 진행 상황을 나타내는 최소한의 정보만 저장
    public string operatorName; // OperatorData.entityName과 동일
    public int currentLevel = 1; 
    public OperatorGrowthSystem.ElitePhase currentPhase = OperatorGrowthSystem.ElitePhase.Elite0;
    public int currentExp = 0;

    // 런타임에만 존재하는 계산된 필드들
    [System.NonSerialized] private OperatorStats currentStats;
    [System.NonSerialized] private List<Vector2Int> currentAttackableGridPos = new List<Vector2Int>();
    [System.NonSerialized] private BaseSkill defaultSelectedSkill = default!;
    [System.NonSerialized] private List<BaseSkill> unlockedSkills = new List<BaseSkill>();
    [System.NonSerialized] private OperatorData baseData = default!;
    [System.NonSerialized] private BaseSkill stageSelectedSkill = default!;

    // 읽기 전용 프로퍼티
    public OperatorStats CurrentStats => currentStats;
    public List<BaseSkill> UnlockedSkills => unlockedSkills;
    public BaseSkill DefaultSelectedSkill => defaultSelectedSkill;
    public List<Vector2Int> CurrentAttackableGridPos => currentAttackableGridPos;
    public OperatorData OperatorProgressData
    {
        get
        {
            // 최초 접근 시
            if (baseData == null) 
            {
                // Lazy Loading에서 게터임에도 필드를 할당하는 건 잘 확립된 방식임
                baseData = GameManagement.Instance!.PlayerDataManager.GetOperatorData(operatorName);  // PlayerDataManager에서 operatorID에 해당하는 OperatorData를 가져옴
            }
            return baseData;
        }
    }
    public BaseSkill StageSelectedSkill
    {
        get => stageSelectedSkill ?? defaultSelectedSkill;
    }

    public bool CanLevelUp => OperatorGrowthSystem.CanLevelUp(currentPhase, currentLevel);
    public bool CanPromote => OperatorGrowthSystem.CanPromote(currentPhase, currentLevel);


    // 생성자
    // 중요!) 저장된 데이터를 로드할 때는 생성자가 호출되지 않으므로 Initialize는 별도로 실행해야 한다.
    public OwnedOperator(OperatorData opData)
    {
        operatorName = opData.entityName;
        currentLevel = 1;
        currentPhase = OperatorGrowthSystem.ElitePhase.Elite0;
        currentExp = 0;
        baseData = opData;

        Initialize();
    }

    // 정예화, 레벨에 따른 사항을 반영함. 게임 실행 / 정예화 시에 실행된다.
    public void Initialize()
    {
        currentStats = OperatorGrowthSystem.CalculateStats(this, currentLevel, currentPhase);
        InitializeAttackRange();
        InitializeSkills();
    }

    public void SetDefaultSelectedSkills(BaseSkill skill)
    {
        defaultSelectedSkill = skill;
    }

    private void InitializeAttackRange()
    {
        currentAttackableGridPos = new List<Vector2Int>(OperatorProgressData.attackableTiles);

        if (currentPhase > OperatorGrowthSystem.ElitePhase.Elite0)
        {
            currentAttackableGridPos.AddRange(baseData.elite1Unlocks.additionalAttackTiles);
        }
    }

    private void InitializeSkills()
    {
        unlockedSkills = new List<BaseSkill> { baseData.elite0Skill };

        if (currentPhase > OperatorGrowthSystem.ElitePhase.Elite0 && 
            baseData.elite1Unlocks.unlockedSkill != null)
        {
            unlockedSkills.Add(baseData.elite1Unlocks.unlockedSkill);
        }

        defaultSelectedSkill = unlockedSkills[0];
    }

    public void LevelUP(int targetLevel, int remainingExp)
    {
        if (targetLevel <= currentLevel) return;
        if (targetLevel > OperatorGrowthSystem.GetMaxLevel(currentPhase)) return;

        currentLevel = targetLevel;
        currentExp = remainingExp;

        Initialize();
    }

    // 정예화 처리
    public void Promote()
    {
        if (!OperatorGrowthSystem.CanPromote(this)) return;

        currentPhase = OperatorGrowthSystem.ElitePhase.Elite1;
        currentLevel = 1;
        currentExp = 0;

        Initialize();
    }

    public void SetStageSelectedSkill(BaseSkill newSkill)
    {
        defaultSelectedSkill = newSkill;
    }
}