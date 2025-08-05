using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;


/// <summary>
/// ������Ʈ Ǯ��(=ź����)�� �̿��ϴ� �͵��� �������ݴϴ�
/// ����ü, ����� �˾� ��� ���⿡ ���Ե˴ϴ�
/// </summary>
public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager? Instance;

    [System.Serializable]
    public class Pool
    {
        public string? tag;
        public GameObject? prefab;
        public int size;
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
    }

    private Dictionary<string, Pool> poolInfos = new Dictionary<string, Pool>(); // �� Ǯ�� ����(����, ��Ÿ������)
    public Dictionary<string, Queue<GameObject>> poolDictionary = new Dictionary<string, Queue<GameObject>>(); // ���� Ǯ�� ���� ������Ʈ �ν��Ͻ� ����
    private Dictionary<string, HashSet<GameObject>> activeObjects = new Dictionary<string, HashSet<GameObject>>(); // ���� Ȱ��ȭ�� ������Ʈ�� ����

    [Header("�ؽ�Ʈ ����")]
    [SerializeField] private GameObject? floatingTextPrefab;
    [SerializeField] private int floatingTextCounts = 2;
    public string FLOATING_TEXT_TAG { get; private set; } = "FloatingText";

    //[Header("����Ʈ ����")]
    //[SerializeField] private int effectPoolSize = 3;


    private void Start()
    {
        if (floatingTextPrefab != null)
        {
            // �˾� �ؽ�Ʈ Ǯ ����
            CreatePool(FLOATING_TEXT_TAG, floatingTextPrefab, floatingTextCounts);
        }
    }


    // ������ �±�, ������, ũ��� ���ο� ������Ʈ Ǯ�� �����մϴ�.
    public void CreatePool(string tag, GameObject prefab, int size = 3)
    {
        // �̹� Ǯ�� �����Ѵٸ� �������� ����
        if (IsPoolExist(tag)) return;

        Queue<GameObject> objectPool = new Queue<GameObject>();
        poolInfos[tag] = new Pool { tag = tag, prefab = prefab, size = size };

        for (int i = 0; i < size; i++)
        {
            GameObject obj = Instantiate(prefab);
            objectPool.Enqueue(obj);
            obj.SetActive(false);
        }

        poolDictionary[tag] = objectPool;
    }

    // ������ �±��� Ǯ�� �ִ��� Ȯ��
    public bool IsPoolExist(string tag)
    {
        // poolDicitonary�� poolInfo�� �Բ� ����ȭ�ǹǷ� �ϳ��� ���Ǹ� üũ��
        return poolDictionary.ContainsKey(tag);
    }

    // ������ �±��� Ǯ���� ������Ʈ�� ������ Ȱ��ȭ�ϰ� ��ġ�� ȸ���� �����մϴ�.
    public GameObject? SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!IsPoolExist(tag))
        {
            Debug.LogError($"{tag}��� �̸��� ���� �±װ� Ǯ ��Ͽ� �������� ����");
            return null; 
        }

        if (!activeObjects.ContainsKey(tag))
        {
            activeObjects[tag] = new HashSet<GameObject>();
        }

        Queue<GameObject> objectPool = poolDictionary[tag];
        GameObject obj;

        // (if) Ǯ�� ��� ����ߴٸ� ���� ���� / (else) Ǯ�� ���� ���
        if (objectPool.Count == 0)
        {
            Pool? poolInfo = poolInfos[tag];
            if (poolInfo == null || poolInfo.prefab == null)
            {
                Debug.LogError($"poolInfo.prefab�� null��!!");
                return null;
            }

            // ������ Ȯ�� �������� ����
            obj = Instantiate(poolInfo.prefab);
            poolInfo.size++; // Ǯ�� ������ ũ�⸦ 1 �ø���.
        }
        else
        {
            obj = objectPool.Dequeue();
        }

        activeObjects[tag].Add(obj);
        return SetupPooledObject(obj, tag, position, rotation);
    }


    // ������Ʈ Ȱ��ȭ �� ����
    private GameObject SetupPooledObject(GameObject obj, string tag, Vector3 position, Quaternion rotation)
    {
        obj.SetActive(true);
        obj.transform.position = position;
        obj.transform.rotation = rotation;

        IPooledObject pooledObj = obj.GetComponent<IPooledObject>();
        if (pooledObj != null)
        {
            pooledObj.OnObjectSpawn(tag);
        }

        return obj;
    }

    // ����� ���� ������Ʈ�� Ǯ�� ��ȯ�մϴ�. Ǯ�� ���ŵ� ��� ������Ʈ�� �ı��մϴ�.
    public void ReturnToPool(string tag, GameObject obj)
    {
        if (poolDictionary.TryGetValue(tag, out Queue<GameObject> objectPool))
        {
            // ť�� ���� �� ��Ȱ��ȭ
            objectPool.Enqueue(obj);
            if (activeObjects.ContainsKey(tag))
            {
                activeObjects[tag].Remove(obj);
            }
            obj.SetActive(false);
        }
        else
        {
            // Ǯ�� ������ ���, Ȱ��ȭ�� ������Ʈ ����
            Destroy(obj);
        }
    }


    // ������ �±��� Ǯ�� ������ �����մϴ�. Ǯ�� ��� ������Ʈ�� �ı��մϴ�.
    public void RemovePool(string tag)
    {
        if (poolDictionary.TryGetValue(tag, out Queue<GameObject> objectPool))
        {
            // Ȱ��ȭ�� ������Ʈ �ı�
            if (activeObjects.TryGetValue(tag, out HashSet<GameObject> currentlyActive))
            {
                // ���纻�� ���� ��ȸ�ؾ� �����ϴ�
                foreach (var activeObj in currentlyActive.ToArray())
                {
                    Destroy(activeObj);
                }
                activeObjects.Remove(tag);
            }

            // ť�� ��Ȱ��ȭ ������Ʈ �ı�
            while (objectPool.Count > 0)
            {
                GameObject obj = objectPool.Dequeue();
                Destroy(obj);
            }

            poolDictionary.Remove(tag);
            poolInfos.Remove(tag);
        }
    }

    // ����� �˾� ���� ����
    public void ShowFloatingText(Vector3 position, float value, bool isHealing)
    {
        GameObject? floatingTextObj = SpawnFromPool(FLOATING_TEXT_TAG, position + Vector3.up * 0.3f, CameraManager.Instance.baseRotation);
        if (floatingTextObj != null)
        {
            FloatingText floatingText = floatingTextObj.GetComponent<FloatingText>();
            if (floatingText != null)
            {
                floatingText.SetValue(value, isHealing);
            }
        }
    }

    // ��� Ǯ�� �����ϴ� �޼���. �������� ���� �ÿ� ȣ��ȴ�.
    public void ClearAllPools()
    {
        foreach (var tag in poolDictionary.Keys.ToArray())
        {
            RemovePool(tag);
        }

        // �ؽ�Ʈ Ǯ�� ���� ���� ������ �� �����Ƿ� �ʿ信 ���� ���� ó��

    }
}

public interface IPooledObject
{
    void OnObjectSpawn(string tag);
}
