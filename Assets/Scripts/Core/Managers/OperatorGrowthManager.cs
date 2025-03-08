using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 성장 시스템을 실행하는 매니저.
public class OperatorGrowthManager: MonoBehaviour
{
    public static OperatorGrowthManager? Instance { get; private set; }

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


    // 레벨업 시도 메서드
    public bool TryLevelUpOperator(OwnedOperator op, int targetLevel, ExpCalculationSystem.ExpItemUsagePlan usagePlan)
    {
        // 아이템 사용 가능 여부 검증
        Dictionary<string, int> itemsToUse = usagePlan.itemsToUse.ToDictionary(pair => pair.Key.itemName!, pair => pair.Value);

        // 아이템 소비 시도
        if (GameManagement.Instance == null)
        {
            throw new InvalidOperationException("게임 매니지먼트 인스턴스가 초기화되지 않았음");
        }

        bool itemUseSuccess = GameManagement.Instance!.PlayerDataManager.UseItems(itemsToUse);
        if (!itemUseSuccess) return false;

        int remainingExp = usagePlan.remainingExp;

        // 레벨업 반영
        op.LevelUP(targetLevel, remainingExp);

        return true;
    }

    public bool TryPromoteOperator(OwnedOperator op)
    {
        if (!OperatorGrowthSystem.CanPromote(op)) return false;

        // 정예화 진행
        op.Promote();

        if (GameManagement.Instance == null)
        {
            throw new InvalidOperationException("게임 매니지먼트 인스턴스가 초기화되지 않았음");
        }

        GameManagement.Instance.PlayerDataManager.SavePlayerData();

        return true; 
    }


    // 선택한 레벨까지 필요한 아이템을 계산합니다. UI 프리뷰용.
    public ExpCalculationSystem.ExpItemUsagePlan CalculateRequiredItems (OwnedOperator op, int targetLevel)
    {
        if (GameManagement.Instance == null)
        {
            throw new InvalidOperationException("게임 매니지먼트 인스턴스가 초기화되지 않았음");
        }

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