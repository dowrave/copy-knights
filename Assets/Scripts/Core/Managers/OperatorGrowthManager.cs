using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ���� �ý����� �����ϴ� �Ŵ���.
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

    // ���۷����� ���� �޼���
    public bool TryLevelUpOperator(OwnedOperator op, int targetLevel)
    {
        // ������ �Ұ� ��Ȳ��
        if (op == null) return false;
        if (targetLevel <= op.currentLevel) return false;
        int maxLevel = OperatorGrowthSystem.GetMaxLevel(op.currentPhase);
        if (targetLevel > maxLevel) return false; // ����ȭ�� �ִ� ���� �ʰ� ��

        // ������ ����
        op.LevelUp(targetLevel);

        GameManagement.Instance.PlayerDataManager.SavePlayerData();
        return true;
    }

    public bool TryPromoteOperator(OwnedOperator op)
    {
        if (!OperatorGrowthSystem.CanPromote(op)) return false;

        // ����ȭ ����
        op.Promote();

        GameManagement.Instance.PlayerDataManager.SavePlayerData();

        return true; 
    }
}