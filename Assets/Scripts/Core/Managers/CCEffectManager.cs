
using UnityEngine;

public class CCEffectManager : MonoBehaviour
{
    public static CCEffectManager? Instance { get; private set; }

    [SerializeField] private CCEffectDatabase? effectDatabase;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (effectDatabase != null)
            {
                effectDatabase.Initialize();
            }
        }
        else
        {
            Destroy(this);
        }
    }

    public GameObject? CreateCCVFXObject(CrowdControl cc, Transform target)
    {
        if (effectDatabase == null) return null;

        GameObject prefab = effectDatabase.GetEffectPrefab(cc.GetType());
        if (prefab != null)
        {
            GameObject effectObj = Instantiate(prefab, target.position, Quaternion.identity, target);
            return effectObj;
        }
        return null;
    }
}
