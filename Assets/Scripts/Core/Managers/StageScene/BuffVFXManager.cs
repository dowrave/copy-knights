
using UnityEngine;

// 버프 이펙트 효과를 위한 데이터베이스
public class BuffVFXManager : MonoBehaviour
{
    public static BuffVFXManager? Instance { get; private set; }

    [SerializeField] private BuffVFXDatabase? VFXDatabase;

    // ObjectPoolManager보다 늦게 초기화되어야 하므로 Start로 구현해둠
    private void Start()
    {
        if (Instance == null)
        {
            Instance = this;
            if (VFXDatabase != null)
            {
                VFXDatabase.Initialize();
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 버프 VFX 오브젝트를 오브젝트 풀에서 생성, 타겟의 위치에 생성
    public GameObject? GetBuffVFXObject(Buff buff, Transform target)
    {
        if (ObjectPoolManager.Instance == null || VFXDatabase == null) return null;

        // GameObject? prefab = VFXDatabase.GetVFXPrefab(buff.GetType());
        if (VFXDatabase.TryGetVFXData(buff.GetType(), out BuffVFXDatabase.BuffVFXMapping vfxData))
        {
            string poolTag = vfxData.exactBuffClassName;
            GameObject vfxObj = ObjectPoolManager.Instance.SpawnFromPool(poolTag, Vector3.zero, Quaternion.identity);
            if (vfxObj != null)
            {
                vfxObj.transform.SetParent(
                    target,
                    worldPositionStays: false // 부모를 기준으로 로컬 좌표계를 새로 설정함
                    );

                vfxObj.transform.localPosition = vfxData.vfxOffset;

                return vfxObj;
            }
        }

        return null;
    }

    // 풀로 버프 이펙트를 반환하는 메서드
    public void ReleaseBuffVFXObject(Buff buff, GameObject VFXObj)
    {
        if (VFXDatabase == null || ObjectPoolManager.Instance == null || VFXObj == null) return;

        if (VFXDatabase.TryGetVFXData(buff.GetType(), out BuffVFXDatabase.BuffVFXMapping vfxData)) 
        {
            string poolTag = vfxData.exactBuffClassName;
            VFXObj.transform.SetParent(null); // 부모-자식 관계 해제
            ObjectPoolManager.Instance.ReturnToPool(poolTag, VFXObj);
        }

    }
}
