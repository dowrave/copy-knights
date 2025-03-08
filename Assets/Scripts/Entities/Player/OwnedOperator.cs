using System.Collections.Generic;
using UnityEngine;
using Skills.Base;


// ������ ������ ���۷������� ����
[System.Serializable]
public class OwnedOperator
{
    // ����Ǵ� �ٽ� ������ : ���� ��Ȳ�� ��Ÿ���� �ּ����� ������ ����
    public string operatorName; // OperatorData.entityName�� ����
    public int currentLevel = 1; 
    public OperatorGrowthSystem.ElitePhase currentPhase = OperatorGrowthSystem.ElitePhase.Elite0;
    public int currentExp = 0;

    // ��Ÿ�ӿ��� �����ϴ� ���� �ʵ��
    [System.NonSerialized] private OperatorStats currentStats;
    [System.NonSerialized] private List<Vector2Int> currentAttackableGridPos = new List<Vector2Int>();
    [System.NonSerialized] private BaseSkill defaultSelectedSkill = default!;
    [System.NonSerialized] private List<BaseSkill> unlockedSkills = new List<BaseSkill>();
    [System.NonSerialized] private OperatorData baseData = default!;
    [System.NonSerialized] private BaseSkill stageSelectedSkill = default!;

    // �б� ���� ������Ƽ
    public OperatorStats CurrentStats => currentStats;
    public List<BaseSkill> UnlockedSkills => unlockedSkills;
    public BaseSkill DefaultSelectedSkill => defaultSelectedSkill;
    public List<Vector2Int> CurrentAttackableGridPos => currentAttackableGridPos;
    public OperatorData OperatorProgressData
    {
        get
        {
            // ���� ���� ��
            if (baseData == null) 
            {
                // Lazy Loading���� �����ӿ��� �ʵ带 �Ҵ��ϴ� �� �� Ȯ���� �����
                baseData = GameManagement.Instance!.PlayerDataManager.GetOperatorData(operatorName);  // PlayerDataManager���� operatorID�� �ش��ϴ� OperatorData�� ������
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


    // ������
    // �߿�!) ����� �����͸� �ε��� ���� �����ڰ� ȣ����� �����Ƿ� Initialize�� ������ �����ؾ� �Ѵ�.
    public OwnedOperator(OperatorData opData)
    {
        operatorName = opData.entityName;
        currentLevel = 1;
        currentPhase = OperatorGrowthSystem.ElitePhase.Elite0;
        currentExp = 0;
        baseData = opData;

        Initialize();
    }

    // ����ȭ, ������ ���� ������ �ݿ���. ���� ���� / ����ȭ �ÿ� ����ȴ�.
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

    // ����ȭ ó��
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