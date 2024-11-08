using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;


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

    [SerializeField] private IconData classIconData;

    private Dictionary<MenuPanel, GameObject> panelMap = new Dictionary<MenuPanel, GameObject>();
    private MenuPanel currentPanel; // ����Ʈ�� 0���� �ִ� ��. null�� �ƴ�.

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
                ShowPanel(panel.type, false);
            } 
            else
            {
                HidePanel(panel.panel, false);
            }
        }

        GameManagement.Instance.UserSquadManager.OnSquadUpdated += OnSquadUpdated; 
    }

    public void ShowPanel(MenuPanel newPanel, bool animate = true)
    {
        if (currentPanel == newPanel) return;

        // ���� �г� �����
        if (panelMap.TryGetValue(currentPanel, out GameObject currentPanelObj))
        {
            HidePanel(currentPanelObj, animate);
        }

        Debug.Log($"{newPanel}�� �����ַ��� �Ѵ�");
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
                Debug.Log($"{panel} Ȱ��ȭ �Ϸ�");
                canvasGroup.alpha = 0f;
                canvasGroup.DOFade(1f, 0.3f);
                return;
            }
        }

        panel.SetActive(true);
        Debug.Log($"{panel} Ȱ��ȭ �Ϸ�");
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
        else
        {
            panel.SetActive(false);
        }
    }

    // �������� ����
    public void StartStage()
    {

        List<OperatorData> currentSquad = GameManagement.Instance.UserSquadManager.GetCurrentSquad();

        if (currentSquad.Count > 0)
        {
            // ���⼭�� ���� �̸��� �ҷ��;� ��(���������� �̸��� �ƴ�!)
            //SceneManager.LoadScene(selectedStage.stageName);

            GameManagement.Instance.StageLoader.LoadStage(selectedStage);
        }
        else
        {
            Debug.LogWarning("�� ������δ� ���������� ������ �� ����");
        }
    }

    public void OnStageSelected(StageData stageData)
    {
        selectedStage = stageData;
        ShowPanel(MenuPanel.SquadEdit);
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
 