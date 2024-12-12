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
        public int targetLevel;
        public int remainingExp; // ���� ����ġ <-- �ʿ��Ѱ�?
        public bool isTargetLevelReachable;
    }

    /// <summary>
    /// Ư�� ������ �����ϱ� ���� ������ ������ ������ ã���ϴ�.
    /// 1. ���� ���� ����ġ ���� �߻��ϴ� ������ ã���ϴ�.
    /// 2. ���� ����ġ ������, ���� ���� �������� ����ϴ� ������ �����մϴ�.
    /// </summary>
    public static ExpItemUsagePlan CalculateOptimalItemUsage(
            OperatorGrowthSystem.ElitePhase phase,
            int currentLevel,
            int targetLevel,
            int currentExp,
            List<(ItemData item, int count)> availableItems)
    {
        // �޼� ������ �ִ� ���� ���
        int maxReachableLevel = CalculateMaxReachableLevel(
            phase,
            currentLevel,
            currentExp,
            availableItems
        );

        // ��ǥ ���� > ���� �����̸� ��ȹ�� ������ ����
        if (targetLevel > maxReachableLevel)
        {
            return new ExpItemUsagePlan
            {
                itemsToUse = new Dictionary<ItemData, int>(),
                totalExp = 0,
                targetLevel = maxReachableLevel, // �޼� ������ �ִ� ������ ����
                remainingExp = currentExp,
                isTargetLevelReachable = false
            };
        }

        // �ʿ��� �� ����ġ ���
        int requiredExp = OperatorGrowthSystem.GetTotalExpRequiredForLevel(phase, currentLevel, targetLevel, currentExp);

        // DP�� ���� ĳ�� ���̺� - key : ����ġ / value : ����ġ�� �����ϱ� ���� ������ ��� ��ȹ (a ������ x��, b ������ y��..)

        // ��������� ������ ���� ã��
        ExpItemUsagePlan optimalPlan = FindOptimalCombination(
            requiredExp,
            availableItems.OrderBy(x => x.item.expAmount).ToList(),
            new Dictionary<int, ExpItemUsagePlan>()
        );

        // ����ġ �����÷ο� ó�� : �÷� ��� �� ���� ���� ����ġ���� �ִ� ����ġ���� �ʰ��� ���
        int remainingExp = (currentExp + optimalPlan.totalExp) - requiredExp;
        int finalLevel = targetLevel; 
        while (true)
        {
            int nextLevelExp = OperatorGrowthSystem.GetMaxExpForNextLevel(phase, finalLevel);
            if (remainingExp >= nextLevelExp &&
                finalLevel < OperatorGrowthSystem.GetMaxLevel(phase))
            {
                remainingExp -= nextLevelExp;
                finalLevel++;
            }
            else break;
        }

        optimalPlan.remainingExp = remainingExp;
        optimalPlan.targetLevel = finalLevel; 

        return optimalPlan;
    }

    /// <summary>
    /// ������ ������ dp�� ��ͽ����� ã���ϴ�
    /// </summary>
    private static ExpItemUsagePlan FindOptimalCombination(
        int targetExp,
        List<(ItemData item, int count)> items,
        Dictionary<int, ExpItemUsagePlan> dpTable
        )
    {
        // ���� ��� : �ʿ� ����ġ�� 0 ����
        if (targetExp <= 0)
        {
            return new ExpItemUsagePlan
            {
                itemsToUse = new Dictionary<ItemData, int>(),
                totalExp = 0
            };
        }

        // �̹� ���� ����� ����
        if (dpTable.ContainsKey(targetExp))
        {
            return dpTable[targetExp];
        }

        ExpItemUsagePlan bestPlan = new ExpItemUsagePlan
        {
            itemsToUse = new Dictionary<ItemData, int>(),
            totalExp = int.MaxValue,
            isTargetLevelReachable = true
        }; 

        foreach (var (item, count) in items)
        {
            if (count <= 0) continue;

            // ���� ������ ���
            var updateItems = items.Select(x =>
            x.item == item ?
            (x.item, x.count - 1) : x
            ).ToList();

            // ��������� ���� ����ġ�� ���� ������ ã��
            var subPlan = FindOptimalCombination(
                targetExp - item.expAmount,
                updateItems,
                dpTable
            );

            // ���� �������� ����ϴ� �� �� ȿ�������� ��
            int totalExp = subPlan.totalExp + item.expAmount;
            if (totalExp >= targetExp &&
                (bestPlan.totalExp == int.MaxValue ||  // ù �ظ� ã�Ұų�
                totalExp - targetExp < bestPlan.totalExp - targetExp)) // �� ���� ���� �߻��ϸ�
            {
                var newPlan = new ExpItemUsagePlan
                {
                    itemsToUse = new Dictionary<ItemData, int>(subPlan.itemsToUse),
                    totalExp = totalExp
                };

                if (!newPlan.itemsToUse.ContainsKey(item))
                {
                    newPlan.itemsToUse[item] = 0;
                }
                newPlan.itemsToUse[item]++;

                bestPlan = newPlan;
            }
        }

        dpTable[targetExp] = bestPlan;
        return bestPlan; 
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

    public static int CalculateMaxReachableLevel(
        OperatorGrowthSystem.ElitePhase phase,
        int currentLevel,
        int currentExp,
        List<(ItemData item, int count)> availableItems)
    {
        // ��� ������ �� ����ġ�� ���
        int totalAvailableExp = currentExp + availableItems.Sum(x => x.item.expAmount * x.count);

        int level = currentLevel;
        int remainingExp = totalAvailableExp; 

        // ����ġ�� �Ҹ��ϸ� ���ް����� �ִ뷹�� ���
        while (level < OperatorGrowthSystem.GetMaxLevel(phase))
        {
            int nextLevelExp = OperatorGrowthSystem.GetMaxExpForNextLevel(phase, level);
            if (remainingExp < nextLevelExp)
                break;

            remainingExp -= nextLevelExp;
            level++;
        }

        return level;
    }

}
