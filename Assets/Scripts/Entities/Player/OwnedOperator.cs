using System.Collections.Generic;
using UnityEngine;
using Skills.Base;

/// <summary>
/// ������ ������ ���۷����ʹ� �̷� ������ ����ȴ�.
/// </summary>
[System.Serializable]
public class OwnedOperator
{
    // ����ȭ�ؼ� �����ϴ� �κ� - PlayerPrefs�� �����
    public string operatorName; // OperatorData�� entityName�� ��Ī
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

    // ���� ���� �ÿ� �ε��ؼ� ����ȭ���� �ʴ� ����
    [System.NonSerialized]
    private OperatorData _baseData; 
    public OperatorData BaseData
    {
        get
        {
            // ���� ���� ��
            if (_baseData == null) 
            {
                // Lazy Loading���� �̷� ������ �����ӿ��� �ʵ带 �Ҵ��ϴ� �� �� Ȯ���� �����
                _baseData = GameManagement.Instance.PlayerDataManager.GetOperatorData(operatorName);  // PlayerDataManager���� operatorID�� �ش��ϴ� OperatorData�� ������
            }
            return _baseData;
        }
    }

    // ������
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

    // ����ȭ ó��
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
    /// ����ȭ �ÿ� �رݵǴ� ��� ����
    /// </summary>
    public void ApplyElitePhaseUnlocks()
    {
        if (currentPhase >= OperatorGrowthSystem.ElitePhase.Elite1)
        {
            // ���� ���� �߰�
            currentAttackableTiles.AddRange(BaseData.elite1Unlocks.additionalAttackTiles);

            // ���ο� ��ų �߰�
            if (BaseData.elite1Unlocks.unlockedSkill != null && !unlockedSkills.Contains(BaseData.elite1Unlocks.unlockedSkill))
            {
                unlockedSkills.Add(BaseData.elite1Unlocks.unlockedSkill);
            }
        }
    }

}