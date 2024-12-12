using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class ExpCalculationSystem
{
    public struct ExpItemUsagePlan
    {
        public Dictionary<ItemData, int> itemsToUse; // 사용할 아이템, 수량
        public int totalExp; // 획득할 총 경험치
        public int remainingExp; // 남는 경험치 <-- 필요한가?
    }

    /// <summary>
    /// 목표 경험치에 도달하기 위한 최적의 아이템 사용 계획
    /// 높은 경험치부터 우선적으로 사용함
    /// </summary>
    public static ExpItemUsagePlan CalculateOptimalItemUsage(
            OperatorGrowthSystem.ElitePhase phase,
            int currentLevel,
            int targetLevel,
            int currentExp,
            List<(ItemData item, int count)> availableItems)
    {

        // 필요 경험치 계산
        int requiredExp = OperatorGrowthSystem.GetTotalExpRequiredForLevel(phase, currentLevel, targetLevel, currentExp);

        ExpItemUsagePlan result = new ExpItemUsagePlan
        {
            itemsToUse = new Dictionary<ItemData, int>(),
            totalExp = 0,
            remainingExp = 0
        };

        // 경험치가 높은 아이템부터 정렬
        var sortedItems = availableItems
            .Where(x => x.item.type == ItemData.ItemType.Exp)
            .OrderByDescending(x => x.item.expAmount)
            .ToList();

        int remainingExpNeeded = requiredExp;

        foreach (var (item, availableCount) in sortedItems)
        {
            if (remainingExpNeeded <= 0) break;

            // 아이템으로 채울 수 있는 최대 갯수 계산
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
    /// 현재 보유한 아이템으로 도달할 수 있는 최대 레벨 계산
    /// </summary>
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
}
