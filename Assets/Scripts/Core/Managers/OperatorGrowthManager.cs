using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

/// <summary>
/// 성장 시스템을 실행하는 매니저.
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
    /// 레벨업 진행 메서드
    /// </summary>
    public bool TryLevelUpOperator(OwnedOperator op, int targetLevel, ExpCalculationSystem.ExpItemUsagePlan usagePlan)
    {
        // 1. 아이템 사용 가능 여부 검증
        Dictionary<string, int> itemsToUse = usagePlan.itemsToUse.ToDictionary(pair => pair.Key.itemName, pair => pair.Value);

        // 2. 아이템 소비 시도
        bool itemUseSuccess = GameManagement.Instance.PlayerDataManager.UseItems(itemsToUse);
        if (!itemUseSuccess) return false;

        // 3. 경험치 효과 계산 및 레벨업 적용
        int totalExp = usagePlan.totalExp;
        op.currentExp += totalExp;

        // 4. 레벨업, 스탯 재계산
        op.currentLevel = targetLevel;
        op.currentStats = OperatorGrowthSystem.CalculateStats(op, targetLevel, op.currentPhase);

        return true;
    }

    public bool TryPromoteOperator(OwnedOperator op)
    {
        if (!OperatorGrowthSystem.CanPromote(op)) return false;

        // 정예화 진행
        op.Promote();

        GameManagement.Instance.PlayerDataManager.SavePlayerData();

        return true; 
    }

    /// <summary>
    /// 선택한 레벨까지 필요한 아이템을 계산합니다. UI 프리뷰용.
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