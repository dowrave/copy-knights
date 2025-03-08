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

        return true;
    }

    public bool TryPromoteOperator(OwnedOperator op)
    {
        if (!OperatorGrowthSystem.CanPromote(op)) return false;

        // ����ȭ ����
        op.Promote();

        if (GameManagement.Instance == null)
        {
            throw new InvalidOperationException("���� �Ŵ�����Ʈ �ν��Ͻ��� �ʱ�ȭ���� �ʾ���");
        }

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
}