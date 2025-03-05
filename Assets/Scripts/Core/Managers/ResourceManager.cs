
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
    [SerializeField] private string textUpdateColor = "#179bff"; // 업데이트되는 요소 미리 보여줄 때 사용

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
            Debug.LogError("IconData가 할당되지 않음");
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
    public OperatorIconData IconData { get { return iconData; } }
    public Sprite StageButtonStar1 { get { return stageButtonStar1; } }
    public Sprite StageButtonStar2 { get { return stageButtonStar2; } }
    public Sprite StageButtonStar3 { get { return stageButtonStar3; } }
    public string TextUpdateColor { get { return textUpdateColor; } }
    public Color OnSkillColor { get { return onSkillColor; } }
    public Color OffSkillColor { get { return offSkillColor; } }
}
