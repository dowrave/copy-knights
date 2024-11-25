

using System.Collections.Generic;

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
                _baseData = PlayerDataManager.Instance.GetOperatorData(operatorName);  // PlayerDataManager���� operatorID�� �ش��ϴ� OperatorData�� ������
            }

            return _baseData;
        }
    }

    /// <summary>
    /// ������ �ݿ��� ���� ���� ���
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