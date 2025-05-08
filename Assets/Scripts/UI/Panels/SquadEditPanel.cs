using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;


public class SquadEditPanel : MonoBehaviour
{
    [Header("UI References")]
    //[SerializeField] private Transform operatorSlotsContainer = default!;
    [SerializeField] private List<OperatorSlot> operatorSlots = new List<OperatorSlot>();
    [SerializeField] private Button startButton = default!;

    private void Awake()
    {
        startButton.onClick.AddListener(HandleStartButtonClicked);
    }

    private void Start()
    {
        InitializePanel();
        UpdateSquadUI();
        UpdateEnterButtonState();
    }

    private void InitializePanel()
    {
       // 슬롯 초기화
       for (int i = 0; i < operatorSlots.Count; i++)
        {
            int slotIndex = i;  // 이런 식으로 복사해서 변수를 사용함 - i가 같이 움직일 위험이 있단다.
            OperatorSlot slot = operatorSlots[i];

            bool isActiveSlot = i < GameManagement.Instance!.UserSquadManager.MaxSquadSize;
            slot.Initialize(isActiveSlot);

            if (isActiveSlot)
            {
                slot.OnSlotClicked.AddListener((clickedSlot) => HandleSlotClicked(clickedSlot, slotIndex));
            }
        }
    }

    private void OnEnable()
    {
        InitializePanel();
        UpdateSquadUI();
    }


    private void UpdateSquadUI()
    {
        List<SquadOperatorInfo?> currentSquad = GameManagement.Instance!.PlayerDataManager.GetCurrentSquadWithNull();
        if (currentSquad == null) return; // 현재 스쿼드 정보가 없으면 실행 중단

        for (int i = 0; i < operatorSlots.Count; i++)
        {
            OperatorSlot slot = operatorSlots[i];
            bool isActiveSlot = i < GameManagement.Instance!.UserSquadManager.MaxSquadSize;

            if (isActiveSlot)
            {
                if (currentSquad[i] != null)
                {
                    // 오퍼레이터 할당 슬롯
                    slot.AssignOperator(currentSquad[i].op!);
                }
                else
                {
                    // 빈 슬롯
                    slot.InitializeEmptyOrDisabled(true);
                }
            }
            else
            {
                // 사용 불가능 슬롯
                slot.InitializeEmptyOrDisabled(false);
            }
        }

        UpdateEnterButtonState();
    }


    // OperatorSlot 버튼 클릭 시 OpeatorInventoryPanel로 넘어감
    private void HandleSlotClicked(OperatorSlot clickedSlot, int slotIndex)
    {
        // 현재 수정 중인 인덱스 설정
        GameManagement.Instance!.UserSquadManager.StartEditingSlot(slotIndex);
        //Debug.Break(); // 테스트 용, 클릭 시 일시 정지 -> 한 프레임씩 체크
        MainMenuManager.Instance!.FadeInAndHide(MainMenuManager.Instance!.PanelMap[MainMenuManager.MenuPanel.OperatorInventory], gameObject);
    }

    private void HandleStartButtonClicked()
    {
        // 어떤 스테이지인지는 MainMenuManager에서 관리 중
        MainMenuManager.Instance!.StartStage();
    }

    /// <summary>
    /// 활성화된 슬롯 중에서 오퍼레이터가 배치된 슬롯의 수
    /// </summary>
    private int GetDeployedOperatorCount()
    {
        int count = 0;
        for (int i = 0; i < GameManagement.Instance!.UserSquadManager.MaxSquadSize; i++)
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
        startButton.interactable = GetDeployedOperatorCount() > 0;
    }
}
