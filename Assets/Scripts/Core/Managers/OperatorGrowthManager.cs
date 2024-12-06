using System.Collections.Generic;
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

    // 오퍼레이터 성장 메서드
    public bool TryLevelUpOperator(OwnedOperator op, int targetLevel)
    {
        // 레벨업 불가 상황들
        if (op == null) return false;
        if (targetLevel <= op.currentLevel) return false;
        int maxLevel = OperatorGrowthSystem.GetMaxLevel(op.currentPhase);
        if (targetLevel > maxLevel) return false; // 정예화별 최대 레벨 초과 시

        // 레벨업 진행
        op.LevelUp(targetLevel);

        GameManagement.Instance.PlayerDataManager.SavePlayerData();
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
}