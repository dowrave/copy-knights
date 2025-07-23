using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New CC Effect DB", menuName = "Game/CC Effect Database")]
public class BuffVFXDatabase : ScriptableObject
{
    [System.Serializable]
    public struct BuffVFXMapping
    {
        [Tooltip("���� Ŭ������ ��Ȯ�� �̸� (���ӽ����̽� ����)")]
        public string exactBuffClassName;
        public GameObject vfxPrefab;
        [Tooltip("Ǯ�� �ʱ� ���� ����")]
        public int initialPoolSize;
        [Tooltip("Ÿ���� �������� �� VFX�� ���� ��ġ ������")]
        public Vector3 vfxOffset;
    }

    [SerializeField] private List<BuffVFXMapping> vfxMappings = new List<BuffVFXMapping>();

    // ��Ÿ�ӿ� Dict
    private Dictionary<System.Type, BuffVFXMapping> vfxDataMap; // Ÿ�Կ� ���� vfx ������ ���� ��ųʸ�

    public void Initialize()
    {
        vfxDataMap = new Dictionary<System.Type, BuffVFXMapping>();

        foreach (var mapping in vfxMappings)
        {
            // ���ڿ� Ŭ���� �̸��� ���� Type���� ��ȯ
            System.Type buffType = System.Type.GetType(mapping.exactBuffClassName);

            if (buffType == null)
            {
                Debug.LogWarning($"���� ����Ʈ �����ͺ��̽��� : {mapping.exactBuffClassName}�� ����");
                continue;
            }
            if (mapping.vfxPrefab == null)
            {
                Debug.LogWarning($"���� ����Ʈ �����ͺ��̽��� : {mapping.vfxPrefab}�� ����");
                continue;
            }
            if (vfxDataMap.ContainsKey(buffType))
            {
                Debug.Log($"[BuffVFXDatabase] �ߺ��� ���� Ÿ���� �����մϴ�. : {mapping.exactBuffClassName}");
                continue;
            }

            vfxDataMap.Add(buffType, mapping);

            // Ǯ ����
            if (ObjectPoolManager.Instance != null)
            {
                string poolTag = mapping.exactBuffClassName;
                int poolSize = mapping.initialPoolSize > 0 ? mapping.initialPoolSize : 5;
                ObjectPoolManager.Instance.CreatePool(poolTag, mapping.vfxPrefab, poolSize);
            }
            else
            {
                Debug.Log("ObjectPoolManager �ʱ�ȭ���� ����");
            }

        }
    }

    public bool TryGetVFXData(System.Type buffType, out BuffVFXMapping data)
    {
        if (vfxDataMap == null)
        {
            Debug.LogError("[BuffVFXDatabase] db�� �ʱ�ȭ���� �ʾ���");
            data = default;
            return false;
        }

        return vfxDataMap.TryGetValue(buffType, out data);
    }
}
