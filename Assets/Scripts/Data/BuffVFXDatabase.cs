using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New CC Effect DB", menuName = "Game/CC Effect Database")]
public class BuffVFXDatabase : ScriptableObject
{
    [System.Serializable]
    public struct BuffVFXMapping
    {
        [Tooltip("버프 클래스의 정확한 이름 (네임스페이스 포함)")]
        public string exactBuffClassName;
        public GameObject vfxPrefab;
        [Tooltip("풀의 초기 생성 개수")]
        public int initialPoolSize;
        [Tooltip("타겟을 기준으로 한 VFX의 로컬 위치 오프셋")]
        public Vector3 vfxOffset;
    }

    [SerializeField] private List<BuffVFXMapping> vfxMappings = new List<BuffVFXMapping>();

    // 런타임용 Dict
    private Dictionary<System.Type, BuffVFXMapping> vfxDataMap; // 타입에 따른 vfx 데이터 저장 딕셔너리

    public void Initialize()
    {
        vfxDataMap = new Dictionary<System.Type, BuffVFXMapping>();

        foreach (var mapping in vfxMappings)
        {
            // 문자열 클래스 이름을 실제 Type으로 변환
            System.Type buffType = System.Type.GetType(mapping.exactBuffClassName);

            if (buffType == null)
            {
                Debug.LogWarning($"버프 이펙트 데이터베이스에 : {mapping.exactBuffClassName}이 없음");
                continue;
            }
            if (mapping.vfxPrefab == null)
            {
                Debug.LogWarning($"버프 이펙트 데이터베이스에 : {mapping.vfxPrefab}이 없음");
                continue;
            }
            if (vfxDataMap.ContainsKey(buffType))
            {
                Debug.Log($"[BuffVFXDatabase] 중복된 버프 타입이 존재합니다. : {mapping.exactBuffClassName}");
                continue;
            }

            vfxDataMap.Add(buffType, mapping);

            // 풀 생성
            if (ObjectPoolManager.Instance != null)
            {
                string poolTag = mapping.exactBuffClassName;
                int poolSize = mapping.initialPoolSize > 0 ? mapping.initialPoolSize : 5;
                ObjectPoolManager.Instance.CreatePool(poolTag, mapping.vfxPrefab, poolSize);
            }
            else
            {
                Debug.Log("ObjectPoolManager 초기화되지 않음");
            }

        }
    }

    public bool TryGetVFXData(System.Type buffType, out BuffVFXMapping data)
    {
        if (vfxDataMap == null)
        {
            Debug.LogError("[BuffVFXDatabase] db가 초기화되지 않았음");
            data = default;
            return false;
        }

        return vfxDataMap.TryGetValue(buffType, out data);
    }
}
