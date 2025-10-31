
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ResourceManager : MonoBehaviour
{
    [Header("Shared Resources")]
    [SerializeField] private OperatorIconData iconData = default!;

    [Header("Star Images")]
    [SerializeField] private Sprite stageButtonStar1 = default!;
    [SerializeField] private Sprite stageButtonStar2 = default!;
    [SerializeField] private Sprite stageButtonStar3 = default!;

    [Header("Update Text Color")]
    [SerializeField] private string textUpdateColor = "#179bff"; // ������Ʈ�Ǵ� ��� �̸� ������ �� ���

    [Header("Color Palette")]
    [SerializeField] private Color onSkillColor = new Color(255, 134, 0, 255);
    [SerializeField] private Color offSkillColor = new Color(115, 219, 103, 255);

    [Header("Enemy Path Indicator")]
    [SerializeField] private GameObject pathIndicator = default!;

    private void Awake()
    {
        InitializeResources();
    }

    private void InitializeResources()
    {
        if (iconData == null)
        {
            Logger.LogError("IconData�� �Ҵ���� ����");
            return;
        }

        OperatorIconHelper.Initialize(iconData);
    }

    // SceneManager.OnSceneLoaded �̺�Ʈ�� ������ ���ҽ� �ʱ�ȭ
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
    {
        // �� ��ȯ �ÿ� �ʿ��� ���ҽ� ���ʱ�ȭ ����
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // ���� ���� �� ���ҽ� ���� �۾�
    private void OnApplicationQuit()
    {
        
    }

    // �б� ���� ������Ƽ
    public OperatorIconData IconData => iconData; 
    public Sprite StageButtonStar1 => stageButtonStar1; 
    public Sprite StageButtonStar2 => stageButtonStar2;
    public Sprite StageButtonStar3 => stageButtonStar3;
    public string TextUpdateColor => textUpdateColor;

    public Color OnSkillColor => onSkillColor;

    public Color OffSkillColor => offSkillColor;

    public GameObject PathIndicator => pathIndicator;
}
