
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using static OperatorGrowthSystem;

[CreateAssetMenu(fileName = "New Operator Level Data", menuName =  "Game/Operator Level Data")]
public class OperatorLevelData : ScriptableObject
{
    // 정예화별 최대 레벨
    private const int ELITE0_MAX_LEVEL = 50;
    private const int ELITE1_MAX_LEVEL = 60;

    // 레벨별 필요 경험치
    [Tooltip("1->2 레벨업에 필요한 경험치량.")]
    public int baseExpForLevelUp = 100;
    public int phase1ExpForLevelUp = 120;

    [Tooltip("레벨당 증가하는 요구 경험치량(등차)")]
    public float baseExpIncreasePerLevel = 17f;
    public float phase1ExpIncreasePerLevel = 20f;

    // 레벨 x에서 레벨업하기 위해 필요한 경험치량은 인덱스 x-1에 저장된다
    [HideInInspector]
    public List<int> baseLevelUpExpRequirements = new List<int>();
    public List<int> phase1LevelUpExpRequirements = new List<int>();

    private void OnValidate()
    {
        baseLevelUpExpRequirements.Clear();
        phase1LevelUpExpRequirements.Clear();

        // Elite0 레벨업 요구치 계산 (1->2부터 49->50까지)
        for (int level = 1; level <= ELITE0_MAX_LEVEL; level++)
        {
            float expRequired = baseExpForLevelUp + (baseExpIncreasePerLevel * (level - 1));
            baseLevelUpExpRequirements.Add(Mathf.RoundToInt(expRequired));
        }

        // Elite1 레벨업 요구치 계산 (1->2부터 59->60까지)
        for (int level = 1; level <= ELITE1_MAX_LEVEL; level++)
        {
            float expRequired = phase1ExpForLevelUp + (phase1ExpIncreasePerLevel * (level - 1));
            phase1LevelUpExpRequirements.Add(Mathf.RoundToInt(expRequired));
        }
    }

    // 특정 정예화/레벨의 경험치 요구량을 반환하는 헬퍼 메서드
    public int GetExpRequirement(ElitePhase phase, int level)
    {   
        // 최대 레벨이거나 잘못된 레벨이면 0 반환
        if (phase == ElitePhase.Elite0 && level >= ELITE0_MAX_LEVEL) return 0;
        if (phase == ElitePhase.Elite1 && level >= ELITE1_MAX_LEVEL) return 0;
        if (level < 1) return 0;

        int arrayIndex = level - 1;

        return phase switch
        {
            ElitePhase.Elite0 when arrayIndex < baseLevelUpExpRequirements.Count
                => baseLevelUpExpRequirements[arrayIndex],
            ElitePhase.Elite1 when arrayIndex < phase1LevelUpExpRequirements.Count
                => phase1LevelUpExpRequirements[arrayIndex],
            _ => 0
        };
    }

}
