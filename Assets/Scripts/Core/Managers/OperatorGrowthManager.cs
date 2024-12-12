using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

/// <summary>
/// ���� �ý����� �����ϴ� �Ŵ���.
/// </summary>
public class OperatorGrowthManager: MonoBehaviour
{
    public static OperatorGrowthManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// ������ ���� �޼���
    /// </summary>
    public bool TryLevelUpOperator(OwnedOperator op, int targetLevel, ExpCalculationSystem.ExpItemUsagePlan usagePlan)
    {
        // 1. ������ ��� ���� ���� ����
        Dictionary<string, int> itemsToUse = usagePlan.itemsToUse.ToDictionary(pair => pair.Key.itemName, pair => pair.Value);

        // 2. ������ �Һ� �õ�
        bool itemUseSuccess = GameManagement.Instance.PlayerDataManager.UseItems(itemsToUse);
        if (!itemUseSuccess) return false;

        // 3. ����ġ ȿ�� ��� �� ������ ����
        int totalExp = usagePlan.totalExp;
        op.currentExp += totalExp;

        // 4. ������, ���� ����
        op.currentLevel = targetLevel;
        op.currentStats = OperatorGrowthSystem.CalculateStats(op, targetLevel, op.currentPhase);

        return true;
    }

    public bool TryPromoteOperator(OwnedOperator op)
    {
        if (!OperatorGrowthSystem.CanPromote(op)) return false;

        // ����ȭ ����
        op.Promote();

        GameManagement.Instance.PlayerDataManager.SavePlayerData();

        return true; 
    }

    /// <summary>
    /// ������ �������� �ʿ��� �������� ����մϴ�. UI �������.
    /// </summary>
    public ExpCalculationSystem.ExpItemUsagePlan CalculateRequiredItems (OwnedOperator op, int targetLevel)
    {
        var availableItems = GameManagement.Instance.PlayerDataManager.GetAllItems()
            .Where(x => x.itemData.type == ItemData.ItemType.Exp)
            .ToList();

        return ExpCalculationSystem.CalculateOptimalItemUsage(
                op.currentPhase,
                op.currentLevel,
                targetLevel,
                op.currentExp,
                availableItems
            );
    }
}