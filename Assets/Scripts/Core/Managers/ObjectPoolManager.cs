using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;

/// <summary>
/// 오브젝트 풀링(=탄알집)을 이용하는 것들을 구현해줍니다
/// 투사체, 대미지 팝업 등등 여기에 포함됩니다
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
    private Dictionary<string, HashSet<GameObject>> activeObjects; // 현재 활성화된 오브젝트들 추적

    [Header("텍스트 관련")]
    [SerializeField] private GameObject floatingTextPrefab;
    [SerializeField] private int floatingTextCounts = 2;
    public string FLOATING_TEXT_TAG { get; private set; } = "FloatingText";

    [Header("이펙트 관련")]
    [SerializeField] private int effectPoolSize = 3;


    private void Start()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();
        activeObjects = new Dictionary<string, HashSet<GameObject>>();

        // 팝업 텍스트 풀 생성
        CreatePool(FLOATING_TEXT_TAG, floatingTextPrefab, floatingTextCounts);
    }


    // 지정된 태그, 프리팹, 크기로 새로운 오브젝트 풀을 생성합니다.
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


    // 지정된 태그의 풀에서 오브젝트를 가져와 활성화하고 위치와 회전을 설정합니다.
    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag) || !poolInfos.ContainsKey(tag)) return null;

        if (!activeObjects.ContainsKey(tag))
        {
            activeObjects[tag] = new HashSet<GameObject>();
        }

        Queue<GameObject> objectPool = poolDictionary[tag];
        GameObject obj;

        // (if) 풀을 모두 사용했다면 새로 생성 / (else) 풀의 내용 사용
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


    // 풀에서 가져온 오브젝트를 설정합니다.
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

    // 사용이 끝난 오브젝트를 풀로 반환합니다. 풀이 제거 예정인 경우 오브젝트를 파괴합니다.
    public void ReturnToPool(string tag, GameObject obj)
    {
        if (poolDictionary.TryGetValue(tag, out Queue<GameObject> objectPool))
        {
            // 큐에 넣은 뒤 비활성화
            poolDictionary[tag].Enqueue(obj);
            if (activeObjects.ContainsKey(tag))
            {
                activeObjects[tag].Remove(obj);
            }
            obj.SetActive(false);
        }
        else
        {
            // 풀이 없어진 경우, 활성화된 오브젝트 제거
            Destroy(obj);
        }
    }


    // 지정된 태그의 풀을 완전히 제거합니다. 풀의 모든 오브젝트를 파괴합니다.
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

    // 대미지 팝업 관련 구현
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
