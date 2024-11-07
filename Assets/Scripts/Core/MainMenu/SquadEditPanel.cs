using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;


public class SquadEditPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform operatorSlotsContainer;
    [SerializeField] private List<OperatorSlot> operatorSlots = new List<OperatorSlot>();
    [SerializeField] private Button enterStageButton;

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
            int slotIndex = i; // 클로저를 위해 로컬 변수로 복사
            OperatorSlot slot = operatorSlots[i];

            bool isActiveSlot = i < UserSquadManager.Instance.MaxSquadSize;
            slot.Initialize(isActiveSlot);

            if (isActiveSlot)
            {
                // 클로저를 사용하지 않고 i를 쓰면 루프가 끝난 후에는 모든 리스너가 마지막 i값을 참조하게 된다
                // 클로저를 쓰면 반복마다 생성된 새로운 변수 slotIndex를 각 리스너가 자신만의 인덱스로 사용하게 된다.
                slot.OnSlotClicked.AddListener((clickedSlot) => HandleSlotClicked(clickedSlot, slotIndex));
            }
        }

        UpdateSquadUI();
    }

    private void OnEnable()
    {
        UpdateSquadUI();
    }

    private void UpdateSquadUI()
    {
        List<OperatorData> currentSquad = UserSquadManager.Instance.GetCurrentSquad();

        for (int i = 0; i < operatorSlots.Count; i++)
        {
            OperatorSlot slot = operatorSlots[i];
            bool isActiveSlot = i < UserSquadManager.Instance.MaxSquadSize;

            if (isActiveSlot)
            {
                if (i < currentSquad.Count && currentSquad[i] != null)
                {
                    // 오퍼레이터 할당 슬롯
                    slot.AssignOperator(currentSquad[i]);
                }
                else
                {
                    // 빈 슬롯
                    slot.SetEmptyOrDisabled(true);
                }
            }
            else
            {
                // 사용 불가능 슬롯
                slot.SetEmptyOrDisabled(false);
            }
        }

        UpdateEnterButtonState();
    }

    /// <summary>
    /// OperatorSlot 버튼 클릭 시 실행됨(이벤트 구독)
    /// </summary>
    private void HandleSlotClicked(OperatorSlot clickedSlot, int slotIndex)
    {
        // 현재 수정 중인 인덱스 설정
        UserSquadManager.Instance.StartEditingSlot(slotIndex);

        // 패널 전환
        MainMenuManager.Instance.ShowPanel(MainMenuManager.MenuPanel.OperatorList);

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
        for (int i = 0; i < UserSquadManager.Instance.MaxSquadSize; i++)
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
