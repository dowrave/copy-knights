
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ResourceManager : MonoBehaviour
{
    [Header("Shared Resources")]
    [SerializeField] private OperatorIconData iconData;

    [Header("Star Images")]
    [SerializeField] private Sprite stageButtonStar1;
    [SerializeField] private Sprite stageButtonStar2;
    [SerializeField] private Sprite stageButtonStar3;

    [Header("Update Text Color")]
    [SerializeField] private string textUpdateColor = "#179bff"; // ������Ʈ�Ǵ� ��� �̸� ������ �� ���

    [Header("Color Palette")]
    [SerializeField] private Color onSkillColor = new Color(255, 134, 0, 255);
    [SerializeField] private Color offSkillColor = new Color(115, 219, 103, 255);


    private void Awake()
    {
        InitializeResources();
    }

    private void InitializeResources()
    {
        if (iconData == null)
        {
            Debug.LogError("IconData�� �Ҵ���� ����");
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
    public OperatorIconData IconData { get { return iconData; } }
    public Sprite StageButtonStar1 { get { return stageButtonStar1; } }
    public Sprite StageButtonStar2 { get { return stageButtonStar2; } }
    public Sprite StageButtonStar3 { get { return stageButtonStar3; } }
    public string TextUpdateColor { get { return textUpdateColor; } }
    public Color OnSkillColor { get { return onSkillColor; } }
    public Color OffSkillColor { get { return offSkillColor; } }
}
