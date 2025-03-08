using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New CC Effect DB", menuName = "Game/CC Effect Database")]
public class CCEffectDatabase : ScriptableObject
{
    [System.Serializable]
    public struct CCEffectMapping
    {
        public System.Type ccType; // CC Ÿ��
        public GameObject vfxPrefab;
    }

    [Header("CC Vfx Prefabs")]
    [SerializeField] private GameObject? stunVfxPrefab;
    [SerializeField] private GameObject? slowVfxPrefab;

    private Dictionary<System.Type, GameObject>? CCVfxMap;

    public void Initialize()
    {
        CCVfxMap = new Dictionary<System.Type, GameObject>();

        // ����Ʈ �������� �Ҵ�� ��쿡�� ���� �߰�. �޼���ȭ �Ұ���
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
