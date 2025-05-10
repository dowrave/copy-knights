using System;
using System.Collections.Generic;
using System.Xml;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// �� ��ȯ �ÿ��� �����Ǿ�� �ϴ� ���� �Ŵ����� �����ϴ� Ŭ����
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager? Instance { get; private set; }

    // ���� �гε��� ���������� ����
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
        // ���ο� �г��� ���������� ��� �߰��ϸ� ��
    }

    [System.Serializable]
    private class PanelInfo
    {
        public MenuPanel type;
        public GameObject? panel;
        public List<MenuPanel> parentPanels = new List<MenuPanel>(); // "�ڷ� ����" ��ư ���� �� �̵��� ���� �г� ���
    }

    [Header("Canvas")]
    [SerializeField] private Canvas? mainCanvas;

    // ���� �гο��� �������� ����� �ڷ� ���� ��ư�� Ȩ ��ư
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

    // �гη� ������Ʈ ��ȸ�ϴ� ��ųʸ�
    private Dictionary<MenuPanel, GameObject> panelMap = new Dictionary<MenuPanel, GameObject>();

    // ������Ʈ�� �г� ��ȸ�ϴ� ��ųʸ�
    private Dictionary<GameObject, MenuPanel> reversePanelMap = new Dictionary<GameObject, MenuPanel>();

    // � �г��� �θ� �гε��� ��ȸ�ϴ� ��ųʸ�
    private Dictionary<MenuPanel, List<MenuPanel>> parentMap = new Dictionary<MenuPanel, List<MenuPanel>>();

    public Dictionary<MenuPanel, GameObject> PanelMap => panelMap;

    public MenuPanel CurrentPanel { get; private set; } = MenuPanel.StageSelect;
    // BeforePanel�� �����ϴ� �� ũ�� �ǹ̰� ���� ���� : ���� -> ������ ���� ���� -> ������ �̻��� ��

    // ���� ���� parentPanels�� �� �гη� ������ ��, � �гο��� �����ߴ����� �����ϴ� ����
    public MenuPanel ConditionalParentPanel { get; private set; } = MenuPanel.None;

    public DateTime LastNotificationTime { get; private set; }

    public StageData? SelectedStage { get; private set; }

    // ĵ���� ��ȯ �� �ּ� ���İ�
    private float minAlpha = 0f;

    // 2�� Ŭ���Ǵ� ��ư�� ���� �ɼ�. ���ʷ� Ŭ���� ��ư�� ���´�.
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
            // Dict �ʱ�ȭ: panelMap�� PanelInfo���� ������
            foreach (PanelInfo panelInfo in panels)
            {
                if (panelInfo.panel == null)
                {
                    Debug.LogError("panelInfo.panel ���� null");
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
            // Dict �ʱ�ȭ: panelMap, parentMap, reversePanelMap
            foreach (PanelInfo panelInfo in panels)
            {
                if (panelInfo.panel != null)
                {
                    panelMap[panelInfo.type] = panelInfo.panel;
                    // �ʱ�ȭ�� ����Ʈ�� �״�� ���
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
                //Debug.Break(); // �׽�Ʈ��
                GameManagement.Instance!.UserSquadManager.CancelOperatorSelection();
            }

            // ���� �θ� �г��� �־���, 
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
            Debug.LogWarning($"{CurrentPanel}���� ����� ���� �г��� ����");
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
            Debug.LogError("navButtonContainer�� null�̰ų� itemInventoryButton�� null");
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
            throw new InvalidOperationException("���� �Ŵ�����Ʈ �ν��Ͻ��� �ʱ�ȭ���� �ʾ���");
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

    // �������� ���� : StageSelect > SquadEdit �� ����ڰ� �´ڶ߸��� �뷫���� ����

    // �ִϸ��̼� 1 - ���� �г� -> ���� �гη� �� �� ���
    public void FadeInAndHide(GameObject panelToShow, GameObject panelToHide)
    {
        // ���� �θ� ���� �г��� ��� - ����� �г��� ���Ǻ� �θ� �гη� ���� 
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

    // �ִϸ��̼� 2 - ���� �гο��� ���� �гη� ���� �� ���
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
            throw new InvalidOperationException("���� �Ŵ�����Ʈ �ν��Ͻ��� �ʱ�ȭ���� �ʾ���");
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
            Debug.LogWarning("�� ������δ� ���������� ������ �� ����");
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
        // UI ������Ʈ
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
        // ���� �˸� �г��� 2�� ���� ���ٸ� �ƹ� �͵� Ȱ��ȭ���� ����
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

        // �ڽ� �гο� �ִ� �θ� �гε��� �˻�
        if (parentMap.TryGetValue(childPanel, out List<MenuPanel> childsParentPanels)) 
        {
            // �θ� �г��� ���� ���̰�, parentPanel�� ������ ��
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

    // expandableButton�� Ŭ������ �� �ٸ� ��ư�� Ȯ�� ���¸� ������
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

    // ����Ƽ �׼ǿ��� ���
    public void HandleSquadBulkEditButton()
    {
        NavigateToOperatorInventoryWithBulk();
    }

    // ����Ƽ �׼ǿ��� ���
    public void HandleResetSquadButton()
    {
        // ������ �ʱ�ȭ�� �ұ �˸��� Ȯ�� �г��� ���. 
        // ���� �ʱ�ȭ�� Ȯ�� �г��� Ȯ�� ��ư�� Ŭ������ �� ����
        ConfirmationPanel confirmPanelInstance = Instantiate(confirmPanelPrefab, mainCanvas.transform);
        confirmPanelInstance.Initialize("���� �����带 �ʱ�ȭ�ϰڽ��ϱ�?", isCancelButton: true, blurAreaActivation: false);
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
 