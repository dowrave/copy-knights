using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class OperatorGrowthManager: MonoBehaviour
{
    public static OperatorGrowthManager Instance { get; private set; }

    // 진행 상황 저장 구조, operatorName이 Key.
    private Dictionary<string, OperatorProgress> operatorProgressData = new Dictionary<string, OperatorProgress>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            LoadProgressData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadProgressData()
    {
        string savedData = PlayerPrefs.GetString("OperatorProgress", "");
        if (!string.IsNullOrEmpty(savedData))
        {
            try
            {
                var progressList = JsonUtility.FromJson<OperatorProgressList>(savedData);
                operatorProgressData = progressList.progressList.ToDictionary(p => p.operatorName);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"오퍼레이터 진행 상황 로드 실패 : {e}");
            }
        }
    }

    private void SaveProgressData()
    {
        try
        {
            var progressList = new OperatorProgressList
            {
                progressList = operatorProgressData.Values.ToList()
            };
            string jsonData = JsonUtility.ToJson(progressList);
            PlayerPrefs.SetString("OperatorProgress", "");
            PlayerPrefs.Save();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"오퍼레이터 진행 상황 저장 실패 : {e}");
        }
    }

    // 오퍼레이터 성장 메서드
    public bool TryLevelUpOperator(string operatorName)
    {
        if (!operatorProgressData.TryGetValue(operatorName, out var progress))
        {
            progress = new OperatorProgress { operatorName = operatorName };
            operatorProgressData[operatorName] = progress; 
        }

        if (!progress.CanLevelUp) return false;

        progress.currentLevel++;
        SaveProgressData();
        return true;
    }

    public bool TryPromoteOperator(OwnedOperator op)
    {
        if (!operatorProgressData.TryGetValue(op.operatorName, out var progress)) return false;

        if (!progress.CanPromote) return false;

        progress.currentPhase = OperatorGrowthSystem.ElitePhase.Elite1;
        progress.currentLevel = 1;

        // 정예화에 따른 변경사항 적용
        var opData = GameManagement.Instance.PlayerDataManager.GetOperatorData(op.operatorName);
        if (opData != null)
        {
            progress.ApplyElitePhaseChanges(opData);
        }

        SaveProgressData();
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