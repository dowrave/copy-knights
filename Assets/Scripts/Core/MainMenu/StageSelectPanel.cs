
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class StageSelectPanel : MonoBehaviour
{
    [SerializeField] List<StageButton> stageButtons;
    [SerializeField] private GameObject stageDetailPanel;
    [SerializeField] private TextMeshProUGUI stageTitleText;
    [SerializeField] private TextMeshProUGUI stageDetailText;
    [SerializeField] private Button confirmButton;

    private StageButton currentSelectedStageButton;

    private void Start()
    {
        InitializeButtons();
        InitializeDetailPanel();
    }

    private void InitializeButtons()
    {
        foreach (StageButton button in stageButtons)
        {
            button.onClick.AddListener(OnStageButtonClicked);
        }
    }

    private void InitializeDetailPanel()
    {
        stageDetailPanel.SetActive(false);
        confirmButton.onClick.AddListener(() => OnConfirmButtonClicked());
        confirmButton.interactable = false;
    }

    public StageData GetStageDataById(string stageId)
    {
        var targetButton = stageButtons.FirstOrDefault(btn => btn.StageData.stageId == stageId);

        if (targetButton != null)
        {
            OnStageButtonClicked(targetButton);
            ShowDetailPanel(targetButton.StageData);
            return targetButton.StageData;
        }

        return null;
    }

    /// <summary>
    /// 버튼 클릭시의 동작은 상위 오브젝트 Panel에서 관리함
    /// </summary>
    private void OnStageButtonClicked(StageButton clickedButton)
    {
        if (currentSelectedStageButton != null)
        {
            currentSelectedStageButton.SetSelected(false);
        }

        // 새로운 선택 처리
        currentSelectedStageButton = clickedButton;
        currentSelectedStageButton.SetSelected(true);

        // DetailPanel 업데이트
        ShowDetailPanel(currentSelectedStageButton.StageData);
    }

    public void ShowDetailPanel(StageData stageData)
    {
        if (stageDetailPanel != null)
        {
            stageDetailPanel.SetActive(true);
            stageTitleText.text = stageData.stageId;
            stageDetailText.text = stageData.stageDetail;
            confirmButton.interactable = true;
        }
    }


    private void OnConfirmButtonClicked()
    {
        if (currentSelectedStageButton != null)
        {
            MainMenuManager.Instance.SetSelectedStage(currentSelectedStageButton.StageData);

            GameObject squadEditPanel = MainMenuManager.Instance.PanelMap[MainMenuManager.MenuPanel.SquadEdit];
            GameObject stageSelectPanel = MainMenuManager.Instance.PanelMap[MainMenuManager.MenuPanel.StageSelect];

            // squadEditPanel을 보여주고 stageSelectPanel을 숨김
            MainMenuManager.Instance.FadeInAndHide(squadEditPanel, stageSelectPanel);
        }
    }

    /// <summary>
    /// 패널 비활성화 시 상태 초기화
    /// </summary>
    private void OnDisable()
    {
        if (currentSelectedStageButton != null)
        {
            currentSelectedStageButton.SetSelected(false);
            currentSelectedStageButton = null;
        }

        stageDetailPanel.SetActive(false);
        confirmButton.interactable = false;
    }
}
