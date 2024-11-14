using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 메인 메뉴 씬에서 오퍼레이터들이 편성을 관리하고,
/// 스테이지 씬으로 편성된 리스트를 전달한다.
/// </summary>
public class UserSquadManager : MonoBehaviour
{
    [Header("Squad Settings")]
    [SerializeField] private int maxSquadSize = 6;
    public int MaxSquadSize => maxSquadSize;

    // 현재 편성된 스쿼드
    private List<OperatorData> currentSquad = new List<OperatorData>();

    // 편집 상태 관리
    private int editingSlotIndex = -1;
    public int EditingSlotIndex => editingSlotIndex;
    public bool IsEditingSquad => editingSlotIndex != -1; 

    // 이벤트
    public event System.Action OnSquadUpdated;
    private void Awake()
    {
        InitializeSquad();
    }

    /// <summary>
    /// 스쿼드의 초기화를 담당. MaxSquadSize만큼의 null을 만든다.
    /// </summary>
    private void InitializeSquad()
    {
        currentSquad = new List<OperatorData>(MaxSquadSize);
        for (int i=0; i < MaxSquadSize; i++)
        {
            currentSquad.Add(null);
        }

        // 저장된 스쿼드 데이터가 있다면 로드함
        LoadSquadData();
    }
    

    private void LoadSquadData()
    {
        // PlayerPrefs에 저장된 스쿼드 데이터를 로드
        string savedData = PlayerPrefs.GetString("SquadData", "");
        if (!string.IsNullOrEmpty(savedData))
        {
            try
            {
                var savedSquad = JsonUtility.FromJson<SavedSquadData>(savedData);
                for (int i = 0; i < savedSquad.operatorIds.Count && i < maxSquadSize; i++)
                {
                    string operatorId = savedSquad.operatorIds[i];
                    if (!string.IsNullOrEmpty(operatorId))
                    {
                        currentSquad[i] = PlayerDataManager.Instance.GetOperatorDataFromDatabase(operatorId);
                    } 
                }
                OnSquadUpdated?.Invoke();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"스쿼드 로드 실패 : {e.Message}");
            }
        }
    }

    private void SaveSquadData()
    {
        try
        {
            // 스쿼드 데이터 = currentSquad에 있는 오퍼레이터들의 엔티티 이름을 리스트로 만듦
            var squadData = new SavedSquadData
            {
                operatorIds = currentSquad
                .Where(op => op != null)
                .Select(op => op.entityName)
                .ToList()
            };

            string jsonData = JsonUtility.ToJson(squadData);
            PlayerPrefs.SetString("SquadData", jsonData);
            PlayerPrefs.Save();
        }

        catch (System.Exception e)
        {
            Debug.LogError($"스쿼드 저장 실패 : {e.Message}");
        }
    }

    // 스쿼드 편집 관련 메서드
    public void StartEditingSlot(int index)
    {
        if (index >= 0 && index < MaxSquadSize)
        {
            editingSlotIndex = index;
        }
    }

    public void ConfirmOperatorSelection(OperatorData selectedOperator)
    {
        if (IsEditingSquad)
        {
            TryReplaceOperator(editingSlotIndex, selectedOperator);
            editingSlotIndex = -1; // 편집 상태 초기화
        }

        Debug.Log("오퍼레이터 배치 후 스쿼드 확인");
        for (int i = 0; i < currentSquad.Count(); i++)
        {
            Debug.Log($"{i}번째 오퍼레이터 : {currentSquad[i]}");
        }
    }

    /// <summary>
    /// Squad의 Index에 오퍼레이터를 배치/대체 하려고 할 때 사용
    /// </summary>
    public bool TryReplaceOperator(int index, OperatorData newOpData = null)
    {
        if (index < 0 || index >= MaxSquadSize)
        {
            Debug.LogWarning("유효하지 않은 인덱스");
            return false;
        }

        currentSquad[index] = newOpData;
        OnSquadUpdated?.Invoke();
        SaveSquadData();
        return true;
    }

    public void CancelOperatorSelection()
    {
        editingSlotIndex = -1;
    }

    public bool UpdateUserSquad(List<OperatorData> newSquad)
    {
        if (newSquad.Count > MaxSquadSize)
        {
            Debug.LogWarning("현재 스쿼드의 크기가 최대 크기를 넘음!");
            return false; 
        }

        if (newSquad.Count != newSquad.Distinct().Count())
        {
            Debug.LogWarning("이미 중복된 멤버가 있음!");
            return false;
        }

        currentSquad = new List<OperatorData>(newSquad); // 복사 후 사용으로 안전성 보장
        OnSquadUpdated?.Invoke();
        return true;
    }

    /// <summary>
    /// null이 포함된 currentSquad 리스트를 반환합니다.
    /// </summary>
    public List<OperatorData> GetCurrentSquad()
    {
        return new List<OperatorData>(currentSquad);
    }

    /// <summary>
    /// currentSquad에서 null인 것들은 제외하고 반환합니다.
    /// </summary>
    public List<OperatorData> GetActiveOperators()
    {
        return currentSquad.Where(op => op != null).ToList();
    }



    /// <summary>
    /// 편성에서 오퍼레이터 제거
    /// </summary>
    public bool TryRemoveOperator(OperatorData opData)
    {
        bool removed = currentSquad.Remove(opData);
        if (removed)
        {
            OnSquadUpdated?.Invoke();
        }
        return removed;
    }


    public void ClearSquad()
    {
        currentSquad.Clear();
        OnSquadUpdated?.Invoke();
    }

    // 저장용 데이터 클래스
    [System.Serializable]
    private class SavedSquadData
    {
        public List<string> operatorIds = new List<string>();
    }

}
