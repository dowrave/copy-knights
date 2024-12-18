using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

    public List<Pool> pools;
    public Dictionary<string, Queue<GameObject>> poolDictionary;

    [Header("�ؽ�Ʈ ����")]
    [SerializeField] private GameObject floatingTextPrefab;
    [SerializeField] private int floatingTextCounts = 2;
    public string FLOATING_TEXT_TAG { get; private set; } = "FloatingText";

    [Header("����Ʈ ����")]
    [SerializeField] private int effectPoolSize = 3;
    public string EFFECT_PREFIX { get; private set; } = "Effect_";


    private void Start()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();

        // �˾� �ؽ�Ʈ Ǯ ����
        CreatePool(FLOATING_TEXT_TAG, floatingTextPrefab, floatingTextCounts);
    }

    /// <summary>
    /// ������ �±�, ������, ũ��� ���ο� ������Ʈ Ǯ�� �����մϴ�.
    /// </summary>
    public void CreatePool(string tag, GameObject prefab, int size)
    {
        Queue<GameObject> objectPool = new Queue<GameObject>(); 

        for (int i = 0; i < size; i++)
        {
            GameObject obj = Instantiate(prefab);
            obj.SetActive(false);
            objectPool.Enqueue(obj);
        }

        poolDictionary[tag] = objectPool;
    }

    public void CreateEffectPool(string effectName, GameObject effectPrefab)
    {
        // ����Ʈ ���� ������ �±׸� ������
        string poolTag = EFFECT_PREFIX + effectName; 

        if (!poolDictionary.ContainsKey(poolTag))
        {
            CreatePool(poolTag, effectPrefab, effectPoolSize);
        }
    }


    /// <summary>
    /// ������ �±��� Ǯ���� ������Ʈ�� ������ Ȱ��ȭ�ϰ� ��ġ�� ȸ���� �����մϴ�.
    /// </summary>
    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag)) return null;

        Queue<GameObject> objectPool = poolDictionary[tag];

        // ��� ������ ��ü�� �����ٸ� ���ο� �ν��Ͻ��� ����
        if (objectPool.Count == 0)
        {
            Pool poolInfo = pools.Find(p => p.tag == tag);
            GameObject newObj = Instantiate(poolInfo.prefab);
            return SetupPooledObject(newObj, tag, position, rotation);
        }

        // �̹� ������ ��ü�� �ִٸ�, �׳� ť���� ����
        GameObject objectToSpawn = objectPool.Dequeue();
        return SetupPooledObject(objectToSpawn, tag, position, rotation);
    }
    /// <summary>
    /// Ǯ���� ������ ������Ʈ�� �����մϴ�. ��ġ, ȸ���� �����ϰ� IPooledObject �������̽��� ������ ��� OnObjectSpawn�� ȣ���մϴ�.
    /// </summary>
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

    /// <summary>
    /// ����� ���� ������Ʈ�� Ǯ�� ��ȯ�մϴ�. Ǯ�� ���� ������ ��� ������Ʈ�� �ı��մϴ�.
    /// </summary>
    public void ReturnToPool(string tag, GameObject obj)
    {
        if (poolDictionary.TryGetValue(tag, out Queue<GameObject> objectPool))
        {
            // �� ��, ť���� �� ���� Ȱ��ȭ���� -> ���� ���� ť�� ���� ���� ��Ȱ��ȭ? ��Ȱ��ȭ�� ���� ť�� ����? 
            poolDictionary[tag].Enqueue(obj);
            obj.SetActive(false);
        }
    }

    /// <summary>
    /// ������ �±��� Ǯ�� ������ �����մϴ�. Ǯ�� ��� ������Ʈ�� �ı��մϴ�.
    /// </summary>
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
            pools.RemoveAll(p => p.tag == tag);
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
