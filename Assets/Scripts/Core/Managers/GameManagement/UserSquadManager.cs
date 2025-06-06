using System;
using System.Collections.Generic;
using System.Linq;
using Skills.Base;
using UnityEngine;

/// <summary>
/// 실제 스쿼드 관리는 PlayerDataManager에서 이뤄진다.
/// 여기서는 스쿼드 UI 조작 로직에 집중함
/// </summary>
public class UserSquadManager : MonoBehaviour
{
    // 개별 인덱스 편집 상태 관리
    public int EditingSlotIndex { get; private set; }
    public bool IsEditingSlot => EditingSlotIndex != -1; // -1이 아니면 편집 중이니까 true

    // 단체 인덱스 편집 상태 관리
    public bool IsEditingBulk { get; private set; }

    public int MaxSquadSize => GameManagement.Instance!.PlayerDataManager.GetMaxSquadSize();

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
            SetIsEditingSlotIndex(index);
        }
    }

    public void ConfirmOperatorSelection(OwnedOperator selectedOperator, int skillIndex)
    {
        if (IsEditingSlot)
        {
            TryReplaceOperator(EditingSlotIndex, selectedOperator, skillIndex);
            ResetIsEditingSlotIndex();
            MainMenuManager.Instance!.SetCurrentExistingOperator(null); // 현재 편집 중인 오퍼레이터 정보 초기화
        }
    }

    /// 스쿼드에 오퍼레이터를 배치/대체 하려고 할 때 사용
    public bool TryReplaceOperator(int squadIndex, OwnedOperator? newOp = null, int skillIndex = 0)
    {
        // 해당 인덱스 초기화
        if (newOp == null)
        {
            skillIndex = -1;
            return GameManagement.Instance!.PlayerDataManager.TryUpdateSquad(squadIndex, string.Empty, skillIndex);
        }
        // 해당 인덱스에 오퍼레이터 지정
        else
        {
            if (skillIndex < 0 || skillIndex >= newOp.UnlockedSkills.Count)
            {
                Debug.LogError("잘못된 스킬 인덱스에 접근 중. 기본 인덱스인 0번으로 지정하는 것으로 변경");
                return GameManagement.Instance!.PlayerDataManager.TryUpdateSquad(squadIndex, newOp.operatorName, 0);
            }
            return GameManagement.Instance!.PlayerDataManager.TryUpdateSquad(squadIndex, newOp.operatorName, skillIndex);
        }
    }

    public void CancelOperatorSelection()
    {
        ResetIsEditingSlotIndex();
    }

    // 스쿼드를 초기화합니다.
    public void ClearSquad()
    {
        GameManagement.Instance!.PlayerDataManager.ClearSquad();
    }

    public List<SquadOperatorInfo> GetCurrentSquad()
    {
        return GameManagement.Instance!.PlayerDataManager.GetCurrentSquad();
    }

    /// <summary>
    /// null이 포함된 currentSquad 리스트를 반환합니다.
    /// </summary>
    public List<SquadOperatorInfo?> GetCurrentSquadWithNull()
    {
        return GameManagement.Instance!.PlayerDataManager.GetCurrentSquadWithNull();
    }

    // OperatorData를 사용한 리스트가 필요할 경우
    public List<OperatorData> GetCurrentSquadData()
    {
        return GameManagement.Instance!.PlayerDataManager.GetCurrentSquadData();
    }

    public bool UpdateFullSquad(List<SquadOperatorInfo> ownedOpInfoSquad)
    {
        return GameManagement.Instance!.PlayerDataManager.UpdateFullSquad(ownedOpInfoSquad);
    }

    // 현재 스쿼드에서 오퍼레이터에 설정된 스킬 인덱스를 반환합니다.
    public int GetCurrentSkillIndex(OwnedOperator op)
    {
        List<SquadOperatorInfo> currentSquad = GetCurrentSquad();
        SquadOperatorInfo targetOpInfo = currentSquad.FirstOrDefault(member => member.op == op);
        if (targetOpInfo != null) return targetOpInfo.skillIndex;

        return -1;
    }

    
    public void SetIsEditingSlotIndex(int slotIndex)
    {
        if (slotIndex == -1)
        {
            ResetIsEditingSlotIndex();
            return;
        }
        else
        {
            Debug.Log($"슬롯 인덱스 : {slotIndex}");
            EditingSlotIndex = slotIndex;

            // 반대되는 개념이 켜져 있으면 자동으로 해제
            if (IsEditingBulk)
            {
                SetIsEditingBulk(false); 
            }
        }
    }

    // IsEditingSlot도 함께 초기화됨
    public void ResetIsEditingSlotIndex()
    {
        EditingSlotIndex = -1;
    }

    public void SetIsEditingBulk(bool state)
    {
        IsEditingBulk = state;

        // 반대되는 개념이 켜져 있으면 자동으로 해제
        if (IsEditingSlot)
        {
            ResetIsEditingSlotIndex();
        }
    }
}

public class SquadOperatorInfo
{
    public OwnedOperator op;
    public int skillIndex;

    // 생성자
    public SquadOperatorInfo()
    {
        op = null;
        skillIndex = -1;
    }

    public SquadOperatorInfo(OwnedOperator op, int index)
    {
        this.op = op;
        skillIndex = index;
    }
}