// ���� �ý����� �⺻ ��Ģ ����
using UnityEngine;

public static class OperatorGrowthSystem
{
    public enum ElitePhase
    {
        Elite0 = 0,  // �ʱ� ����
        Elite1 = 1   // 1�� ����ȭ
    }

    public static readonly int Elite0MaxLevel = 50;
    public static readonly int Elite1MaxLevel = 60;

    // ����ġ ���� ����� ���������� ����
    private const int BASE_EXP_FOR_LEVEL_UP = 100; // ���۷����� 1->2������ ���� ���� �ʿ��� ����ġ�� ��
    private const float EXP_INCREASE_PER_LEVEL = 17f; 

    
    /// <summary>
    /// �ش� ����ȭ �ܰ��� �ִ� ���� ��ȯ
    /// </summary>
    public static int GetMaxLevel(ElitePhase phase)
    {
        // ���� swtich (phase) {case Elite0: return Elite0MaxLevel ...} ���� ����������
        // �̰Ŵ� C#�� ���� ��Ī�� ����� switch ǥ������
        return phase switch
        {
            ElitePhase.Elite0 => Elite0MaxLevel,
            ElitePhase.Elite1 => Elite1MaxLevel,
            _ => 1
        };
    }

    /// <summary>
    /// ���� �������� ���� ���� �ʿ��� ����ġ �� ���
    /// </summary>
    public static int GetRequiredExp(int currentLevel)
    {
        if (currentLevel <= 1) return 0;
        return Mathf.RoundToInt(BASE_EXP_FOR_LEVEL_UP + (EXP_INCREASE_PER_LEVEL * (currentLevel - 2)));
    }

    public static bool CanLevelUp(int currentLevel, ElitePhase currentPhase, int currentExp)
    {
        int maxLevel = GetMaxLevel(currentPhase);
        if (currentLevel >= maxLevel) return false;
        return currentExp >= GetRequiredExp(currentLevel + 1);
    }

    public static bool CanPromote(int currentLevel, ElitePhase currentPhase)
    {
        return currentPhase == ElitePhase.Elite0 &&
            currentLevel >= GetMaxLevel(currentPhase);
    }

    /// <summary>
    /// �����÷ο� ���� �����ϰ� ����ġ ȹ��
    /// </summary>
    public static int GetSafeExpAmount(int currentExp, int expToAdd, int currentLevel, ElitePhase currentPhase)
    {
        int maxLevel = GetMaxLevel(currentPhase);
        if (currentLevel >= maxLevel) return currentExp; // �ִ� ���������� ����ġ ȹ�� �Ұ���

        int requiredExpForMaxLevel = GetRequiredExp(maxLevel);
        return Mathf.Min(currentExp + expToAdd, requiredExpForMaxLevel); 
    }
}
