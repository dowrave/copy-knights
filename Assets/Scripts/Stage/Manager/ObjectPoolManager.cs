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
    private HashSet<string> poolsMarkedForRemoval = new HashSet<string>();

    // �ؽ�Ʈ ����
    [SerializeField] private GameObject floatingTextPrefab;
    public string FLOATING_TEXT_TAG = "FloatingText";
    [SerializeField] private int floatingTextCounts = 2;

    private void Start()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();

        // �˾� �ؽ�Ʈ Ǯ ����
        CreatePool(FLOATING_TEXT_TAG, floatingTextPrefab, floatingTextCounts);

    }

    /// <summary>
    /// ������ �±�, ������, ũ��� ���ο� ������Ʈ Ǯ�� �����մϴ�.
    /// </summary>
    /// <param name="tag">Ǯ�� ���� �ĺ���</param>
    /// <param name="prefab">Ǯ���� ����� ���� ������Ʈ ������</param>
    /// <param name="size">Ǯ�� �ʱ� ũ��</param>
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

    /// <summary>
    /// ������ �±��� Ǯ���� ������Ʈ�� ������ Ȱ��ȭ�ϰ� ��ġ�� ȸ���� �����մϴ�.
    /// </summary>
    /// <param name="tag">Ǯ�� �±�</param>
    /// <param name="position">���� ��ġ</param>
    /// <param name="rotation">���� ȸ��</param>
    /// <returns>������ ���� ������Ʈ</returns>
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
    /// <param name="obj">������ ���� ������Ʈ</param>
    /// <param name="tag">Ǯ�� �±�</param>
    /// <param name="position">������ ��ġ</param>
    /// <param name="rotation">������ ȸ��</param>
    /// <returns>������ ���� ������Ʈ</returns>
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
    /// <param name="tag">Ǯ�� �±�</param>
    /// <param name="obj">��ȯ�� ���� ������Ʈ</param>
    public void ReturnToPool(string tag, GameObject obj)
    {
        if (poolsMarkedForRemoval.Contains(tag))
        {
            Destroy(obj);

            CheckAndRemovePool(tag);
        }
        else if (poolDictionary.TryGetValue(tag, out Queue<GameObject> objectPool))
        {
            // �� ��, ť���� �� ���� Ȱ��ȭ���� -> ���� ���� ť�� ���� ���� ��Ȱ��ȭ? ��Ȱ��ȭ�� ���� ť�� ����? 
            poolDictionary[tag].Enqueue(obj);
            obj.SetActive(false);
        }
    }

    /// <summary>
    /// ������ �±��� Ǯ�� ������ �����մϴ�. Ǯ�� ��� ������Ʈ�� �ı��մϴ�.
    /// </summary>
    /// <param name="tag">������ Ǯ�� �±�</param>
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

    /// <summary>
    /// ������ �±��� Ǯ�� ���� �������� ǥ���մϴ�. Ȱ�� ������ ������Ÿ�ϵ� ���� �������� ǥ���մϴ�.
    /// </summary>
    /// <param name="tag">���� �������� ǥ���� Ǯ�� �±�</param>
    public void MarkPoolForRemoval(string tag)
    {
       if (poolDictionary.ContainsKey(tag))
        {
            poolsMarkedForRemoval.Add(tag);
            MarkActiveProjectilesForRemoval(tag);
        }
    }

    /// <summary>
    /// ������ �±��� Ȱ�� ���� ������Ÿ���� ���� �������� ǥ���մϴ�.
    /// </summary>
    /// <param name="tag">��� Ǯ�� �±�</param>
    private void MarkActiveProjectilesForRemoval(string tag)
    {
        Projectile[] activeProjectiles = FindObjectsOfType<Projectile>()
            .Where(p => p.gameObject.activeInHierarchy && p.PoolTag == tag)
            .ToArray();

        foreach (var projectile in activeProjectiles)
        {
            projectile.MarkPoolForRemoval();
        }
    }

    /// <summary>
    /// ���� �������� ǥ�õ� Ǯ�� �˻��ϰ�, ��� ������Ʈ�� ��ȯ�� ��� Ǯ�� ������ �����մϴ�.
    /// </summary>
    /// <param name="tag">�˻��� Ǯ�� �±�</param>
    private void CheckAndRemovePool(string tag)
    {
        if (poolDictionary.TryGetValue(tag, out Queue<GameObject> objectPool) && objectPool.Count == 0)
        {
            poolDictionary.Remove(tag);
            pools.RemoveAll(p => p.tag == tag);
            poolsMarkedForRemoval.Remove(tag);
        }
    }

    // ����� �˾� ���� ����
    public void ShowFloatingText(Vector3 position, float value, bool isHealing)
    {
        GameObject popupObj = SpawnFromPool(FLOATING_TEXT_TAG, position, Quaternion.identity);
        if (popupObj != null)
        {
            FloatingText popup = popupObj.GetComponent<FloatingText>();
            if (popup != null)
            {
                popup.SetValue(value, isHealing);
            }
        }
    }
}

public interface IPooledObject
{
    void OnObjectSpawn();
}
