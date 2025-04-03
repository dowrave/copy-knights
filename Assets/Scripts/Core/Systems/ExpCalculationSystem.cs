using System.Collections.Generic;
using System.Linq;

public static class ExpCalculationSystem
{
    public struct ExpItemUsagePlan
    {
        public Dictionary<ItemData, int> itemsToUse; // ����� ������, ����
        public int totalItemExp; // ���������� ȹ���� �� ����ġ
        public int targetLevel;
        public int remainingExp; // ���� ����ġ
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
                totalItemExp = 0,
                targetLevel = maxReachableLevel, // �޼� ������ �ִ� ������ ����
                remainingExp = currentExp,
                isTargetLevelReachable = false
            };
        }

        // ���� ������ ���� ����ġ����, ��ǥ ������ 0 ����ġ�� �����ϱ� ���� ����ġ��
        int requiredExpForExactLevel = OperatorGrowthSystem.GetTotalExpRequiredForLevel(phase, currentLevel, targetLevel, currentExp);

        // ��������� ������ ���� ã��
        ExpItemUsagePlan optimalPlan = FindOptimalCombination(
            requiredExpForExactLevel,
            availableItems.OrderBy(x => x.item.expAmount).ToList(),
            new Dictionary<int, ExpItemUsagePlan>() // DP�� ���� ĳ�� ���̺� - key : ����ġ / value : ����ġ�� �����ϱ� ���� ������ ��� ��ȹ (a ������ x��, b ������ y��..)
        );

        // ������ �� ���� ����ġ�� Ÿ�� ���� ���
        int remainingExp = optimalPlan.totalItemExp - requiredExpForExactLevel;
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

        // ���� ����ȭ�� �ִ� ������ �����ߴٸ� ���� ����ġ�� 0���� ����
        if (finalLevel == OperatorGrowthSystem.GetMaxLevel(phase))
        {
            optimalPlan.remainingExp = 0;
        }
        else
        {
            optimalPlan.remainingExp = remainingExp;
        }

        optimalPlan.targetLevel = finalLevel;

        return optimalPlan;
    }


    // ������ ������ dp�� ��ͽ����� ã��
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
                totalItemExp = 0
            };
        }

        // ��� ��� ����
        if (dpTable.ContainsKey(targetExp))
        {
            return dpTable[targetExp];
        }

        ExpItemUsagePlan bestPlan = new ExpItemUsagePlan
        {
            itemsToUse = new Dictionary<ItemData, int>(),
            totalItemExp = int.MaxValue,
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
            int totalExp = subPlan.totalItemExp + item.expAmount;

            if (totalExp >= targetExp &&
                (bestPlan.totalItemExp == int.MaxValue ||  // ù �ظ� ã�Ұų�
                totalExp - targetExp < bestPlan.totalItemExp - targetExp)) // �� ���� ���� �߻��ϸ�
            {
                var newPlan = new ExpItemUsagePlan
                {
                    itemsToUse = new Dictionary<ItemData, int>(subPlan.itemsToUse),
                    totalItemExp = totalExp
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


    // ���� ������ ���������� ������ �� �ִ� �ִ� ���� ���
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
