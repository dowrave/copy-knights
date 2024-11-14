using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

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
        OperatorList
        // ���ο� �г��� ���������� ��� �߰��ϸ� ��
    }

    [System.Serializable]
    private class PanelInfo
    {
        public MenuPanel type;
        public GameObject panel;
    }


    [Header("Panel References")]
    [SerializeField] private List<PanelInfo> panels;

    [Header("Class Icon Data")]
    [SerializeField] private IconData classIconData;

    [SerializeField] private float panelTransitionSpeed;

    private Dictionary<MenuPanel, GameObject> panelMap = new Dictionary<MenuPanel, GameObject>();
    public Dictionary<MenuPanel, GameObject> PanelMap => panelMap;

    private StageData selectedStage;


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

        // OperatorClass �������� ����ϴ� IconHelper �ʱ�ȭ
        IconHelper.Initialize(classIconData);
    }

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

        // ������ �÷��� ������ Ȯ��, �ش� �������� ����
        string lastPlayedStage = PlayerPrefs.GetString("LastPlayedStage", null);
        if (!string.IsNullOrEmpty(lastPlayedStage))
        {
            if (panelMap.TryGetValue(MenuPanel.StageSelect, out GameObject stageSelectObj))
            {
                var stageSelectPanel = stageSelectObj.GetComponent<StageSelectPanel>();
                if (stageSelectPanel != null)
                {
                    StageData targetStageData = stageSelectPanel.GetStageDataById(lastPlayedStage);
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

        GameManagement.Instance.UserSquadManager.OnSquadUpdated += OnSquadUpdated;
    }

    // �� �г� ���̵� �� �� ���� �г� ��Ȱ��ȭ
    public void FadeInAndHide(GameObject panelToShow, GameObject panelToHide)
    {
        CanvasGroup showGroup = panelToShow.GetComponent<CanvasGroup>();
        panelToShow.SetActive(true);
        showGroup.alpha = 0f;
        showGroup.DOFade(1f, panelTransitionSpeed)
            .OnComplete(() => panelToHide.SetActive(false));
    }

    // �� �г� Ȱ��ȭ �� ���� �г� ���̵� �ƿ�
    public void ActivateAndFadeOut(GameObject panelToShow, GameObject panelToHide)
    {
        // ���� �г� ��� Ȱ��ȭ -> ���� �г� ���̵� �ƿ�
        panelToShow.SetActive(true);
        CanvasGroup hideGroup = panelToHide.GetComponent<CanvasGroup>();
        hideGroup.DOFade(0f, panelTransitionSpeed)
            .OnComplete(() => panelToHide.SetActive(false));
    }

    // �������� ����
    public void StartStage()
    {
        List<OperatorData> currentSquad = GameManagement.Instance.UserSquadManager.GetActiveOperators();

        if (currentSquad.Count > 0) // null�� ������ ���´ٸ� �����ؼ� ���Ƿ� GetActiveOperators()�� ��� �Ѵ�.
        {
            GameManagement.Instance.StageLoader.LoadStage(selectedStage);
        }
        else
        {
            Debug.LogWarning("�� ������δ� ���������� ������ �� ����");
        }
    }

    /// <summary>
    /// StageSelectPanel���� �̿� : ���� �������� ������ �����ϰ� SquadEditPanel�� �Ѿ
    /// </summary>
    public void SetSelectedStage(StageData stageData)
    {
        selectedStage = stageData;
    }

    private void OnSquadUpdated()
    {
        // UI ������Ʈ
        UpdateSquadUI();
    }

    private void UpdateSquadUI()
    {
        List<OperatorData> currentSquad = GameManagement.Instance.UserSquadManager.GetCurrentSquad();

        // UI ������Ʈ
    }

    // SquadEditPanel�� ���Կ� OperatorListPanel���� ���۷����͸� �����ϰ� �Ҵ��� �� ���
    public void OnOperatorSelected(int slotIndex, OperatorData newOperatorData)
    {
        GameManagement.Instance.UserSquadManager.TryReplaceOperator(slotIndex, newOperatorData);
    }

    private void OnDestroy()
    {
        GameManagement.Instance.UserSquadManager.OnSquadUpdated -= OnSquadUpdated;
    }
}
 