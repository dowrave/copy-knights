using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class OperatorGrowthManager: MonoBehaviour
{
    public static OperatorGrowthManager Instance { get; private set; }

    // ���� ��Ȳ ���� ����, operatorName�� Key.
    //private Dictionary<string, OperatorProgress> operatorProgressData = new Dictionary<string, OperatorProgress>();

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
    /// <summary>
    /// ���۷����� ������ ����
    /// </summary>
    public bool TryLevelUpOperator(OwnedOperator op, int targetLevel)
    {
        // ������ �Ұ� ��Ȳ��
        if (op == null) return false;
        if (targetLevel <= op.currentLevel) return false;
        int maxLevel = OperatorGrowthSystem.GetMaxLevel(op.currentPhase);
        if (targetLevel > maxLevel) return false; // ����ȭ�� �ִ� ���� �ʰ� ��

        // ������ ����
        op.currentLevel = targetLevel;
        op.UpdateStats();

        GameManagement.Instance.PlayerDataManager.SavePlayerData();

        Debug.Log($"������ �� ���� �Ϸ�, {op.currentLevel}");
        return true;
    }

    /// <summary>
    /// ���۷����� ����ȭ ����
    /// </summary>
    public bool TryPromoteOperator(OwnedOperator op)
    {
        if (!OperatorGrowthSystem.CanPromote(op)) return false;

        // ����ȭ ����
        op.currentPhase = OperatorGrowthSystem.ElitePhase.Elite1;
        op.currentLevel = 1;
        op.UpdateStats(); // ����ȭ�� ���� ����, �ر� ��� ����

        GameManagement.Instance.PlayerDataManager.SavePlayerData();

        return true; 
    }


    public OperatorStats GetCurentStats(string operatorId)
    {
        // ���� ���� ��� ����
        return new OperatorStats();
    }

    // JSON ����ȭ�� ���� ���� Ŭ����
    [System.Serializable]
    private class OperatorProgressList
    {
        public List<OperatorProgress> progressList = new List<OperatorProgress>();
    }
}