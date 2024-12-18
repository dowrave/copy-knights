using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

    public List<Pool> pools;
    public Dictionary<string, Queue<GameObject>> poolDictionary;

    [Header("텍스트 관련")]
    [SerializeField] private GameObject floatingTextPrefab;
    [SerializeField] private int floatingTextCounts = 2;
    public string FLOATING_TEXT_TAG { get; private set; } = "FloatingText";

    [Header("이펙트 관련")]
    [SerializeField] private int effectPoolSize = 3;
    public string EFFECT_PREFIX { get; private set; } = "Effect_";


    private void Start()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();

        // 팝업 텍스트 풀 생성
        CreatePool(FLOATING_TEXT_TAG, floatingTextPrefab, floatingTextCounts);
    }

    /// <summary>
    /// 지정된 태그, 프리팹, 크기로 새로운 오브젝트 풀을 생성합니다.
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
        // 이펙트 별로 고유한 태그를 생성함
        string poolTag = EFFECT_PREFIX + effectName; 

        if (!poolDictionary.ContainsKey(poolTag))
        {
            CreatePool(poolTag, effectPrefab, effectPoolSize);
        }
    }


    /// <summary>
    /// 지정된 태그의 풀에서 오브젝트를 가져와 활성화하고 위치와 회전을 설정합니다.
    /// </summary>
    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag)) return null;

        Queue<GameObject> objectPool = poolDictionary[tag];

        // 모든 생성된 객체가 나갔다면 새로운 인스턴스를 만듦
        if (objectPool.Count == 0)
        {
            Pool poolInfo = pools.Find(p => p.tag == tag);
            GameObject newObj = Instantiate(poolInfo.prefab);
            return SetupPooledObject(newObj, tag, position, rotation);
        }

        // 이미 생성된 객체가 있다면, 그냥 큐에서 빼냄
        GameObject objectToSpawn = objectPool.Dequeue();
        return SetupPooledObject(objectToSpawn, tag, position, rotation);
    }
    /// <summary>
    /// 풀에서 가져온 오브젝트를 설정합니다. 위치, 회전을 설정하고 IPooledObject 인터페이스를 구현한 경우 OnObjectSpawn을 호출합니다.
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
    /// 사용이 끝난 오브젝트를 풀로 반환합니다. 풀이 제거 예정인 경우 오브젝트를 파괴합니다.
    /// </summary>
    public void ReturnToPool(string tag, GameObject obj)
    {
        if (poolDictionary.TryGetValue(tag, out Queue<GameObject> objectPool))
        {
            // 뺄 때, 큐에서 뺀 다음 활성화했음 -> 넣을 때도 큐에 넣은 다음 비활성화? 비활성화한 다음 큐에 넣음? 
            poolDictionary[tag].Enqueue(obj);
            obj.SetActive(false);
        }
    }

    /// <summary>
    /// 지정된 태그의 풀을 완전히 제거합니다. 풀의 모든 오브젝트를 파괴합니다.
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
