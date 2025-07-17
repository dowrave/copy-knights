using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New CC Effect DB", menuName = "Game/CC Effect Database")]
public class BuffEffectDatabase : ScriptableObject
{
    [System.Serializable]
    public struct BuffEffectMapping
    {
        // public System.Type buffType; // CC Ÿ��
        public string buffClassName; // Ÿ���� ��Ȯ�� ���ڿ��� �־�� ��
        public GameObject vfxPrefab;
    }

    [SerializeField] private List<BuffEffectMapping> effectMappings = new List<BuffEffectMapping>();

    // ��Ÿ�ӿ� Dict
    private Dictionary<System.Type, GameObject> buffVfxMap;

    public void Initialize()
    {
        buffVfxMap = new Dictionary<System.Type, GameObject>();

        foreach (var mapping in effectMappings)
        {
            // ���ڿ� Ŭ���� �̸��� ���� Type���� ��ȯ
            System.Type buffType = System.Type.GetType(mapping.buffClassName);

            if (buffType == null)
            {
                Debug.LogWarning($"���� ����Ʈ �����ͺ��̽��� : {mapping.buffClassName}�� ����");
                return;
            }
            if (mapping.vfxPrefab == null)
            {
                Debug.LogWarning($"���� ����Ʈ �����ͺ��̽��� : {mapping.vfxPrefab}�� ����");
                return;
            }

            // ���� buffType�� �ְ�, ����Ʈ �����յ� �ִٸ� �߰���
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
            Debug.LogError("���� ����Ʈ DB�� �ʱ�ȭ���� ����");
            return null;
        }  

        return buffVfxMap.TryGetValue(buffType, out GameObject prefab) ? prefab : null;
    }

}
