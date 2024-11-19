
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
            Debug.LogError("IconData�� �Ҵ���� ����");
            return;
        }

        IconHelper.Initialize(iconData);
    }

    public IconData getIconData()
    {
        return iconData;
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
}
