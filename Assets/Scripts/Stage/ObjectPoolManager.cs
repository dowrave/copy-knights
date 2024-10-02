using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 오브젝트 풀링(=탄알집)을 구현하는 매니저
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

    private void Start()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();

        foreach (Pool pool in pools)
        {
            CreatePool(pool.tag, pool.prefab, pool.size);
        }
    }

    /// <summary>
    /// 지정된 태그, 프리팹, 크기로 새로운 오브젝트 풀을 생성합니다.
    /// </summary>
    /// <param name="tag">풀의 고유 식별자</param>
    /// <param name="prefab">풀에서 사용할 게임 오브젝트 프리팹</param>
    /// <param name="size">풀의 초기 크기</param>
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
    /// 지정된 태그의 풀에서 오브젝트를 가져와 활성화하고 위치와 회전을 설정합니다.
    /// </summary>
    /// <param name="tag">풀의 태그</param>
    /// <param name="position">스폰 위치</param>
    /// <param name="rotation">스폰 회전</param>
    /// <returns>스폰된 게임 오브젝트</returns>
    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag)) return null;

        Queue<GameObject> objectPool = poolDictionary[tag];

        if (objectPool.Count == 0)
        {
            Pool poolInfo = pools.Find(p => p.tag == tag);
            GameObject newObj = Instantiate(poolInfo.prefab);
            return SetupPooledObject(newObj, tag, position, rotation);
        }

        GameObject objectToSpawn = objectPool.Dequeue();
        return SetupPooledObject(objectToSpawn, tag, position, rotation);
    }

    /// <summary>
    /// 풀에서 가져온 오브젝트를 설정합니다. 위치, 회전을 설정하고 IPooledObject 인터페이스를 구현한 경우 OnObjectSpawn을 호출합니다.
    /// </summary>
    /// <param name="obj">설정할 게임 오브젝트</param>
    /// <param name="tag">풀의 태그</param>
    /// <param name="position">설정할 위치</param>
    /// <param name="rotation">설정할 회전</param>
    /// <returns>설정된 게임 오브젝트</returns>
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
    /// <param name="tag">풀의 태그</param>
    /// <param name="obj">반환할 게임 오브젝트</param>
    public void ReturnToPool(string tag, GameObject obj)
    {
        if (poolsMarkedForRemoval.Contains(tag))
        {
            Destroy(obj);
            CheckAndRemovePool(tag);
        }
        else if (poolDictionary.TryGetValue(tag, out Queue<GameObject> objectPool))
        {
            obj.SetActive(false);
            poolDictionary[tag].Enqueue(obj);
        }
    }

    /// <summary>
    /// 지정된 태그의 풀을 완전히 제거합니다. 풀의 모든 오브젝트를 파괴합니다.
    /// </summary>
    /// <param name="tag">제거할 풀의 태그</param>
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
    /// 지정된 태그의 풀을 제거 예정으로 표시합니다. 활성 상태의 프로젝타일도 제거 예정으로 표시합니다.
    /// </summary>
    /// <param name="tag">제거 예정으로 표시할 풀의 태그</param>
    public void MarkPoolForRemoval(string tag)
    {
       if (poolDictionary.ContainsKey(tag))
        {
            poolsMarkedForRemoval.Add(tag);
            MarkActiveProjectilesForRemoval(tag);
        }
    }

    /// <summary>
    /// 지정된 태그의 활성 상태 프로젝타일을 제거 예정으로 표시합니다.
    /// </summary>
    /// <param name="tag">대상 풀의 태그</param>
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
    /// 제거 예정으로 표시된 풀을 검사하고, 모든 오브젝트가 반환된 경우 풀을 완전히 제거합니다.
    /// </summary>
    /// <param name="tag">검사할 풀의 태그</param>
    private void CheckAndRemovePool(string tag)
    {
        if (poolDictionary.TryGetValue(tag, out Queue<GameObject> objectPool) && objectPool.Count == 0)
        {
            poolDictionary.Remove(tag);
            pools.RemoveAll(p => p.tag == tag);
            poolsMarkedForRemoval.Remove(tag);
        }
    }
}

public interface IPooledObject
{
    void OnObjectSpawn();
}
