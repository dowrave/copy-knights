using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New CC Effect DB", menuName = "Game/CC Effect Database")]
public class CCEffectDatabase : ScriptableObject
{
    [System.Serializable]
    public struct CCEffectMapping
    {
        public System.Type ccType; // CC 타입
        public GameObject vfxPrefab;
    }

    [Header("CC Vfx Prefabs")]
    [SerializeField] private GameObject? stunVfxPrefab;
    [SerializeField] private GameObject? slowVfxPrefab;

    private Dictionary<System.Type, GameObject>? CCVfxMap;

    public void Initialize()
    {
        CCVfxMap = new Dictionary<System.Type, GameObject>();

        // 이펙트 프리팹이 할당된 경우에만 매핑 추가. 메서드화 불가능
        if (stunVfxPrefab != null)
        {
            CCVfxMap.Add(typeof(StunEffect), stunVfxPrefab);
        }
        if (slowVfxPrefab != null)
        {
            CCVfxMap.Add(typeof(SlowEffect), slowVfxPrefab);
        }
    }


    public GameObject? GetEffectPrefab(System.Type ccType)
    {
        InstanceValidator.ValidateInstance(CCVfxMap);

        return CCVfxMap!.TryGetValue(ccType, out GameObject prefab) ? prefab : null;
    }

}
