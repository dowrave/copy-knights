using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class OperatorGrowthManager: MonoBehaviour
{
    public static OperatorGrowthManager Instance { get; private set; }

    // 진행 상황 저장 구조, operatorName이 Key.
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

    // 오퍼레이터 성장 메서드
    /// <summary>
    /// 오퍼레이터 레벨업 로직
    /// </summary>
    public bool TryLevelUpOperator(OwnedOperator op, int targetLevel)
    {
        // 레벨업 불가 상황들
        if (op == null) return false;
        if (targetLevel <= op.currentLevel) return false;
        int maxLevel = OperatorGrowthSystem.GetMaxLevel(op.currentPhase);
        if (targetLevel > maxLevel) return false; // 정예화별 최대 레벨 초과 시

        // 레벨업 진행
        op.currentLevel = targetLevel;
        op.UpdateStats();

        GameManagement.Instance.PlayerDataManager.SavePlayerData();

        Debug.Log($"레벨업 및 저장 완료, {op.currentLevel}");
        return true;
    }

    /// <summary>
    /// 오퍼레이터 정예화 로직
    /// </summary>
    public bool TryPromoteOperator(OwnedOperator op)
    {
        if (!OperatorGrowthSystem.CanPromote(op)) return false;

        // 정예화 진행
        op.currentPhase = OperatorGrowthSystem.ElitePhase.Elite1;
        op.currentLevel = 1;
        op.UpdateStats(); // 정예화에 따른 스탯, 해금 요소 적용

        GameManagement.Instance.PlayerDataManager.SavePlayerData();

        return true; 
    }


    public OperatorStats GetCurentStats(string operatorId)
    {
        // 현재 스탯 계산 로직
        return new OperatorStats();
    }

    // JSON 직렬화를 위한 래퍼 클래스
    [System.Serializable]
    private class OperatorProgressList
    {
        public List<OperatorProgress> progressList = new List<OperatorProgress>();
    }
}