using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// ���� �ý����� �����ϴ� �Ŵ���.
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


    // ������ �õ� �޼���
    public bool TryLevelUpOperator(OwnedOperator op, int targetLevel, ExpCalculationSystem.ExpItemUsagePlan usagePlan)
    {
        // ������ ��� ���� ���� ����
        Dictionary<string, int> itemsToUse = usagePlan.itemsToUse.ToDictionary(pair => pair.Key.itemName!, pair => pair.Value);

        // ������ �Һ� �õ�
        if (GameManagement.Instance == null)
        {
            throw new InvalidOperationException("���� �Ŵ�����Ʈ �ν��Ͻ��� �ʱ�ȭ���� �ʾ���");
        }

        bool itemUseSuccess = GameManagement.Instance!.PlayerDataManager.UseItems(itemsToUse);
        if (!itemUseSuccess) return false;

        int remainingExp = usagePlan.remainingExp;

        // ������ �ݿ�
        op.LevelUP(targetLevel, remainingExp);

        // ���� �����۵� ���۷����Ϳ� �߰�
        op.AddUsedItem(usagePlan.itemsToUse);

        return true;
    }

    public bool TryPromoteOperator(OwnedOperator op)
    {
        // ����ȭ ������ ���� ���� �˻�
        if (!OperatorGrowthSystem.CanPromote(op)) return false;

        // ������ �Һ� �õ�
        if (GameManagement.Instance == null)
        {
            throw new InvalidOperationException("���� �Ŵ�����Ʈ �ν��Ͻ��� �ʱ�ȭ���� �ʾ���");
        }

        // ����ȭ�� �ʿ��� ������ �˻�: promotionItems �����͸� Dictionary(������ �̸�, ����)�� ��ȯ
        var itemsToUse = op.OperatorProgressData.promotionItems.ToDictionary(
            promotionItem => promotionItem.itemData.itemName,
            promotionItem => promotionItem.count);

        bool itemUseSuccess = GameManagement.Instance!.PlayerDataManager.UseItems(itemsToUse);
        if (!itemUseSuccess) return false;

        // ����ȭ ����
        op.Promote();

        // ���� ������ ����
        op.AddUsedItem(itemsToUse);

        GameManagement.Instance.PlayerDataManager.SavePlayerData();
        return true;
    }


    // ������ �������� �ʿ��� �������� ����մϴ�. UI �������.
    public ExpCalculationSystem.ExpItemUsagePlan CalculateRequiredItems (OwnedOperator op, int targetLevel)
    {
        if (GameManagement.Instance == null)
        {
            throw new InvalidOperationException("���� �Ŵ�����Ʈ �ν��Ͻ��� �ʱ�ȭ���� �ʾ���");
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

    // ��� ���۷������� ���� ���¸� �ʱ�ȭ�ϰ� ���� ��ȭ�� ȸ���մϴ�.
    public void ResetAllOperatorsGrowth()
    {
        if (GameManagement.Instance == null)
        {
            throw new InvalidOperationException("���� �Ŵ�����Ʈ �ν��Ͻ��� �ʱ�ȭ���� �ʾ���");
        }

        // 1. PlayerData���� ownedOperator�� ������
        var ownedOperators = GameManagement.Instance.PlayerDataManager.OwnedOperators;

        // 2. OwnedOperator�� ���鼭 usedItems�� �˻�, ���� �����۵鿡 ���� ItemsWithCount�� �׾ƿø�
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

        // 3. ���۷����͵��� ���� ���¸� 0����ȭ 1������ ����
        List<SquadOperatorInfo?> currentSquad = GameManagement.Instance!.PlayerDataManager.GetCurrentSquadWithNull();
        foreach (var op in ownedOperators)
        {
            op.currentPhase = OperatorGrowthSystem.ElitePhase.Elite0;
            op.currentLevel = 1;
            op.currentExp = 0;
            op.ClearUsedItems();
            op.Initialize();

            // ���� �����忡 �ش� ���۷����Ͱ� �ִٸ� ��ų�� 0������ ����
            int squadIndex = currentSquad.FindIndex(member => member.op.operatorName == op.operatorName);
            if (squadIndex != -1) // FindIndex�� �ش��ϴ� ���� ������ -1�� ��ȯ
            {
                GameManagement.Instance!.UserSquadManager.TryReplaceOperator(squadIndex, op, 0);
            }
        }

        // 5. 2������ ���� �������� ���� �κ��丮 ���·� ����
        GameManagement.Instance.PlayerDataManager.AddItems(refundItems);

        // ���� ����
        GameManagement.Instance.PlayerDataManager.SavePlayerData();
    }
}