using System.Collections.Generic;
using System.Numerics;
using UnityEngine;

// 개별 오퍼레이터의 성장 진행 상황 데이터
[System.Serializable]
public class OperatorProgress
{
    public string operatorName; // OperatorData의 entityName과 매칭
    public OperatorGrowthSystem.ElitePhase currentPhase = OperatorGrowthSystem.ElitePhase.Elite0;
    public int currentLevel = 1;

    public bool CanLevelUp => currentLevel < OperatorGrowthSystem.GetMaxLevel(currentPhase);
    public bool CanPromote => currentLevel < (int)OperatorGrowthSystem.ElitePhase.Elite1 && // 명시적 형 변환 필요
                                currentLevel >= OperatorGrowthSystem.GetMaxLevel(currentPhase);

    // 정예화 시 해금되는 요소 적용하기
    public void ApplyElitePhaseChanges(OperatorData baseData)
    {
        if (currentPhase >= OperatorGrowthSystem.ElitePhase.Elite1)
        {
            // 공격 범위 변경
            var newAttackTiles = new List<Vector2Int>(baseData.attackableTiles);
            newAttackTiles.AddRange(baseData.elite1Unlocks.additionalAttackTiles);
            baseData.attackableTiles = newAttackTiles;
        }

        // 새로운 스킬 추가
        if (baseData.elite1Unlocks.unlockedSkill != null && 
            !baseData.skills.Contains(baseData.elite1Unlocks.unlockedSkill))
        {
            baseData.skills.Add(baseData.elite1Unlocks.unlockedSkill);
        }
    } 
}
