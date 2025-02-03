
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class StageUIData
{
    public string stageId;
    public StageButton stageButton;
    public Image lineImage; 
}

public class StageSelectPanel : MonoBehaviour
{
    [SerializeField] private List<StageUIData> stageUIDataList;
    [SerializeField] private Button cancelArea; // buttonContainer가 이 역할을 함
    [SerializeField] private GameObject stageDetailPanel;
    [SerializeField] private TextMeshProUGUI stageTitleText;
    [SerializeField] private TextMeshProUGUI stageDetailText;
    [SerializeField] private Button confirmButton;

    public StageButton CurrentStageButton { get; private set; }
    private StageData selectedStage => MainMenuManager.Instance.SelectedStage;

    private PlayerDataManager playerDataManager;

    private void Start()
    {
        playerDataManager = GameManagement.Instance.PlayerDataManager;

        InitializeStageButtons();
        InitializeDetailPanel();
    }

    private void InitializeStageButtons()
    {
        
        foreach (StageUIData stageUIData in stageUIDataList)
        {
            // 버튼에 리스너 추가
            StageButton stageButton = stageUIData.stageButton;
            stageButton.onClick.AddListener(OnStageButtonClicked);

            bool isUnlocked = playerDataManager.IsStageUnlocked(stageUIData.stageId);
            stageUIData.stageButton.gameObject.SetActive(isUnlocked);


            // 1-1의 경우 라인 이미지가 없음
            if (stageUIData.lineImage != null)
            {
                stageUIData.lineImage.gameObject.SetActive(isUnlocked);
            }

            // 클리어된 스테이지
            if (playerDataManager.IsStageCleared(stageUIData.stageId))
            {
                var stageResultInfo = playerDataManager.GetStageResultInfo(stageUIData.stageId);
                stageUIData.stageButton.SetUpStar(stageResultInfo.stars);
            }
        }
    }

    private void InitializeDetailPanel()
    {
        if (selectedStage != null)
        {
            stageDetailPanel.SetActive(true);
            SetStageButtonById(selectedStage.stageId);
        }
        else
        {
            stageDetailPanel.SetActive(false);
        }

        confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        cancelArea.onClick.AddListener(OnCancelAreaClicked);

        InitializeDetailPanelButtons();
        cancelArea.interactable = false;
    }

    private void InitializeDetailPanelButtons()
    {
        confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        if (selectedStage != null)
        {
            confirmButton.interactable = true;
        }
        else
        {
            confirmButton.interactable = false;
        }
    }


    // stageId에 해당하는 버튼을 클릭 상태로 바꿈
    public void SetStageButtonById(string stageId)
    {
        StageButton targetButton = stageUIDataList.FirstOrDefault(stageUIData => stageUIData.stageId == stageId).stageButton;
        //StageButton targetButton = stageButtons.FirstOrDefault(btn => btn.StageData.stageId == stageId);

        if (targetButton != null)
        {
            OnStageButtonClicked(targetButton);
        }
    }

    public StageData GetStageDataFromStageButton(StageButton stageButton)
    {
        return stageButton.StageData;
    }


    // 버튼 클릭시의 동작은 상위 오브젝트 Panel에서 관리함
    private void OnStageButtonClicked(StageButton clickedButton)
    {
        if (CurrentStageButton != null)
        {
            CurrentStageButton.SetSelected(false);
        }

        // 새로운 선택 처리
        CurrentStageButton = clickedButton;
        CurrentStageButton.SetSelected(true);

        // DetailPanel 업데이트
        ShowDetailPanel(CurrentStageButton.StageData);
    }

    private void OnCancelAreaClicked()
    {
        if (CurrentStageButton != null)
        {
            CurrentStageButton.SetSelected(false);
            CurrentStageButton = null;
        }
        stageDetailPanel.SetActive(false);
        cancelArea.interactable = false;
    }

    private void ShowDetailPanel(StageData stageData)
    {
        if (stageDetailPanel != null)
        {
            stageDetailPanel.SetActive(true);
            cancelArea.interactable = true;
            stageTitleText.text = stageData.stageId;
            stageDetailText.text = stageData.stageDetail;
            confirmButton.interactable = true;
        }
    }


    private void OnConfirmButtonClicked()
    {
        if (CurrentStageButton != null)
        {
            MainMenuManager.Instance.SetSelectedStage(CurrentStageButton.StageData);

            GameObject squadEditPanel = MainMenuManager.Instance.PanelMap[MainMenuManager.MenuPanel.SquadEdit];
            GameObject stageSelectPanel = MainMenuManager.Instance.PanelMap[MainMenuManager.MenuPanel.StageSelect];

            // squadEditPanel을 보여주고 stageSelectPanel을 숨김
            MainMenuManager.Instance.FadeInAndHide(squadEditPanel, stageSelectPanel);
        }
    }


    // 패널 비활성화 시 상태 초기화
    private void OnDisable()
    {
        if (CurrentStageButton != null)
        {
            CurrentStageButton.SetSelected(false);
            CurrentStageButton = null;
        }

        stageDetailPanel.SetActive(false);
        cancelArea.interactable = false;
        confirmButton.interactable = false;
    }
}
