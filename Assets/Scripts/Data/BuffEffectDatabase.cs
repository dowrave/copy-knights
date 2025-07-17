using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New CC Effect DB", menuName = "Game/CC Effect Database")]
public class BuffEffectDatabase : ScriptableObject
{
    [System.Serializable]
    public struct BuffEffectMapping
    {
        // public System.Type buffType; // CC 타입
        public string buffClassName; // 타입의 정확한 문자열을 넣어야 함
        public GameObject vfxPrefab;
    }

    [SerializeField] private List<BuffEffectMapping> effectMappings = new List<BuffEffectMapping>();

    // 런타임용 Dict
    private Dictionary<System.Type, GameObject> buffVfxMap;

    public void Initialize()
    {
        buffVfxMap = new Dictionary<System.Type, GameObject>();

        foreach (var mapping in effectMappings)
        {
            // 문자열 클래스 이름을 실제 Type으로 변환
            System.Type buffType = System.Type.GetType(mapping.buffClassName);

            if (buffType == null)
            {
                Debug.LogWarning($"버프 이펙트 데이터베이스에 : {mapping.buffClassName}이 없음");
                return;
            }
            if (mapping.vfxPrefab == null)
            {
                Debug.LogWarning($"버프 이펙트 데이터베이스에 : {mapping.vfxPrefab}이 없음");
                return;
            }

            // 실제 buffType이 있고, 이펙트 프리팹도 있다면 추가됨
            if (!buffVfxMap.ContainsKey(buffType))
            {
                buffVfxMap.Add(buffType, mapping.vfxPrefab);
            }
        }
        
    }


    public GameObject? GetEffectPrefab(System.Type buffType)
    {
        if (buffVfxMap == null)
        {
            Debug.LogError("버프 이펙트 DB가 초기화되지 않음");
            return null;
        }  

        return buffVfxMap.TryGetValue(buffType, out GameObject prefab) ? prefab : null;
    }

}
