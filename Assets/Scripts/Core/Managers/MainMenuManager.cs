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
    public static MainMenuManager Instance { get; private set; }

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
        ItemInventory
        // ���ο� �г��� ���������� ��� �߰��ϸ� ��
    }

    [System.Serializable]
    private class PanelInfo
    {
        public MenuPanel type;
        public GameObject panel;
        public MenuPanel parentPanel = MenuPanel.None; // "�ڷ� ����" ��ư�� ������ �� �̵��� �θ� �г� 
    }

    [Header("Canvas")]
    [SerializeField] private Canvas mainCanvas;

    // ���� �гο��� �������� ����� �ڷ� ���� ��ư�� Ȩ ��ư
    [Header("Navigation")]
    [SerializeField] private GameObject navButtonContainer;
    [SerializeField] private Button backButton;
    [SerializeField] private Button homeButton;

    [Header("Panel References")]
    [SerializeField] private List<PanelInfo> panels;

    [SerializeField] private float panelTransitionSpeed;

    [Header("Notification")]
    [SerializeField] private GameObject notificationPanelPrefab;

    // �г� ���� ������ ���� �ϱ� ���� Dict��
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

        // Dict �ʱ�ȭ
        foreach (PanelInfo panelInfo in panels)
        {
            panelMap[panelInfo.type] = panelInfo.panel;
        }

        InitializeNavigation();
    }

    private void InitializeNavigation()
    {
        // Dict �ʱ�ȭ
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
            Debug.LogWarning($"{currentPanel}���� �θ� ����");
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
    /// �� �г��� �ʱ� ���� ���� ������ MainMenuManager���� �����ϴ� �� �� ��Ȯ�ϴ�.
    /// </summary>
    private void Start()
    {
        // StageSelectPanel�� Ȱ��ȭ
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
    /// ���������� �÷����� ���������� 2�� ���Ͽ��ų� �����ߴ� ��� 
    /// �ش� ���������� ������ ���·� ���� �޴��� ��Ÿ��
    /// </summary>
    private void SetLastPlayedStage()
    {
        // ������ �÷��� ������ Ȯ��, �ش� �������� ����
        string lastPlayedStage = PlayerPrefs.GetString("LastPlayedStage", null);
        if (!string.IsNullOrEmpty(lastPlayedStage))
        {
            if (panelMap.TryGetValue(MenuPanel.StageSelect, out GameObject stageSelectObj))
            {
                var stageSelectPanel = stageSelectObj.GetComponent<StageSelectPanel>();
                if (stageSelectPanel != null)
                {
                    // UI �� CurrentStageButton ����
                    stageSelectPanel.SetStageButtonById(lastPlayedStage); 
                    // ���� ���õ� �������� ����
                    StageData targetStageData = stageSelectPanel.GetStageDataFromStageButton(stageSelectPanel.CurrentStageButton);

                    if (targetStageData != null)
                    {
                        SetSelectedStage(targetStageData);
                    }
                }
            }
            // ��� ������ ����
            PlayerPrefs.DeleteKey("LastPlayedStage");
            PlayerPrefs.Save();
        }
    }

    // �� �г� ���̵� �� �� ���� �г� ��Ȱ��ȭ, �ַ� �� ��� �� �� ���
    public void FadeInAndHide(GameObject panelToShow, GameObject panelToHide)
    {
        if (reversePanelMap.TryGetValue(panelToShow, out MenuPanel newPanel))
        {
            // �� �г� Ȱ��ȭ
            panelToShow.SetActive(true);
            currentPanel = newPanel;
            UpdateNavigationButtons();

            // �� �г� ���̵� ��
            CanvasGroup showGroup = panelToShow.GetComponent<CanvasGroup>();
            showGroup.alpha = 0f;
            showGroup.DOKill(); 
            showGroup.DOFade(1f, panelTransitionSpeed)
                .OnComplete(() => {
                    panelToHide.SetActive(false);
                });

        }
    }

    // �� �г� Ȱ��ȭ �� ���� �г� ���̵� �ƿ�, �ַ� ���� �гο��� ���� �гη� ���� �� ���
    public void ActivateAndFadeOut(GameObject panelToShow, GameObject panelToHide)
    {
        if (reversePanelMap.TryGetValue(panelToShow, out MenuPanel newPanel))
        {
            // �� �г� Ȱ��ȭ 
            panelToShow.SetActive(true);
            CanvasGroup showGroup = panelToShow.GetComponent<CanvasGroup>();
            showGroup.alpha = 1f; // � �г��� ���İ� 0�� �����ϴ� ��찡 �־ �Ϻη� ����

            currentPanel = newPanel;
            UpdateNavigationButtons();

            // ���� �г� ���̵� �ƿ�
            CanvasGroup hideGroup = panelToHide.GetComponent<CanvasGroup>();
            hideGroup.DOFade(0f, panelTransitionSpeed)
                .OnComplete(() =>
                {
                    panelToHide.SetActive(false);
                });
        }
    }

    // �������� ����
    public void StartStage()
    {
        List<OwnedOperator> currentSquad = GameManagement.Instance.UserSquadManager.GetCurrentSquad();

        if (currentSquad.Count > 0) // null�� ������ ���´ٸ� �����ؼ� ���Ƿ� GetActiveOperators()�� ��� �Ѵ�.
        {
            GameManagement.Instance.StageLoader.LoadStage(SelectedStage);
        }
        else
        {
            Debug.LogWarning("�� ������δ� ���������� ������ �� ����");
        }
    }

    /// <summary>
    /// ���� �������� ������ ����
    /// </summary>
    public void SetSelectedStage(StageData stageData)
    {
        SelectedStage = stageData;
    }

    private void OnSquadUpdated()
    {
        // UI ������Ʈ
        UpdateSquadUI();
    }

    private void UpdateSquadUI()
    {
        List<OwnedOperator> currentSquad = GameManagement.Instance.UserSquadManager.GetCurrentSquadWithNull();

        // UI ������Ʈ
    }

    // SquadEditPanel�� ���Կ� OperatorListPanel���� ���۷����͸� �����ϰ� �Ҵ��� �� ���
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
 