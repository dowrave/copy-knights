
using UnityEngine;

// ���� ����Ʈ ȿ���� ���� �����ͺ��̽�
public class BuffVFXManager : MonoBehaviour
{
    public static BuffVFXManager? Instance { get; private set; }

    [SerializeField] private BuffVFXDatabase? VFXDatabase;

    // ObjectPoolManager���� �ʰ� �ʱ�ȭ�Ǿ�� �ϹǷ� Start�� �����ص�
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

    // ���� VFX ������Ʈ�� ������Ʈ Ǯ���� ����, Ÿ���� ��ġ�� ����
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
                    worldPositionStays: false // �θ� �������� ���� ��ǥ�踦 ���� ������
                    );

                vfxObj.transform.localPosition = vfxData.vfxOffset;

                return vfxObj;
            }
        }

        return null;
    }

    // Ǯ�� ���� ����Ʈ�� ��ȯ�ϴ� �޼���
    public void ReleaseBuffVFXObject(Buff buff, GameObject VFXObj)
    {
        if (VFXDatabase == null || ObjectPoolManager.Instance == null || VFXObj == null) return;

        if (VFXDatabase.TryGetVFXData(buff.GetType(), out BuffVFXDatabase.BuffVFXMapping vfxData)) 
        {
            string poolTag = vfxData.exactBuffClassName;
            VFXObj.transform.SetParent(null); // �θ�-�ڽ� ���� ����
            ObjectPoolManager.Instance.ReturnToPool(poolTag, VFXObj);
        }

    }
}
