using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;


public class SquadEditPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform operatorSlotsContainer;
    [SerializeField] private List<OperatorSlot> operatorSlots = new List<OperatorSlot>();
    [SerializeField] private Button startButton;
    [SerializeField] private Button backButton;

    private List<OperatorData> currentSquad = new List<OperatorData>();
    private GameObject stageSelectPanel;
    private GameObject operatorListPanel;

    private void Awake()
    {
        startButton.onClick.AddListener(HandleStartButtonClicked);
        backButton.onClick.AddListener(HandleBackButtonClicked);
    }

    private void Start()
    {
        InitializePanel();
        UpdateEnterButtonState();
    }

    private void InitializePanel()
    {
       // 슬롯 초기화
       for (int i = 0; i < operatorSlots.Count; i++)
        {
            int slotIndex = i; // 클로저를 위해 로컬 변수로 복사
            OperatorSlot slot = operatorSlots[i];

            bool isActiveSlot = i < GameManagement.Instance.UserSquadManager.MaxSquadSize;
            slot.Initialize(isActiveSlot);

            if (isActiveSlot)
            {
                // 클로저를 사용하지 않고 i를 쓰면 루프가 끝난 후에는 모든 리스너가 마지막 i값을 참조하게 된다
                // 클로저를 쓰면 반복마다 생성된 새로운 변수 slotIndex를 각 리스너가 자신만의 인덱스로 사용하게 된다.
                slot.OnSlotClicked.AddListener((clickedSlot) => HandleSlotClicked(clickedSlot, slotIndex));
            }
        }

        // 스쿼드를 가져와서 UI에 할당
        UpdateSquadUI();
    }

    private void OnEnable()
    {
        UpdateSquadUI();
        //UpdateVisuals();
    }

    private void UpdateSquadUI()
    {
        List<OperatorData> currentSquad = GameManagement.Instance.UserSquadManager.GetCurrentSquad();

        for (int i = 0; i < operatorSlots.Count; i++)
        {
            OperatorSlot slot = operatorSlots[i];
            bool isActiveSlot = i < GameManagement.Instance.UserSquadManager.MaxSquadSize;

            if (isActiveSlot)
            {
                if (currentSquad[i] != null)
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
    /// OperatorSlot 버튼 클릭 시 OpeatorListPanel로 넘어감
    /// </summary>
    private void HandleSlotClicked(OperatorSlot clickedSlot, int slotIndex)
    {
        // 현재 수정 중인 인덱스 설정
        GameManagement.Instance.UserSquadManager.StartEditingSlot(slotIndex);
        MainMenuManager.Instance.FadeInAndHide(MainMenuManager.Instance.PanelMap[MainMenuManager.MenuPanel.OperatorInventory], gameObject);
    }

    private void HandleStartButtonClicked()
    {
        // 어떤 스테이지인지는 MainMenuManager에서 관리 중
        MainMenuManager.Instance.StartStage();
    }

    private void HandleBackButtonClicked()
    {
        MainMenuManager.Instance.ActivateAndFadeOut(MainMenuManager.Instance.PanelMap[MainMenuManager.MenuPanel.StageSelect], gameObject);
    }

    /// <summary>
    /// 활성화된 슬롯 중에서 오퍼레이터가 배치된 슬롯의 수
    /// </summary>
    /// <returns></returns>
    private int GetDeployedOperatorCount()
    {
        int count = 0;
        for (int i = 0; i < GameManagement.Instance.UserSquadManager.MaxSquadSize; i++)
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
