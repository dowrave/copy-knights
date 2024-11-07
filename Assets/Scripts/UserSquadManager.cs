using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 메인 메뉴 씬에서 오퍼레이터들이 편성을 관리,
/// 스테이지 씬으로 편성된 리스트를 전달한다.
/// </summary>
public class UserSquadManager : MonoBehaviour
{
    public static UserSquadManager Instance { get; private set; }

    private List<OperatorData> currentSquad = new List<OperatorData>();
    public event System.Action OnSquadUpdated;

    // 편집 상태 관리
    private int editingSlotIndex = -1;

    public int MaxSquadSize { get; } = 6;
    public bool IsEditingSquad => editingSlotIndex != -1; 

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 바뀌어도 파괴되지 않음
            InitializeSquad();
        }
        else
        {
            Destroy(gameObject);
        }
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
    }

    public void StartEditingSlot(int index)
    {
        if (index >= 0 && index < MaxSquadSize)
        {
            editingSlotIndex = index;
        }
    }

    /// <summary>
    /// 슬롯에 오퍼레이터를 최종 배치
    /// </summary>
    public void ConfirmOperatorSelection(OperatorData selectedOperator)
    {
        if (IsEditingSquad)
        {
            TryReplaceOperator(editingSlotIndex, selectedOperator);
            editingSlotIndex = -1; // 편집 상태 초기화
        }
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

    public List<OperatorData> GetCurrentSquad()
    {
        return new List<OperatorData>(currentSquad);
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

    public bool TryReplaceOperator(int index, OperatorData newOpData)
    {
        if (index < 0 || index >= MaxSquadSize)
        {
            Debug.LogWarning("유효하지 않은 인덱스");
            return false; 
        }

        currentSquad[index] = newOpData;
        OnSquadUpdated?.Invoke();
        return true; 
    }

    public void ClearSquad()
    {
        currentSquad.Clear();
        OnSquadUpdated?.Invoke();
    }
}
