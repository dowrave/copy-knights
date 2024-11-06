using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;


public class SquadEditPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform operatorSlotsContainer;
    [SerializeField] private List<OperatorSlotButton> operatorSlots = new List<OperatorSlotButton>();
    [SerializeField] private Button enterStageButton;
    [SerializeField] private OperatorListPanel operatorListPanel;

    // 나중에 스테이지에서 정보를 받아와도 되겠다.
    private const int ACTIVE_OPERATOR_SLOTS = 6;

    // private OperatorSlotButton selectedSlot; // SquadEditPanel에선 필요 없을 듯 - 슬롯을 클릭하면 바로 패널이 전환되기 때문에
    private List<OperatorData> currentSquad = new List<OperatorData>();

    private void Start()
    {
        InitializePanel();
        UpdateEnterButtonState();
    }

    private void InitializePanel()
    {
       // 슬롯별 타입 설정
       for (int i = 0; i < operatorSlots.Count; i++)
        {
            OperatorSlotButton slot = operatorSlots[i];
            
            // 1. 활성화
            if (i < ACTIVE_OPERATOR_SLOTS)
            {
                slot.Initialize(true);

                slot.OnSlotClicked.AddListener(HandleSlotClicked); // 파라미터는 Invoke에 들어가는 값을 따라감

                // 이렇게 작성하면 기능은 동일한데 필요없는 래퍼 함수만 하나 더 생김
                //slot.OnSlotClicked.AddListener((slot) => HandleSlotClicked(slot));
            }
            else
            {
                slot.Initialize(false);
            }
        }
    }

    /// <summary>
    /// OperatorSlot 버튼 클릭 시 실행됨(이벤트 구독)
    /// </summary>
    private void HandleSlotClicked(OperatorSlotButton clickedSlot)
    {
        MainMenuManager.Instance.StartOperatorSelection(clickedSlot);
    }

    private void HandleStartButtonClicked()
    {

    }

    /// <summary>
    /// 활성화된 슬롯 중에서 오퍼레이터가 배치된 슬롯의 수
    /// </summary>
    /// <returns></returns>
    private int GetDeployedOperatorCount()
    {
        int count = 0;
        for (int i = 0; i < ACTIVE_OPERATOR_SLOTS; i++)
        {
            if (!operatorSlots[i].IsEmpty())
            {
                count++;
            }
        }
        return count;
    }

    private void UpdateEnterButtonState()
    {
        enterStageButton.interactable = GetDeployedOperatorCount() > 0;
    }
}
