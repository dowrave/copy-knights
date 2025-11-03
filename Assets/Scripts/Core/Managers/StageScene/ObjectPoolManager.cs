using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;


/// <summary>
/// 오브젝트 풀링(=탄알집)을 이용하는 것들을 구현해줍니다
/// 투사체, 대미지 팝업 등등 여기에 포함됩니다
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

    private Dictionary<string, Pool> poolInfos = new Dictionary<string, Pool>(); // 각 풀의 정보(설정, 메타데이터)
    public Dictionary<string, Queue<GameObject>> poolDictionary = new Dictionary<string, Queue<GameObject>>(); // 실제 풀의 개별 오브젝트 인스턴스 관리
    private Dictionary<string, HashSet<GameObject>> activeObjects = new Dictionary<string, HashSet<GameObject>>(); // 현재 활성화된 오브젝트들 추적

    [Header("텍스트 관련")]
    [SerializeField] private GameObject? floatingTextPrefab;
    [SerializeField] private int floatingTextCounts = 2;
    public string FLOATING_TEXT_TAG { get; private set; } = "FloatingText";

    //[Header("이펙트 관련")]
    //[SerializeField] private int effectPoolSize = 3;


    private void Start()
    {
        if (floatingTextPrefab != null)
        {
            // 팝업 텍스트 풀 생성
            CreatePool(FLOATING_TEXT_TAG, floatingTextPrefab, floatingTextCounts);
        }
    }


    // 지정된 태그, 프리팹, 크기로 새로운 오브젝트 풀을 생성합니다.
    public void CreatePool(string tag, GameObject prefab, int size = 3)
    {
        // 이미 풀이 존재한다면 생성하지 않음
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

    // 지정된 태그의 풀이 있는지 확인
    public bool IsPoolExist(string tag)
    {
        // poolDicitonary와 poolInfo가 함께 동기화되므로 하나의 조건만 체크함
        return poolDictionary.ContainsKey(tag);
    }

    // 지정된 태그의 풀에서 오브젝트를 가져와 활성화하고 위치와 회전을 설정합니다.
    public GameObject? SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!IsPoolExist(tag))
        {
            Logger.LogError($"{tag}라는 이름을 가진 태그가 풀 목록에 존재하지 않음");
            return null; 
        }

        if (!activeObjects.ContainsKey(tag))
        {
            activeObjects[tag] = new HashSet<GameObject>();
        }

        Queue<GameObject> objectPool = poolDictionary[tag];
        GameObject obj;

        // (if) 풀을 모두 사용했다면 새로 생성 / (else) 풀의 내용 사용
        if (objectPool.Count == 0)
        {
            Pool? poolInfo = poolInfos[tag];
            if (poolInfo == null || poolInfo.prefab == null)
            {
                Logger.LogError($"poolInfo.prefab이 null임!!");
                return null;
            }

            // 반응형 확장 로직으로 수정
            Logger.Log($"{tag}의 새로운 풀을 만듦 : {poolInfo.prefab.name}");
            obj = Instantiate(poolInfo.prefab);
            poolInfo.size++; // 풀의 개념적 크기를 1 늘린다.
        }
        else
        {
            obj = objectPool.Dequeue();
        }

        activeObjects[tag].Add(obj);
        return SetupPooledObject(obj, tag, position, rotation);
    }


    // 오브젝트 활성화 및 생성
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

    // 사용이 끝난 오브젝트를 풀로 반환합니다. 풀이 제거된 경우 오브젝트를 파괴합니다.
    public void ReturnToPool(string tag, GameObject obj)
    {
        if (poolDictionary.TryGetValue(tag, out Queue<GameObject> objectPool))
        {
            // 큐에 넣은 뒤 비활성화
            objectPool.Enqueue(obj);
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
            // 활성화된 오브젝트 파괴
            if (activeObjects.TryGetValue(tag, out HashSet<GameObject> currentlyActive))
            {
                // 복사본을 만들어서 순회해야 안전하다
                foreach (var activeObj in currentlyActive.ToArray())
                {
                    Destroy(activeObj);
                }
                activeObjects.Remove(tag);
            }

            // 큐의 비활성화 오브젝트 파괴
            while (objectPool.Count > 0)
            {
                GameObject obj = objectPool.Dequeue();
                Destroy(obj);
            }

            poolDictionary.Remove(tag);
            poolInfos.Remove(tag);
            // Logger.Log($"태그 {tag}의 오브젝트 풀 파괴");
        }
    }

    // 대미지 팝업 관련 구현
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

    // 모든 풀을 정리하는 메서드. 스테이지 종료 시에 호출된다.
    public void ClearAllPools()
    {
        foreach (var tag in poolDictionary.Keys.ToArray())
        {
            RemovePool(tag);
        }

        // 텍스트 풀은 게임 내내 유지할 수 있으므로 필요에 따라 예외 처리

    }
}

public interface IPooledObject
{
    void OnObjectSpawn(string tag);
}
