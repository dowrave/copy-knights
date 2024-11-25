using System.Collections.Generic;
using System.Numerics;
using UnityEngine;

// ���� ���۷������� ���� ���� ��Ȳ ������
[System.Serializable]
public class OperatorProgress
{
    public string operatorName; // OperatorData�� entityName�� ��Ī
    public OperatorGrowthSystem.ElitePhase currentPhase = OperatorGrowthSystem.ElitePhase.Elite0;
    public int currentLevel = 1;

    public bool CanLevelUp => currentLevel < OperatorGrowthSystem.GetMaxLevel(currentPhase);
    public bool CanPromote => currentLevel < (int)OperatorGrowthSystem.ElitePhase.Elite1 && // ����� �� ��ȯ �ʿ�
                                currentLevel >= OperatorGrowthSystem.GetMaxLevel(currentPhase);

    // ����ȭ �� �رݵǴ� ��� �����ϱ�
    public void ApplyElitePhaseChanges(OperatorData baseData)
    {
        if (currentPhase >= OperatorGrowthSystem.ElitePhase.Elite1)
        {
            // ���� ���� ����
            var newAttackTiles = new List<Vector2Int>(baseData.attackableTiles);
            newAttackTiles.AddRange(baseData.elite1Unlocks.additionalAttackTiles);
            baseData.attackableTiles = newAttackTiles;
        }

        // ���ο� ��ų �߰�
        if (baseData.elite1Unlocks.unlockedSkill != null && 
            !baseData.skills.Contains(baseData.elite1Unlocks.unlockedSkill))
        {
            baseData.skills.Add(baseData.elite1Unlocks.unlockedSkill);
        }
    } 
}
