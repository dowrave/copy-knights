using UnityEngine;
using UnityEngine.VFX;
using static ICombatEntity;

// ����ü ���� Ŭ����
public class Projectile : MonoBehaviour
{
    public float speed = 5f;
    private float value; // ����� or ����
    private bool showValue;
    private bool isHealing = false;
    private UnitEntity attacker;
    private UnitEntity target;
    private Vector3 lastKnownPosition; // ���������� �˷��� ���� ��ġ
    private string poolTag;
    private GameObject hitEffectPrefab;
    public string PoolTag { get; private set; }
    private bool shouldDestroy;

    private VisualEffect vfx;
    private Vector3 vfxBaseDirection;

    // VFX���� ��ü ȸ���� ���� ������ ���
    private float rotationSpeed = 360f; // �ʴ� ȸ�� ���� (�� ����)
    private float currentRotation = 0f;

    public void Initialize(UnitEntity attacker,
        UnitEntity target, 
        float value, 
        bool showValue, 
        string poolTag,
        GameObject hitEffectPrefab)
    {
        UnSubscribeFromEvents();

        this.attacker = attacker;
        this.target = target;
        this.value = value;
        this.showValue = showValue;
        this.poolTag = poolTag;
        lastKnownPosition = target.transform.position;
        shouldDestroy = false;

        this.hitEffectPrefab = hitEffectPrefab;
        target.OnDestroyed += OnTargetDestroyed;
        attacker.OnDestroyed += OnAttackerDestroyed;

        vfx = GetComponentInChildren<VisualEffect>();

        if (vfx != null)
        {
            InitializeVFXDirection(); // ������ �ִ� VFX�� �ʱ� ������ ������
            vfx.Play();
        }

        // �����ڿ� ����� ���ٸ� ���� ����
        if (attacker.Faction == target.Faction)
        {
            isHealing = true;
            this.showValue = true;
        }
    }

    private void Update()
    {
        // �����ڰ� ������� ���� �������� ���� ����
        if (attacker == null && !shouldDestroy)
        {
            shouldDestroy = true; // ��ǥ ���� �Ŀ� �ı�
        }
        // Ÿ���� ��� �ִٸ� ��ġ ����
        if (target != null)
        {
            lastKnownPosition = target.transform.position;
        }

        // ���� ��� �� �̵�
        Vector3 direction = (lastKnownPosition - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;

        UpdateVFXDirection(direction);

        // ��ǥ ���� ���� Ȯ��
        if (Vector3.Distance(transform.position, lastKnownPosition) < 0.1f)
        {
            OnReachTarget();
        }
    }

    // ������ �ִ� VFX�� �ʱ� ���� ����
    private void InitializeVFXDirection() 
    {
        if (vfx != null && vfx.HasVector3("BaseDirection"))
        {
            vfxBaseDirection = vfx.GetVector3("BaseDirection").normalized;

            // �ʱ� ���� ���
            Vector3 initialDirection = (lastKnownPosition - transform.position).normalized;

            // �⺻ -> ��ǥ ���������� ȸ�� ����ؼ� VFX�� ����
            Quaternion rotation = Quaternion.FromToRotation(vfxBaseDirection, initialDirection);
            Vector3 eulerAngles = rotation.eulerAngles;

            // VFX�� �ʱ� ȸ�� ����
            if (vfx.HasVector3("EulerAngle"))
            {
                vfx.SetVector3("EulerAngle", eulerAngles);
            }
            if (vfx.HasVector3("FlyingDirection"))
            {
                vfx.SetVector3("FlyingDirection", initialDirection);
            }
        }
    }


    // ���� ���͸� �޾� VFX�� ���Ϸ� ������ ��ȯ�� �����Ѵ�
    private void UpdateVFXDirection(Vector3 directionVector)
    {
        if (vfx != null)
        {
            if (vfx.HasVector3("FlyingDirection"))
            {
                vfx.SetVector3("FlyingDirection", directionVector);
            }

            // ����Ʈ�� ���⿡ ���� ȸ��
            if (vfxBaseDirection != null)
            {
                Quaternion directionRotation = Quaternion.FromToRotation(vfxBaseDirection, directionVector);
                Vector3 eulerAngles = directionRotation.eulerAngles;

                // ��ü���� ȸ���� ���� ����Ʈ���
                if (vfx.HasBool("SelfRotation"))
                {
                    currentRotation += rotationSpeed * Time.deltaTime;
                    currentRotation %= 360f; // 360���� ���� �ʴ� ����ȭ

                    // ���� ������ ������ �ϴ� ��ü ȸ��
                    Quaternion axialRotation = Quaternion.AngleAxis(currentRotation, directionVector);

                    // ȸ�� ���� (���� ������ �߿���)
                    Quaternion finalRotation = axialRotation * directionRotation;
                    eulerAngles = finalRotation.eulerAngles;
                }


                if (vfx.HasVector3("EulerAngle"))
                {
                    vfx.SetVector3("EulerAngle", eulerAngles);
                }
            }
        }
    }

    // ��ǥ ��ġ�� ���� �ÿ� ����
    private void OnReachTarget()
    {
        // Ÿ���� ����ִ� ���
        if (target != null)
        {
            AttackSource attackSource = new AttackSource(transform.position, true, hitEffectPrefab);

            if (isHealing)
            {
                // �� ����Ʈ�� �ǰ� ����Ʈ�� �����ϰ���
                target.TakeHeal(attacker, attackSource, value);
            }
            else
            {
                // ������� ������ �ϴ� ��쿡�� ������
                if (showValue == true)
                {
                    ObjectPoolManager.Instance.ShowFloatingText(target.transform.position, value, false);
                }

                target.TakeDamage(attacker, attackSource, value);

            }
        }

        // �����ڰ� ������ų�, Ǯ�� ���� ������ ���
        if (shouldDestroy)
        {
            Destroy(gameObject);
        }
        else
        {
            ObjectPoolManager.Instance.ReturnToPool(poolTag, gameObject);
        }
    }

    private void OnTargetDestroyed(UnitEntity unit)
    {
        if (target != null)
        {
            lastKnownPosition = target.transform.position;
            target.OnDestroyed -= OnTargetDestroyed;
            target = null; 
        }
    }

    private void OnAttackerDestroyed(UnitEntity unit)
    {
        if (attacker != null)
        {
            shouldDestroy = true;
            attacker.OnDestroyed -= OnAttackerDestroyed;
            attacker = null;
        }
    }

    private void UnSubscribeFromEvents()
    {
        if (target != null)
        {
            target.OnDestroyed -= OnTargetDestroyed;
        }
        if (attacker != null)
        {
            attacker.OnDestroyed -= OnAttackerDestroyed; 
        }
    }

    private void OnEnable()
    {
        vfx = GetComponentInChildren<VisualEffect>();
    }

    // Ǯ���� ����� �� ȣ��� �޼���
    private void OnDisable()
    {
        UnSubscribeFromEvents();

        if (vfx != null)
        {
            vfx.Stop();
        }

        target = null;
        attacker = null;
        lastKnownPosition = Vector3.zero;
        shouldDestroy = false;
    }

    private void OnDestroy()
    {
        UnSubscribeFromEvents();
    }
}
