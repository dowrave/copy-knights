using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;


public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager Instance { get; private set; }

    // ���� �гε��� ���������� ����
    public enum MenuPanel
    {
        None, 
        StageSelect,
        SquadEdit,
        OperatorSelect
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

    private Dictionary<MenuPanel, GameObject> panelMap = new Dictionary<MenuPanel, GameObject>();
    private MenuPanel currentPanel; // enum Ÿ���̶� ���� �ʱ�ȭ ���ϸ� 0���� StageSelect�� ��

    private StageData selectedStageData;
    private OperatorSlotButton currentEditingSlot; // ���� �Ҵ��� ���۷����� ����
    public OperatorSlotButton CurrentEditingSlot 
    { 
        get => currentEditingSlot;
        private set
        {
            currentEditingSlot = value;
        }
    }


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
        foreach (var panelInfo in panels)
        {
            panelMap[panelInfo.type] = panelInfo.panel;
        }
    }

    private void Start()
    {
        foreach (PanelInfo panel in panels)
        {
            if (panel.type == MenuPanel.StageSelect)
            {
                Debug.Log($"���� �г� {panel.type} / ź ���ǹ� : ShowPanel");
                ShowPanel(panel.type, false);
            } 
            else
            { 
                Debug.Log($"���� �г� {panel.type} / ź ���ǹ� : HidePanel");
                HidePanel(panel.panel, false);
            }
        }
    }

    public void ShowPanel(MenuPanel newPanel, bool animate = true)
    {
        Debug.Log($"���� �г� {currentPanel} / �������� �г� : {newPanel}");

        if (currentPanel == newPanel) return;

        // ���� �г� �����
        if (panelMap.TryGetValue(currentPanel, out GameObject currentPanelObj))
        {
            HidePanel(currentPanelObj, animate);
        }

        // �� �г� ǥ��
        if (panelMap.TryGetValue(newPanel, out GameObject newPanelObj))
        {
            ShowPanelObject(newPanelObj, animate);
            currentPanel = newPanel; 
        }
    }

    private void ShowPanelObject(GameObject panel, bool animate)
    {
        if (animate)
        {
            // �ִϸ��̼� ó��
            CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                panel.SetActive(true);
                canvasGroup.alpha = 0f;
                canvasGroup.DOFade(1f, 0.3f);
            }
            else
            {
                panel.SetActive(true);
            }
        }
        else
        {
            panel.SetActive(true);
        }
    }

    private void HidePanel(GameObject panel, bool animate)
    {
        if (animate)
        {
            CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.DOFade(0f, 0.3f).OnComplete(() => panel.SetActive(false));
            }
            else
            {
                panel.SetActive(false);
            }
        }
    }

    // �������� ����
    public void StartStage(string stageName)
    {
        SceneManager.LoadScene(stageName);
    }

    public void OnStageSelected(StageData stageData)
    {
        selectedStageData = stageData;
        ShowPanel(MenuPanel.SquadEdit);
    }

    /// <summary>
    /// ���� ������ �Ŵ����� �Ҵ��ϰ�, ���۷����� ���� �г��� ���ϴ�.
    /// </summary>
    public void StartOperatorSelection(OperatorSlotButton clickedSlot)
    {
        CurrentEditingSlot = clickedSlot;
        ShowPanel(MenuPanel.OperatorSelect);
    }

}
 