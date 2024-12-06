using System.Collections.Generic;
using UnityEngine;

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
    public int selectedSkillIndex = 0;
    public int currentExp = 0;
    public OperatorStats currentStats;
    public List<Vector2Int> currentAttackableTiles;

    public bool CanLevelUp => OperatorGrowthSystem.CanLevelUp(currentLevel, currentPhase, currentExp);
    public bool CanPromote => OperatorGrowthSystem.CanPromote(currentLevel, currentPhase);

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

        // �ʱ� ���� ����
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

    // ������ ó��
    public bool LevelUp(int targetLevel)
    {
        if (targetLevel <= currentLevel) return false;
        if (targetLevel > OperatorGrowthSystem.GetMaxLevel(currentPhase)) return false;

        OperatorStats newStats = OperatorGrowthSystem.CalculateStats(this, targetLevel, currentPhase);

        // ���� ����
        currentLevel = targetLevel;
        currentStats = newStats;
        currentExp = 0;

        return true; 
    }

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
            if (BaseData.elite1Unlocks.unlockedSkill != null && !BaseData.skills.Contains(BaseData.elite1Unlocks.unlockedSkill))
            {
                BaseData.skills.Add(BaseData.elite1Unlocks.unlockedSkill);
            }
        }
    }

}