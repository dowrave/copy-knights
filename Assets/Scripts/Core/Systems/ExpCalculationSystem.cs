using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class ExpCalculationSystem
{
    public struct ExpItemUsagePlan
    {
        public Dictionary<ItemData, int> itemsToUse; // ����� ������, ����
        public int totalExp; // ȹ���� �� ����ġ
        public int remainingExp; // ���� ����ġ <-- �ʿ��Ѱ�?
    }

    /// <summary>
    /// ��ǥ ����ġ�� �����ϱ� ���� ������ ������ ��� ��ȹ
    /// ���� ����ġ���� �켱������ �����
    /// </summary>
    public static ExpItemUsagePlan CalculateOptimalItemUsage(
            OperatorGrowthSystem.ElitePhase phase,
            int currentLevel,
            int targetLevel,
            int currentExp,
            List<(ItemData item, int count)> availableItems)
    {

        // �ʿ� ����ġ ���
        int requiredExp = OperatorGrowthSystem.GetTotalExpRequiredForLevel(phase, currentLevel, targetLevel, currentExp);

        ExpItemUsagePlan result = new ExpItemUsagePlan
        {
            itemsToUse = new Dictionary<ItemData, int>(),
            totalExp = 0,
            remainingExp = 0
        };

        // ����ġ�� ���� �����ۺ��� ����
        var sortedItems = availableItems
            .Where(x => x.item.type == ItemData.ItemType.Exp)
            .OrderByDescending(x => x.item.expAmount)
            .ToList();

        int remainingExpNeeded = requiredExp;

        foreach (var (item, availableCount) in sortedItems)
        {
            if (remainingExpNeeded <= 0) break;

            // ���������� ä�� �� �ִ� �ִ� ���� ���
            int neededCount = Mathf.CeilToInt(remainingExpNeeded / (float)item.expAmount);
            int actualCount = Mathf.Min(neededCount, availableCount);

            if (actualCount > 0)
            {
                result.itemsToUse[item] = actualCount;
                remainingExpNeeded -= actualCount * item.expAmount;
                result.totalExp += actualCount * item.expAmount;
            }
        }

        result.remainingExp = Mathf.Max(0, result.totalExp - requiredExp);
        return result;
    }

    /// <summary>
    /// ���� ������ ���������� ������ �� �ִ� �ִ� ���� ���
    /// </summary>
    public static (int level, ExpItemUsagePlan usagePlan) CalculateMaxLevel(
        OwnedOperator op, 
        List<(ItemData item, int count)> availableItems)
    {
        int maxLevelForPhase = OperatorGrowthSystem.GetMaxLevel(op.currentPhase);

        // ��� ��� ������ ����ġ�� ���
        int totalAvailableExp = availableItems
            .Where(i => i.item.type == ItemData.ItemType.Exp)
            .Sum(i => i.item.expAmount * i.count);
        totalAvailableExp += op.currentExp; // ���� ����ġ�� ����

        // ���� ������ ���� ���
        var (reachableLevel, remainingExp) = OperatorGrowthSystem.CalculateReachableLevel(op.currentPhase, op.currentLevel, totalAvailableExp);

        // ����� ������ ���
        var usagePlan = CalculateOptimalItemUsage(
            op.currentPhase,
            op.currentLevel,
            reachableLevel,
            op.currentExp,
            availableItems
            );

        return (reachableLevel, usagePlan);
    }
}
