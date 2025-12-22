using System.Collections.Generic;
using System.Linq;
using UnityEngine;


/// <summary>
/// 오브젝트 풀링(=탄알집)을 이용하는 것들을 구현해줍니다
/// 투사체, 대미지 팝업 등등 여기에 포함됩니다
/// </summary>
public class ObjectPoolManager : MonoBehaviour
{
    [Header("텍스트 관련")]
    [SerializeField] private GameObject? floatingTextPrefab;
    [SerializeField] private int floatingTextCounts = 2;

    // 딱히 관리할 곳이 안 보여서 여기서 관리함
    public const string FloatingTextTag = "FloatingText";
    public const string PathIndicatorTag = "pathIndicator";

    public static ObjectPoolManager? Instance;

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

    private Dictionary<string, ObjectPool> _pools = new Dictionary<string, ObjectPool>(); // 각 풀의 정보(설정, 메타데이터)

    private void Start()
    {
        if (floatingTextPrefab != null)
        {
            // 팝업 텍스트 풀 생성
            CreatePool(FloatingTextTag, floatingTextPrefab, floatingTextCounts);
        }
    }

    // 지정된 태그, 프리팹, 크기로 새로운 오브젝트 풀을 생성합니다.
    public void CreatePool(string tag, GameObject prefab, int size = 3)
    {
        if (tag == string.Empty)
        {
            Logger.LogError("[ObjectPoolManager.CreatePool]비어 있는 태그 값이 들어옴");
            return;    
        }

        // 이미 풀이 존재한다면 size만큼 풀을 추가해줌
        if (IsPoolExist(tag))
        {
            ObjectPool existingPool = _pools[tag];
            existingPool.AddPoolSize(size);
            Logger.LogWarning($"이미 있는 풀에 {size}만큼 풀 추가 완료");
            return;
        }

        _pools[tag] = new ObjectPool(tag, prefab, size);
    }

    // 지정된 태그의 풀이 있는지 확인
    public bool IsPoolExist(string tag)
    {
        return _pools.ContainsKey(tag);
    }

    // 지정된 태그의 풀에서 오브젝트를 가져와 활성화하고 위치와 회전을 설정합니다.
    public GameObject? SpawnFromPool(string tag, Vector3 position, Quaternion rotation, Transform? parent = null)
    {
        ObjectPool targetPool = _pools[tag];
        return targetPool.SpawnFromPool(tag, position, rotation, parent);
    }


    // 사용이 끝난 오브젝트를 풀로 반환합니다. 풀이 제거된 경우 오브젝트를 파괴합니다.
    public void ReturnToPool(string tag, GameObject obj)
    {
        ObjectPool targetPool = _pools[tag];
        if (targetPool != null) targetPool.ReturnToPool(obj);
        else Destroy(obj);
    }

    // 지정된 태그의 풀을 완전히 제거합니다. 풀의 모든 오브젝트를 파괴합니다.
    public void RemovePool(string tag)
    {
        ObjectPool targetPool = _pools[tag];

        // 유니티 씬의 실제 게임 오브젝트들을 파괴
        targetPool.Clear();

        // 매니저 관리 목록에서 제거
        _pools.Remove(tag);

        // 로컬 변수 해제는 별도로 구현하지 않아도 null이 되지만 명시적으로
    }

    // 대미지 팝업 관련 구현
    public void ShowFloatingText(Vector3 position, float value, bool isHealing)
    {
        GameObject? floatingTextObj = SpawnFromPool(FloatingTextTag, position + Vector3.up * 0.3f, CameraManager.Instance.baseRotation);
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
        foreach (var tag in _pools.Keys.ToArray())
        {
            RemovePool(tag);
        }

        // 텍스트 풀은 게임 내내 유지할 수 있으므로 필요에 따라 예외 처리
    }
}
