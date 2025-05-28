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

        // 사용된 아이템들 오퍼레이터에 추가
        op.AddUsedItem(usagePlan.itemsToUse);

        return true;
    }

    public bool TryPromoteOperator(OwnedOperator op)
    {
        // 정예화 가능한 성장 상태 검사
        if (!OperatorGrowthSystem.CanPromote(op)) return false;

        // 아이템 소비 시도
        if (GameManagement.Instance == null)
        {
            throw new InvalidOperationException("게임 매니지먼트 인스턴스가 초기화되지 않았음");
        }

        // 정예화에 필요한 아이템 검사: promotionItems 데이터를 Dictionary(아이템 이름, 갯수)로 변환
        var itemsToUse = op.OperatorProgressData.promotionItems.ToDictionary(
            promotionItem => promotionItem.itemData.itemName,
            promotionItem => promotionItem.count);

        bool itemUseSuccess = GameManagement.Instance!.PlayerDataManager.UseItems(itemsToUse);
        if (!itemUseSuccess) return false;

        // 정예화 진행
        op.Promote();

        // 사용된 아이템 저장
        op.AddUsedItem(itemsToUse);

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

    // 모든 오퍼레이터의 성장 상태를 초기화하고 사용된 재화를 회수합니다.
    public void ResetAllOperatorsGrowth()
    {
        if (GameManagement.Instance == null)
        {
            throw new InvalidOperationException("게임 매니지먼트 인스턴스가 초기화되지 않았음");
        }

        // 1. PlayerData에서 ownedOperator을 가져옴
        var ownedOperators = GameManagement.Instance.PlayerDataManager.OwnedOperators;

        // 2. OwnedOperator를 돌면서 usedItems을 검사, 사용된 아이템들에 대한 ItemsWithCount를 쌓아올림
        Dictionary<ItemData, int> refundItems = new Dictionary<ItemData, int>();
        foreach (var op in ownedOperators)
        {
            var usedItems = op.GetUsedItemCount();
            foreach (var itemWithCount in usedItems)
            {
                if (refundItems.ContainsKey(itemWithCount.itemData))
                {
                    refundItems[itemWithCount.itemData] += itemWithCount.count;
                }
                else
                {
                    refundItems[itemWithCount.itemData] = itemWithCount.count;
                }
            }
        }

        // 3. 오퍼레이터들의 성장 상태를 0정예화 1레벨로 만듦
        List<SquadOperatorInfo?> currentSquad = GameManagement.Instance!.PlayerDataManager.GetCurrentSquadWithNull();
        foreach (var op in ownedOperators)
        {
            op.currentPhase = OperatorGrowthSystem.ElitePhase.Elite0;
            op.currentLevel = 1;
            op.currentExp = 0;
            op.ClearUsedItems();
            op.Initialize();

            // 현재 스쿼드에 해당 오퍼레이터가 있다면 스킬은 0번으로 설정
            int squadIndex = currentSquad.FindIndex(member => member.op.operatorName == op.operatorName);
            if (squadIndex != -1) // FindIndex는 해당하는 값이 없으면 -1을 반환
            {
                GameManagement.Instance!.UserSquadManager.TryReplaceOperator(squadIndex, op, 0);
            }
        }

        // 5. 2번에서 얻은 아이템을 현재 인벤토리 상태로 저장
        GameManagement.Instance.PlayerDataManager.AddItems(refundItems);

        // 최종 저장
        GameManagement.Instance.PlayerDataManager.SavePlayerData();
    }
}