using System;
using System.Collections.Generic;
using System.Xml;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 씬 전환 시에도 유지되어야 하는 전역 매니저를 관리하는 클래스
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager? Instance { get; private set; }

    // 여러 패널들을 열거형으로 관리
    public enum MenuPanel
    {
        None,
        StageSelect,
        SquadEdit,
        OperatorInventory,
        OperatorDetail,
        OperatorLevelUp,
        OperatorPromotion,
        ItemInventory,
        // 새로운 패널은 열거형으로 계속 추가하면 됨
    }

    [System.Serializable]
    private class PanelInfo
    {
        public MenuPanel type;
        public GameObject? panel;
        public List<MenuPanel> parentPanels = new List<MenuPanel>(); // "뒤로 가기" 버튼 누를 시 이동할 상위 패널 목록
    }

    [Header("Canvas")]
    [SerializeField] private Canvas? mainCanvas;

    // 여러 패널에서 공통으로 사용할 뒤로 가기 버튼과 홈 버튼
    [Header("Navigation")]
    [SerializeField] private GameObject? navButtonContainer;
    [SerializeField] private Button? backButton;
    [SerializeField] private Button? homeButton;
    [SerializeField] private Button? itemInventoryButton;
    [SerializeField] private Button? operatorInventoryButton;

    [Header("For SquadEdit Panel")]
    [SerializeField] private GameObject squadEditButtonContainer;
    [SerializeField] private ExpandableButton squadBulkEditButton;
    [SerializeField] private ExpandableButton resetSquadButton;


    [Header("Panel References")]
    [SerializeField] private List<PanelInfo>? panels;

    [SerializeField] private float panelTransitionSpeed;

    [Header("Notification")]
    [SerializeField] private GameObject? notificationPanelPrefab;

    [Header("ConfirmationPanel")]
    [SerializeField] private ConfirmationPanel confirmPanelPrefab = default!;

    // 패널로 오브젝트 조회하는 딕셔너리
    private Dictionary<MenuPanel, GameObject> panelMap = new Dictionary<MenuPanel, GameObject>();

    // 오브젝트로 패널 조회하는 딕셔너리
    private Dictionary<GameObject, MenuPanel> reversePanelMap = new Dictionary<GameObject, MenuPanel>();

    // 어떤 패널의 부모 패널들을 조회하는 딕셔너리
    private Dictionary<MenuPanel, List<MenuPanel>> parentMap = new Dictionary<MenuPanel, List<MenuPanel>>();

    public Dictionary<MenuPanel, GameObject> PanelMap => panelMap;

    public MenuPanel CurrentPanel { get; private set; } = MenuPanel.StageSelect;
    // BeforePanel을 관리하는 건 크게 의미가 없어 보임 : 상위 -> 하위는 몰라도 하위 -> 상위는 이상한 듯

    // 여러 개의 parentPanels을 둔 패널로 진입할 때, 어떤 패널에서 진입했는지를 저장하는 변수
    public MenuPanel ConditionalParentPanel { get; private set; } = MenuPanel.None;

    public DateTime LastNotificationTime { get; private set; }

    public StageData? SelectedStage { get; private set; }

    // 캔버스 전환 시 최소 알파값
    private float minAlpha = 0f;

    // 2번 클릭되는 버튼을 위한 옵션. 최초로 클릭된 버튼이 갖는다.
    private Button firstClickedButton;

    public OwnedOperator? CurrentExistingOperator { get; private set; }
    public OwnedOperator? CurrentEditingOperator { get; private set; }


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        if (panels != null)
        {
            // Dict 초기화: panelMap는 PanelInfo에서 가져옴
            foreach (PanelInfo panelInfo in panels)
            {
                if (panelInfo.panel == null)
                {
                    Debug.LogError("panelInfo.panel 값이 null");
                    continue;
                }
                panelMap[panelInfo.type] = panelInfo.panel;
            }
        }

        InitializeNavigation();
    }

    private void InitializeNavigation()
    {
        if (panels != null)
        {
            // Dict 초기화: panelMap, parentMap, reversePanelMap
            foreach (PanelInfo panelInfo in panels)
            {
                if (panelInfo.panel != null)
                {
                    panelMap[panelInfo.type] = panelInfo.panel;
                    // 초기화된 리스트를 그대로 사용
                    parentMap[panelInfo.type] = panelInfo.parentPanels;
                    reversePanelMap[panelInfo.panel] = panelInfo.type;
                }
            }
        }

        InitializeListeners();
        UpdateTopAreaButtons();
    }

    private void InitializeListeners()
    {
        if (backButton != null) backButton.onClick.AddListener(NavigateBack);
        if (homeButton != null) homeButton.onClick.AddListener(NavigateToHome);
        if (itemInventoryButton != null) itemInventoryButton.onClick.AddListener(NavigateToItemInventory);
        if (operatorInventoryButton != null) operatorInventoryButton.onClick.AddListener(NavigateToOperatorInventory);
    }

    public void NavigateBack()
    {
        if (CurrentPanel == MenuPanel.None) return;

        if (parentMap.TryGetValue(CurrentPanel, out List<MenuPanel> parentPanels))
        {
            if (CurrentPanel == MenuPanel.OperatorInventory && 
                GameManagement.Instance!.UserSquadManager.IsEditingSlot)
            {
                //Debug.Break(); // 테스트용
                GameManagement.Instance!.UserSquadManager.CancelOperatorSelection();
            }

            // 여러 부모 패널이 있었고, 
            if (parentPanels.Count > 1 && ConditionalParentPanel != MenuPanel.None)
            {
                ActivateAndFadeOut(panelMap[ConditionalParentPanel], panelMap[CurrentPanel]);
                ConditionalParentPanel = MenuPanel.None;
            }
            else
            {
                ActivateAndFadeOut(panelMap[parentPanels[0]], panelMap[CurrentPanel]);
            }
        }
        else
        {
            Debug.LogWarning($"{CurrentPanel}에는 연결된 상위 패널이 없음");
        }
    }


    public void NavigateToHome()
    {
        ActivateAndFadeOut(panelMap[MenuPanel.StageSelect], panelMap[CurrentPanel]);
    }

    public void NavigateToItemInventory()
    {
        FadeInAndHide(panelMap[MenuPanel.ItemInventory], panelMap[CurrentPanel]);
    }

    private void NavigateToOperatorInventory()
    {
        FadeInAndHide(panelMap[MenuPanel.OperatorInventory], panelMap[CurrentPanel]);
    }

    private void NavigateToOperatorInventoryWithBulk()
    {
        GameManagement.Instance!.UserSquadManager.SetIsBulkEditing(true);
        NavigateToOperatorInventory();
    }

    private void UpdateTopAreaButtons()
    {
        if (navButtonContainer == null || itemInventoryButton == null)
        {
            Debug.LogError("navButtonContainer가 null이거나 itemInventoryButton이 null");
            return;
        }

        if (CurrentPanel == MenuPanel.StageSelect)
        {
            navButtonContainer.SetActive(false);
            itemInventoryButton.gameObject.SetActive(true);
            operatorInventoryButton.gameObject.SetActive(true);
        }
        else
        {
            navButtonContainer.SetActive(true);
            itemInventoryButton.gameObject.SetActive(false);
            operatorInventoryButton.gameObject.SetActive(false);
        }

        if (CurrentPanel == MenuPanel.SquadEdit)
        {
            squadEditButtonContainer.gameObject.SetActive(true);
            InitializeExpandableButtons();
        }
        else
        {
            squadEditButtonContainer.gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        if (panels == null) return;

        foreach (PanelInfo panel in panels)
        {
            if (panel.type == MenuPanel.StageSelect)
            {
                panel.panel?.SetActive(true);
            }
            else
            {
                panel.panel?.SetActive(false);
            }
        }

        SetLastPlayedStage();

        if (GameManagement.Instance == null)
        {
            throw new InvalidOperationException("게임 매니지먼트 인스턴스가 초기화되지 않았음");
        }

        GameManagement.Instance.UserSquadManager.OnSquadUpdated += OnSquadUpdated;
    }

    private void SetLastPlayedStage()
    {
        string lastPlayedStage = PlayerPrefs.GetString("LastPlayedStage", null);
        if (!string.IsNullOrEmpty(lastPlayedStage))
        {
            if (panelMap.TryGetValue(MenuPanel.StageSelect, out GameObject stageSelectObj))
            {
                var stageSelectPanel = stageSelectObj.GetComponent<StageSelectPanel>();
                if (stageSelectPanel != null)
                {
                    stageSelectPanel.SetStageButtonById(lastPlayedStage);
                    StageButton? currentStageButton = stageSelectPanel.CurrentStageButton;
                    if (currentStageButton != null)
                    {
                        StageData targetStageData = stageSelectPanel.GetStageDataFromStageButton(currentStageButton);
                        if (targetStageData != null)
                        {
                            SetSelectedStage(targetStageData);
                        }
                    }
                }
            }
            PlayerPrefs.DeleteKey("LastPlayedStage");
            PlayerPrefs.Save();
        }
    }

    // 상하위의 정의 : StageSelect > SquadEdit 등 사용자가 맞닥뜨리는 대략적인 순서

    // 애니메이션 1 - 상위 패널 -> 하위 패널로 들어갈 때 사용
    public void FadeInAndHide(GameObject panelToShow, GameObject panelToHide)
    {
        // 여러 부모를 가진 패널의 경우 - 숨기는 패널을 조건부 부모 패널로 지정 
        SetConditionalParentPanel(panelToHide, panelToShow);

        if (reversePanelMap.TryGetValue(panelToShow, out MenuPanel newPanel))
        {
            CurrentPanel = newPanel;
            panelToShow.SetActive(true);
            UpdateTopAreaButtons();
            CanvasGroup showGroup = panelToShow.GetComponent<CanvasGroup>();
            showGroup.alpha = minAlpha;
            showGroup.DOKill();
            showGroup.DOFade(1f, panelTransitionSpeed)
                .OnComplete(() =>
                {
                    panelToHide.SetActive(false);
                });
        }
    }

    // 애니메이션 2 - 하위 패널에서 상위 패널로 나올 때 사용
    public void ActivateAndFadeOut(GameObject panelToShow, GameObject panelToHide)
    {
        if (reversePanelMap.TryGetValue(panelToShow, out MenuPanel newPanel))
        {
            CurrentPanel = newPanel;
            panelToShow.SetActive(true);
            CanvasGroup showGroup = panelToShow.GetComponent<CanvasGroup>();
            showGroup.alpha = 1f;
            UpdateTopAreaButtons();
            CanvasGroup hideGroup = panelToHide.GetComponent<CanvasGroup>();
            hideGroup.DOFade(minAlpha, panelTransitionSpeed)
                .OnComplete(() =>
                {
                    panelToHide.SetActive(false);
                });
        }
    }

    public void StartStage()
    {
        if (GameManagement.Instance == null)
        {
            throw new InvalidOperationException("게임 매니지먼트 인스턴스가 초기화되지 않았음");
        }
        List<SquadOperatorInfo> currentSquad = GameManagement.Instance.UserSquadManager.GetCurrentSquad();
        if (currentSquad.Count > 0)
        {
            if (SelectedStage != null)
            {
                GameManagement.Instance.StageLoader.LoadStage(SelectedStage);
            }
        }
        else
        {
            Debug.LogWarning("빈 스쿼드로는 스테이지를 시작할 수 없음");
        }
    }

    public void SetSelectedStage(StageData stageData)
    {
        SelectedStage = stageData;
    }

    private void OnSquadUpdated()
    {
        UpdateSquadUI();
    }

    private void UpdateSquadUI()
    {
        List<SquadOperatorInfo?> currentSquad = GameManagement.Instance!.UserSquadManager.GetCurrentSquadWithNull();
        // UI 업데이트
    }

    public void OnOperatorSelected(int slotIndex, OwnedOperator newOperator)
    {
        if (GameManagement.Instance != null)
        {
            GameManagement.Instance.UserSquadManager.TryReplaceOperator(slotIndex, newOperator);
        }
    }

    public void ShowNotification(string message)
    {
        // 저번 알림 패널이 2초 내에 떴다면 아무 것도 활성화하지 않음
        if (DateTime.Now - LastNotificationTime < TimeSpan.FromSeconds(2)) return;

        if (notificationPanelPrefab != null && mainCanvas != null)
        {
            GameObject notificationObj = Instantiate(notificationPanelPrefab, mainCanvas.transform);
            NotificationPanel notificationPanel = notificationObj.GetComponent<NotificationPanel>();
            notificationPanel?.Initialize(message);

            LastNotificationTime = DateTime.Now;
        }
    }

    private void SetConditionalParentPanel(GameObject parentPanelObject, GameObject childPanelObject)
    {
        MenuPanel parentPanel = reversePanelMap[parentPanelObject];
        MenuPanel childPanel = reversePanelMap[childPanelObject];

        // 자식 패널에 있는 부모 패널들을 검사
        if (parentMap.TryGetValue(childPanel, out List<MenuPanel> childsParentPanels)) 
        {
            // 부모 패널이 여러 개이고, parentPanel을 포함할 때
            if (childsParentPanels.Count > 1 && childsParentPanels.Contains(parentPanel))
            {
                ConditionalParentPanel = parentPanel;
            }
        }
    }

    private void InitializeExpandableButtons()
    {
        squadBulkEditButton.SetIsExpanded(true, isInitializing: true);
        resetSquadButton.SetIsExpanded(false, isInitializing: true);
    }

    // expandableButton을 클릭했을 때 다른 버튼은 확장 상태를 해제함
    public void SetTheOtherExpandableButton(ExpandableButton clickedButton)
    {
        if (clickedButton == squadBulkEditButton)
        {
            resetSquadButton.SetIsExpanded(false);
        }
        else
        {
            squadBulkEditButton.SetIsExpanded(false);
        }
    }

    // 유니티 액션에서 등록
    public void HandleSquadBulkEditButton()
    {
        NavigateToOperatorInventoryWithBulk();
    }

    // 유니티 액션에서 등록
    public void HandleResetSquadButton()
    {
        // 스쿼드 초기화를 할까를 알리는 확인 패널을 띄움. 
        // 실제 초기화는 확인 패널의 확인 버튼을 클릭했을 때 진행
        ConfirmationPanel confirmPanelInstance = Instantiate(confirmPanelPrefab, mainCanvas.transform);
        confirmPanelInstance.Initialize("현재 스쿼드를 초기화하겠습니까?", isCancelButton: true, blurAreaActivation: false);
        confirmPanelInstance.OnConfirm += ClearSquad;
    }

    private void ClearSquad()
    {
        GameManagement.Instance!.UserSquadManager.ClearSquad();
    }

    public void SetCurrentEditingOperator(OwnedOperator op)
    {
        CurrentEditingOperator = op;
    }

    private void OnDestroy()
    {
        if (GameManagement.Instance != null)
        {
            GameManagement.Instance.UserSquadManager.OnSquadUpdated -= OnSquadUpdated;
        }
    }
}
 