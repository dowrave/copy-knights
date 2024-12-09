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
    public static MainMenuManager Instance { get; private set; }

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
        ItemInventory
        // 새로운 패널은 열거형으로 계속 추가하면 됨
    }

    [System.Serializable]
    private class PanelInfo
    {
        public MenuPanel type;
        public GameObject panel;
        public MenuPanel parentPanel = MenuPanel.None; // "뒤로 가기" 버튼을 눌렀을 때 이동할 부모 패널 
    }

    [Header("Canvas")]
    [SerializeField] private Canvas mainCanvas;

    // 여러 패널에서 공통으로 사용할 뒤로 가기 버튼과 홈 버튼
    [Header("Navigation")]
    [SerializeField] private GameObject navButtonContainer;
    [SerializeField] private Button backButton;
    [SerializeField] private Button homeButton;

    [Header("Panel References")]
    [SerializeField] private List<PanelInfo> panels;

    [SerializeField] private float panelTransitionSpeed;

    [Header("Notification")]
    [SerializeField] private GameObject notificationPanelPrefab;

    // 패널 간의 연결을 쉽게 하기 위한 Dict들
    private Dictionary<MenuPanel, GameObject> panelMap = new Dictionary<MenuPanel, GameObject>();
    private Dictionary<GameObject, MenuPanel> reversePanelMap = new Dictionary<GameObject, MenuPanel>();
    private Dictionary<MenuPanel, MenuPanel> parentMap = new Dictionary<MenuPanel, MenuPanel>();
    public Dictionary<MenuPanel, GameObject> PanelMap => panelMap;

    private MenuPanel currentPanel = MenuPanel.StageSelect;
    public StageData SelectedStage { get; private set; }

    public event Action OnSelectedStageChanged;
    //public event Action OnPanelChanged;

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

        // Dict 초기화
        foreach (PanelInfo panelInfo in panels)
        {
            panelMap[panelInfo.type] = panelInfo.panel;
        }

        InitializeNavigation();
    }

    private void InitializeNavigation()
    {
        // Dict 초기화
        foreach (PanelInfo panelInfo in panels)
        {
            panelMap[panelInfo.type] = panelInfo.panel;
            parentMap[panelInfo.type] = panelInfo.parentPanel;
            reversePanelMap[panelInfo.panel] = panelInfo.type; 
        }

        if (backButton != null) backButton.onClick.AddListener(NavigateBack);
        if (homeButton != null) homeButton.onClick.AddListener(NavigateToHome);

        UpdateNavigationButtons();
    }

    public void NavigateBack()
    {
        if (currentPanel == MenuPanel.None) return;

        if (parentMap.TryGetValue(currentPanel, out MenuPanel parentPanel) && parentPanel != MenuPanel.None)
        {
            ActivateAndFadeOut(panelMap[parentPanel], panelMap[currentPanel]);
            //OnPanelChanged?.Invoke();
        }
        else
        {
            Debug.LogWarning($"{currentPanel}에는 부모가 없음");
        }
    }

    public void NavigateToHome()
    {
        ActivateAndFadeOut(panelMap[MenuPanel.StageSelect], panelMap[currentPanel]);
    }

    private void UpdateNavigationButtons()
    {
        Debug.Log(currentPanel);

        if (currentPanel == MenuPanel.StageSelect)
        {
            navButtonContainer.SetActive(false);
        }
        else
        {
            navButtonContainer.SetActive(true);
        }
    }

    /// <summary>
    /// 각 패널의 초기 상태 지시 구조는 MainMenuManager에서 관리하는 게 더 명확하다.
    /// </summary>
    private void Start()
    {
        // StageSelectPanel만 활성화
        foreach (PanelInfo panel in panels)
        {
            if (panel.type == MenuPanel.StageSelect)
            {
                panel.panel.SetActive(true);
            }
            else
            {
                panel.panel.SetActive(false);
            }
        }
        SetLastPlayedStage();

        GameManagement.Instance.UserSquadManager.OnSquadUpdated += OnSquadUpdated;
    }

    /// <summary>
    /// 마지막으로 플레이한 스테이지가 2성 이하였거나 실패했던 경우 
    /// 해당 스테이지를 선택한 상태로 메인 메뉴가 나타남
    /// </summary>
    private void SetLastPlayedStage()
    {
        // 마지막 플레이 정보를 확인, 해당 스테이지 선택
        string lastPlayedStage = PlayerPrefs.GetString("LastPlayedStage", null);
        if (!string.IsNullOrEmpty(lastPlayedStage))
        {
            if (panelMap.TryGetValue(MenuPanel.StageSelect, out GameObject stageSelectObj))
            {
                var stageSelectPanel = stageSelectObj.GetComponent<StageSelectPanel>();
                if (stageSelectPanel != null)
                {
                    // UI 상 CurrentStageButton 변경
                    stageSelectPanel.SetStageButtonById(lastPlayedStage); 
                    // 현재 선택된 스테이지 지정
                    StageData targetStageData = stageSelectPanel.GetStageDataFromStageButton(stageSelectPanel.CurrentStageButton);

                    if (targetStageData != null)
                    {
                        SetSelectedStage(targetStageData);
                    }
                }
            }
            // 사용 정보는 삭제
            PlayerPrefs.DeleteKey("LastPlayedStage");
            PlayerPrefs.Save();
        }
    }

    // 새 패널 페이드 인 후 이전 패널 비활성화, 주로 더 깊게 들어갈 때 사용
    public void FadeInAndHide(GameObject panelToShow, GameObject panelToHide)
    {
        if (reversePanelMap.TryGetValue(panelToShow, out MenuPanel newPanel))
        {
            // 새 패널 활성화
            panelToShow.SetActive(true);
            currentPanel = newPanel;
            UpdateNavigationButtons();

            // 새 패널 페이드 인
            CanvasGroup showGroup = panelToShow.GetComponent<CanvasGroup>();
            showGroup.alpha = 0f;
            showGroup.DOKill(); 
            showGroup.DOFade(1f, panelTransitionSpeed)
                .OnComplete(() => {
                    panelToHide.SetActive(false);
                });

        }
    }

    // 새 패널 활성화 후 이전 패널 페이드 아웃, 주로 깊은 패널에서 얕은 패널로 나올 때 사용
    public void ActivateAndFadeOut(GameObject panelToShow, GameObject panelToHide)
    {
        if (reversePanelMap.TryGetValue(panelToShow, out MenuPanel newPanel))
        {
            // 새 패널 활성화 
            panelToShow.SetActive(true);
            CanvasGroup showGroup = panelToShow.GetComponent<CanvasGroup>();
            showGroup.alpha = 1f; // 어떤 패널은 알파가 0을 유지하는 경우가 있어서 일부러 넣음

            currentPanel = newPanel;
            UpdateNavigationButtons();

            // 현재 패널 페이드 아웃
            CanvasGroup hideGroup = panelToHide.GetComponent<CanvasGroup>();
            hideGroup.DOFade(0f, panelTransitionSpeed)
                .OnComplete(() =>
                {
                    panelToHide.SetActive(false);
                });
        }
    }

    // 스테이지 시작
    public void StartStage()
    {
        List<OwnedOperator> currentSquad = GameManagement.Instance.UserSquadManager.GetCurrentSquad();

        if (currentSquad.Count > 0) // null을 값으로 갖는다면 포함해서 세므로 GetActiveOperators()을 써야 한다.
        {
            GameManagement.Instance.StageLoader.LoadStage(SelectedStage);
        }
        else
        {
            Debug.LogWarning("빈 스쿼드로는 스테이지를 시작할 수 없음");
        }
    }

    /// <summary>
    /// 현재 스테이지 정보를 저장
    /// </summary>
    public void SetSelectedStage(StageData stageData)
    {
        SelectedStage = stageData;
    }

    private void OnSquadUpdated()
    {
        // UI 업데이트
        UpdateSquadUI();
    }

    private void UpdateSquadUI()
    {
        List<OwnedOperator> currentSquad = GameManagement.Instance.UserSquadManager.GetCurrentSquadWithNull();

        // UI 업데이트
    }

    // SquadEditPanel의 슬롯에 OperatorListPanel에서 오퍼레이터를 결정하고 할당할 때 사용
    public void OnOperatorSelected(int slotIndex, OwnedOperator newOperator)
    {
        GameManagement.Instance.UserSquadManager.TryReplaceOperator(slotIndex, newOperator);
    }

    public void ShowNotification(string message)
    {
        if (notificationPanelPrefab != null)
        {
            GameObject notificationObj = Instantiate(notificationPanelPrefab, mainCanvas.transform);
            NotificationPanel notificationPanel = notificationObj.GetComponent<NotificationPanel>();
            notificationPanel.Initialize(message);
        }
    }

    private void OnDestroy()
    {
        GameManagement.Instance.UserSquadManager.OnSquadUpdated -= OnSquadUpdated;
    }
}
 