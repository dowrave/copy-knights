
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ResourceManager : MonoBehaviour
{
    [Header("Shared Resources")]
    [SerializeField] private OperatorIconData iconData;

    [Header("Update Text Color")]
    public string textUpdateColor = "#179bff"; // ������Ʈ�Ǵ� ��� �̸� ������ �� ���

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

    public OperatorIconData getIconData()
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
