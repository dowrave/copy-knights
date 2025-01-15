using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;

/// <summary>
/// ������Ʈ Ǯ��(=ź����)�� �̿��ϴ� �͵��� �������ݴϴ�
/// ����ü, ����� �˾� ��� ���⿡ ���Ե˴ϴ�
/// </summary>
public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager Instance;

    [System.Serializable] 
    public class Pool
    {
        public string tag;
        public GameObject prefab;
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

    private Dictionary<string, Pool> poolInfos = new Dictionary<string, Pool>();
    public Dictionary<string, Queue<GameObject>> poolDictionary;
    private Dictionary<string, HashSet<GameObject>> activeObjects; // ���� Ȱ��ȭ�� ������Ʈ�� ����

    [Header("�ؽ�Ʈ ����")]
    [SerializeField] private GameObject floatingTextPrefab;
    [SerializeField] private int floatingTextCounts = 2;
    public string FLOATING_TEXT_TAG { get; private set; } = "FloatingText";

    [Header("����Ʈ ����")]
    [SerializeField] private int effectPoolSize = 3;


    private void Start()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();
        activeObjects = new Dictionary<string, HashSet<GameObject>>();

        // �˾� �ؽ�Ʈ Ǯ ����
        CreatePool(FLOATING_TEXT_TAG, floatingTextPrefab, floatingTextCounts);
    }


    // ������ �±�, ������, ũ��� ���ο� ������Ʈ Ǯ�� �����մϴ�.
    public void CreatePool(string tag, GameObject prefab, int size = 5)
    {
        Queue<GameObject> objectPool = new Queue<GameObject>();

        poolInfos[tag] = new Pool { tag = tag, prefab = prefab, size = size };
        
        for (int i = 0; i < size; i++)
        {
            GameObject obj = Instantiate(prefab);
            obj.SetActive(false);
            objectPool.Enqueue(obj);
        }

        poolDictionary[tag] = objectPool;
    }


    // ������ �±��� Ǯ���� ������Ʈ�� ������ Ȱ��ȭ�ϰ� ��ġ�� ȸ���� �����մϴ�.
    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag) || !poolInfos.ContainsKey(tag)) return null;

        if (!activeObjects.ContainsKey(tag))
        {
            activeObjects[tag] = new HashSet<GameObject>();
        }

        Queue<GameObject> objectPool = poolDictionary[tag];
        GameObject obj;

        // (if) Ǯ�� ��� ����ߴٸ� ���� ���� / (else) Ǯ�� ���� ���
        if (objectPool.Count == 0)
        {
            Pool poolInfo = poolInfos[tag];
            obj = Instantiate(poolInfo.prefab);
        }
        else
        {
            obj = objectPool.Dequeue();
        }

        activeObjects[tag].Add(obj);
        return SetupPooledObject(obj, tag, position, rotation);
    }


    // Ǯ���� ������ ������Ʈ�� �����մϴ�.
    private GameObject SetupPooledObject(GameObject obj, string tag, Vector3 position, Quaternion rotation)
    {
        obj.SetActive(true);
        obj.transform.position = position;
        obj.transform.rotation = rotation;

        IPooledObject pooledObj = obj.GetComponent<IPooledObject>();
        if (pooledObj != null)
        {
            pooledObj.OnObjectSpawn();
        }

        return obj;
    }

    // ����� ���� ������Ʈ�� Ǯ�� ��ȯ�մϴ�. Ǯ�� ���� ������ ��� ������Ʈ�� �ı��մϴ�.
    public void ReturnToPool(string tag, GameObject obj)
    {
        if (poolDictionary.TryGetValue(tag, out Queue<GameObject> objectPool))
        {
            // ť�� ���� �� ��Ȱ��ȭ
            poolDictionary[tag].Enqueue(obj);
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
        GameObject floatingTextObj = SpawnFromPool(FLOATING_TEXT_TAG, position, Quaternion.identity);
        if (floatingTextObj != null)
        {
            FloatingText floatingText = floatingTextObj.GetComponent<FloatingText>();
            if (floatingText != null)
            {
                floatingText.SetValue(value, isHealing);
            }
        }
    }
}

public interface IPooledObject
{
    void OnObjectSpawn();
}
