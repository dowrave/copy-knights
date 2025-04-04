using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 실제 스쿼드 관리는 PlayerDataManager에서 이뤄진다.
/// 여기서는 스쿼드 UI 조작 로직에 집중함
/// </summary>
public class UserSquadManager : MonoBehaviour
{
    // 편집 상태 관리
    private int editingSlotIndex = -1;
    public int EditingSlotIndex => editingSlotIndex;
    public bool IsEditingSquad => editingSlotIndex != -1; // -1이 아니면 편집 중이니까 true

    public int? MaxSquadSize => GameManagement.Instance!.PlayerDataManager.GetMaxSquadSize();

    // 이벤트
    public event System.Action? OnSquadUpdated;


    private void OnEnable()
    {
        GameManagement.Instance!.PlayerDataManager.OnSquadUpdated += HandleSquadUpdated;
    }

    private void OnDisable()
    {
        GameManagement.Instance!.PlayerDataManager.OnSquadUpdated -= HandleSquadUpdated;
    }

    private void HandleSquadUpdated()
    {
        OnSquadUpdated?.Invoke();
    }

    // 스쿼드 편집 관련 메서드
    public void StartEditingSlot(int index)
    {
        if (index >= 0 && index < MaxSquadSize)
        {
            editingSlotIndex = index;
        }
    }

    public void ConfirmOperatorSelection(OwnedOperator selectedOperator)
    {
        if (IsEditingSquad)
        {
            TryReplaceOperator(editingSlotIndex, selectedOperator);
            editingSlotIndex = -1; // 편집 상태 초기화
        }
    }

    /// Squad의 Index에 오퍼레이터를 배치/대체 하려고 할 때 사용
    public bool TryReplaceOperator(int index, OwnedOperator? newOp = null)
    {
        return GameManagement.Instance!.PlayerDataManager.TryUpdateSquad(index, newOp?.operatorName ?? string.Empty); // operatorName이 null일 경우의 처리 추가
    }

    public void CancelOperatorSelection()
    {
        editingSlotIndex = -1;
    }


    public List<OwnedOperator> GetCurrentSquad()
    {
        return GameManagement.Instance!.PlayerDataManager.GetCurrentSquad();
    }

    /// <summary>
    /// null이 포함된 currentSquad 리스트를 반환합니다.
    /// </summary>
    public List<OwnedOperator?> GetCurrentSquadWithNull()
    {
        return GameManagement.Instance!.PlayerDataManager.GetCurrentSquadWithNull();
    }

    // OperatorData를 사용한 리스트가 필요할 경우
    public List<OperatorData> GetCurrentSquadData()
    {
        return GameManagement.Instance!.PlayerDataManager.GetCurrentSquadData();
    }
}
