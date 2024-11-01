using UnityEngine;
using UnityEngine.SceneManagement;


public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager Instance { get; private set; }

    [Header("Panel References")]
    [SerializeField] private StageSelectPanel stageSelectPanel;
    [SerializeField] private SquadEditPanel squadEditPanel;

    private StageData selectedStageData;

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
    }

    private void Start()
    {
        InitializePanels();
    }

    private void InitializePanels()
    {
        stageSelectPanel.gameObject.SetActive(true);
        squadEditPanel.gameObject.SetActive(false);
    }

    public void ShowSquadEditPanel()
    { 
        stageSelectPanel.gameObject.SetActive(false);
        squadEditPanel.gameObject.SetActive(true);
    }

    // �������� ����
    public void StartStage(string stageName)
    {
        SceneManager.LoadScene(stageName);
    }

    public void OnStageSelected(StageData stageData)
    {
        selectedStageData = stageData;

        // ������ �� �гη� ��ȯ
        ShowSquadEditPanel();
    }
}
