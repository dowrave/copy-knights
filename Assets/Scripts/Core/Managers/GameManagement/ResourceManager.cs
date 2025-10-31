
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
    [SerializeField] private string textUpdateColor = "#179bff"; // 업데이트되는 요소 미리 보여줄 때 사용

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
            Logger.LogError("IconData가 할당되지 않음");
            return;
        }

        OperatorIconHelper.Initialize(iconData);
    }

    // SceneManager.OnSceneLoaded 이벤트에 대응한 리소스 초기화
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
    {
        // 씬 전환 시에 필요한 리소스 재초기화 로직
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // 게임 종료 시 리소스 정리 작업
    private void OnApplicationQuit()
    {
        
    }

    // 읽기 전용 프로퍼티
    public OperatorIconData IconData => iconData; 
    public Sprite StageButtonStar1 => stageButtonStar1; 
    public Sprite StageButtonStar2 => stageButtonStar2;
    public Sprite StageButtonStar3 => stageButtonStar3;
    public string TextUpdateColor => textUpdateColor;

    public Color OnSkillColor => onSkillColor;

    public Color OffSkillColor => offSkillColor;

    public GameObject PathIndicator => pathIndicator;
}
