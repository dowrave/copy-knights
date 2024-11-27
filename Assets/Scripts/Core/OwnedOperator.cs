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
        UpdateStats();
    }

    /// <summary>
    /// ������ �ݿ��� ���� ���� ���
    /// </summary>
    public void UpdateStats()
    {
        if (BaseData == null)
        {
            Debug.LogError($"{BaseData}�� �ʱ�ȭ���� ����");
            return;
        }

        OperatorStats baseStats = BaseData.stats;
        OperatorData.OperatorLevelStats levelUpStats = BaseData.levelStats;
        int levelDifference = currentLevel - 1;

        currentStats = new OperatorStats
        {
            AttackPower = baseStats.AttackPower + (levelUpStats.attackPowerPerLevel * levelDifference),
            Health = baseStats.Health + (levelUpStats.healthPerLevel * levelDifference),
            Defense = baseStats.Defense + (levelUpStats.defensePerLevel * levelDifference),
            MagicResistance = baseStats.MagicResistance + (levelUpStats.magicResistancePerLevel * levelDifference),
            AttackSpeed = baseStats.AttackSpeed,
            DeploymentCost = baseStats.DeploymentCost,
            MaxBlockableEnemies = baseStats.MaxBlockableEnemies,
            RedeployTime = baseStats.RedeployTime,
            SPRecoveryRate = baseStats.SPRecoveryRate,
            StartSP = baseStats.StartSP
        };
    }

    public OperatorStats GetOperatorStats() => currentStats;
    public List<Vector2Int> GetCurrentAttackableTiles() => currentAttackableTiles;

    /// <summary>
    /// ����ȭ �ÿ� �رݵǴ� ��� ����
    /// </summary>
    public void ApplyElitePhaseUnlocks()
    {
        if (currentPhase >= OperatorGrowthSystem.ElitePhase.Elite1)
        {
            // ���� ���� ����
            var newAttackTiles = BaseData.attackableTiles;
            newAttackTiles.AddRange(BaseData.elite1Unlocks.additionalAttackTiles);
            BaseData.attackableTiles = newAttackTiles;

            // ���ο� ��ų �߰�
            if (BaseData.elite1Unlocks.unlockedSkill != null && !BaseData.skills.Contains(BaseData.elite1Unlocks.unlockedSkill))
            {
                BaseData.skills.Add(BaseData.elite1Unlocks.unlockedSkill);
            }
        }
    }

}