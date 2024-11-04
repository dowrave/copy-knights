using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class OperatorSelectionPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform contentContainer;
    [SerializeField] private OperatorSlotButton slotButtonPrefab;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private ScrollRect scrollRect;

    private List<OperatorSlotButton> operatorSlots = new List<OperatorSlotButton>();
    private OperatorSlotButton selectedSlot;

    private void Awake()
    {
        confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        cancelButton.onClick.AddListener(OnCancelButtonClicked);
        confirmButton.interactable = false;
    }

    private void OnEnable()
    {
        PopulateOperators();
        ResetSelection();
    }


    /// <summary>
    /// 보유한 오퍼레이터 리스트를 만들고 오퍼레이터 슬롯들을 초기화합니다.
    /// </summary>
    private void PopulateOperators()
    {
        // 슬롯 정리
        ClearSlots();

        // 보유 중인 오퍼레이터 로드
        var ownedOperators = PlayerDataManager.Instance.GetOwnedOperators();

        // 오퍼레이터 별로 슬롯 생성
        foreach (var operatorData in ownedOperators)
        {
            OperatorSlotButton slot = Instantiate(slotButtonPrefab, contentContainer);
            slot.Initialize(true);
            slot.AssignOperator(operatorData);
            slot.OnSlotClicked.AddListener(HandleSlotClicked);
            operatorSlots.Add(slot);
        }
    }

    private void HandleSlotClicked(OperatorSlotButton clickedSlot)
    {
        if (selectedSlot != null) { selectedSlot.SetSelected(false); }

        selectedSlot = clickedSlot;
        selectedSlot.SetSelected(true);
        confirmButton.interactable = true;
    }
    
    /// <summary>
    /// 확인 버튼 클릭 시 동작
    /// </summary>
    private void OnConfirmButtonClicked()
    {
        if (selectedSlot != null && selectedSlot.AssignedOperator != null)
        {
            OperatorSlotButton currentSlot = MainMenuManager.Instance.CurrentEditingSlot;
            if (currentSlot != null)
            {
                currentSlot.AssignOperator(selectedSlot.AssignedOperator);
            }

            // 돌아가기
            MainMenuManager.Instance.ShowPanel(MainMenuManager.MenuPanel.SquadEdit);
        }
    }

    /// <summary>
    /// 취소 버튼 클릭 시 동작
    /// </summary>
    private void OnCancelButtonClicked()
    {
        MainMenuManager.Instance.ShowPanel(MainMenuManager.MenuPanel.SquadEdit);
    }

    private void ResetSelection()
    {
        if (selectedSlot != null)
        {
            selectedSlot.SetSelected(false);
            selectedSlot = null;
        }

        //confirmButton.interactable = false;
    }

    private void ClearSlots()
    {
        foreach (OperatorSlotButton slot in operatorSlots)
        {
            Destroy(slot.gameObject);
        }
        operatorSlots.Clear();
    }

    private void OnDisable()
    {
        ResetSelection();
    }
}
