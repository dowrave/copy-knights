
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using static OperatorGrowthSystem;

[CreateAssetMenu(fileName = "New Operator Level Data", menuName =  "Game/Operator Level Data")]
public class OperatorLevelData : ScriptableObject
{
    // ����ȭ�� �ִ� ����
    private const int ELITE0_MAX_LEVEL = 50;
    private const int ELITE1_MAX_LEVEL = 60;

    // ������ �ʿ� ����ġ
    [Tooltip("1->2 �������� �ʿ��� ����ġ��.")]
    public int baseExpForLevelUp = 100;
    public int phase1ExpForLevelUp = 120;

    [Tooltip("������ �����ϴ� �䱸 ����ġ��(����)")]
    public float baseExpIncreasePerLevel = 17f;
    public float phase1ExpIncreasePerLevel = 20f;

    // ���� x���� �������ϱ� ���� �ʿ��� ����ġ���� �ε��� x-1�� ����ȴ�
    [HideInInspector]
    public List<int> baseLevelUpExpRequirements = new List<int>();
    public List<int> phase1LevelUpExpRequirements = new List<int>();

    private void OnValidate()
    {
        baseLevelUpExpRequirements.Clear();
        phase1LevelUpExpRequirements.Clear();

        // Elite0 ������ �䱸ġ ��� (1->2���� 49->50����)
        for (int level = 1; level <= ELITE0_MAX_LEVEL; level++)
        {
            float expRequired = baseExpForLevelUp + (baseExpIncreasePerLevel * (level - 1));
            baseLevelUpExpRequirements.Add(Mathf.RoundToInt(expRequired));
        }

        // Elite1 ������ �䱸ġ ��� (1->2���� 59->60����)
        for (int level = 1; level <= ELITE1_MAX_LEVEL; level++)
        {
            float expRequired = phase1ExpForLevelUp + (phase1ExpIncreasePerLevel * (level - 1));
            phase1LevelUpExpRequirements.Add(Mathf.RoundToInt(expRequired));
        }
    }

    // Ư�� ����ȭ/������ ����ġ �䱸���� ��ȯ�ϴ� ���� �޼���
    public int GetExpRequirement(ElitePhase phase, int level)
    {   
        // �ִ� �����̰ų� �߸��� �����̸� 0 ��ȯ
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
