using System.Collections.Generic;
using System.Linq;

public static class ExpCalculationSystem
{
    public struct ExpItemUsagePlan
    {
        public Dictionary<ItemData, int> itemsToUse; // 사용할 아이템, 수량
        public int totalItemExp; // 아이템으로 획득할 총 경험치
        public int targetLevel;
        public int remainingExp; // 남는 경험치
        public bool isTargetLevelReachable;
    }

    /// <summary>
    /// 특정 레벨에 도달하기 위한 최적의 아이템 조합을 찾습니다.
    /// 1. 가장 적은 경험치 낭비가 발생하는 조합을 찾습니다.
    /// 2. 같은 경험치 낭비라면, 적은 수의 아이템을 사용하는 조합을 선택합니다.
    /// </summary>
    public static ExpItemUsagePlan CalculateOptimalItemUsage(
            OperatorGrowthSystem.ElitePhase phase,
            int currentLevel,
            int targetLevel,
            int currentExp,
            List<(ItemData item, int count)> availableItems)
    {

        // 달성 가능한 최대 레벨 계산
        int maxReachableLevel = CalculateMaxReachableLevel(
            phase,
            currentLevel,
            currentExp,
            availableItems
        );

        // 목표 레벨 > 가능 레벨이면 계획을 세우지 않음
        if (targetLevel > maxReachableLevel)
        {
            return new ExpItemUsagePlan
            {
                itemsToUse = new Dictionary<ItemData, int>(),
                totalItemExp = 0,
                targetLevel = maxReachableLevel, // 달성 가능한 최대 레벨만 저장
                remainingExp = currentExp,
                isTargetLevelReachable = false
            };
        }

        // 현재 레벨과 현재 경험치에서, 목표 레벨의 0 경험치에 도달하기 위한 경험치량
        int requiredExpForExactLevel = OperatorGrowthSystem.GetTotalExpRequiredForLevel(phase, currentLevel, targetLevel, currentExp);

        // 재귀적으로 최적의 조합 찾기
        ExpItemUsagePlan optimalPlan = FindOptimalCombination(
            requiredExpForExactLevel,
            availableItems.OrderBy(x => x.item.expAmount).ToList(),
            new Dictionary<int, ExpItemUsagePlan>() // DP를 위한 캐시 테이블 - key : 경험치 / value : 경험치에 도달하기 위한 아이템 사용 계획 (a 아이템 x개, b 아이템 y개..)
        );

        // 레벨업 후 남는 경험치와 타겟 레벨 계산
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

        // 현재 정예화의 최대 레벨에 도달했다면 현재 경험치는 0으로 설정
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


    // 최적의 조합을 dp와 재귀식으로 찾음
    private static ExpItemUsagePlan FindOptimalCombination(
        int targetExp,
        List<(ItemData item, int count)> items,
        Dictionary<int, ExpItemUsagePlan> dpTable
        )
    {
        // 기저 사례 : 필요 경험치가 0 이하
        if (targetExp <= 0)
        {
            return new ExpItemUsagePlan
            {
                itemsToUse = new Dictionary<ItemData, int>(),
                totalItemExp = 0
            };
        }

        // 계산 결과 재사용
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

            // 현재 아이템 사용
            var updateItems = items.Select(x =>
            x.item == item ?
            (x.item, x.count - 1) : x
            ).ToList();

            // 재귀적으로 남은 경험치에 대한 최적해 찾기
            var subPlan = FindOptimalCombination(
                targetExp - item.expAmount,
                updateItems,
                dpTable
            );

            // 현재 아이템을 사용하는 게 더 효율적인지 평가
            int totalExp = subPlan.totalItemExp + item.expAmount;

            if (totalExp >= targetExp &&
                (bestPlan.totalItemExp == int.MaxValue ||  // 첫 해를 찾았거나
                totalExp - targetExp < bestPlan.totalItemExp - targetExp)) // 더 적은 낭비가 발생하면
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


    // 현재 보유한 아이템으로 도달할 수 있는 최대 레벨 계산
    public static (int level, ExpItemUsagePlan usagePlan) CalculateMaxLevel(
        OwnedOperator op, 
        List<(ItemData item, int count)> availableItems)
    {
        int maxLevelForPhase = OperatorGrowthSystem.GetMaxLevel(op.currentPhase);

        // 모든 사용 가능한 경험치를 계산
        int totalAvailableExp = availableItems
            .Where(i => i.item.type == ItemData.ItemType.Exp)
            .Sum(i => i.item.expAmount * i.count);
        totalAvailableExp += op.currentExp; // 현재 경험치도 포함

        // 도달 가능한 레벨 계산
        var (reachableLevel, remainingExp) = OperatorGrowthSystem.CalculateReachableLevel(op.currentPhase, op.currentLevel, totalAvailableExp);

        // 사용할 아이템 계산
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
        // 사용 가능한 총 경험치량 계산
        int totalAvailableExp = currentExp + availableItems.Sum(x => x.item.expAmount * x.count);

        int level = currentLevel;
        int remainingExp = totalAvailableExp; 

        // 경험치를 소모하며 도달가능한 최대레벨 계산
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
