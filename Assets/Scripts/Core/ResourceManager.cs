
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    [Header("Shared Resources")]
    [SerializeField] private IconData iconData;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeResources();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeResources()
    {
        if (iconData == null)
        {
            Debug.LogError("IconData가 할당되지 않음");
            return;
        }

        IconHelper.Initialize(iconData);
    }

    public IconData getIconData()
    {
        return iconData;
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
}
