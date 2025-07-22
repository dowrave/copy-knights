
using UnityEngine;

// CC 효과를 위한 매니저 클래스
public class BuffEffectManager : MonoBehaviour
{
    public static BuffEffectManager? Instance { get; private set; }

    [SerializeField] private BuffEffectDatabase? effectDatabase;

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

    public GameObject? CreateBuffVFXObject(Buff buff, Transform target)
    {
        if (effectDatabase == null) return null;

        GameObject? prefab = effectDatabase.GetEffectPrefab(buff.GetType());
        if (prefab != null)
        {
            GameObject effectObj = Instantiate(prefab, target.position, Quaternion.identity, target);
            return effectObj;
        }
        return null;
    }
}
