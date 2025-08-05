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
    private UnitEntity? attacker; // ���ư��� �߿� �ı��� �� ����
    private UnitEntity? target; // ���ư��� �߿� �ı��� �� ����
    private Vector3 lastKnownPosition = new Vector3(0f, 0f, 0f); // ���������� �˷��� ���� ��ġ
    private string poolTag = string.Empty;
    private string hitEffectTag = string.Empty;
    private GameObject hitEffectPrefab = default!;
    private bool shouldDestroy = false;
    private AttackType attackType;

    private VisualEffect? vfx;
    private Vector3 vfxBaseDirection = new Vector3(0f, 0f, 0f);

    // VFX���� ��ü ȸ���� ���� ������ ���
    private float rotationSpeed = 360f; // �ʴ� ȸ�� ���� (�� ����)
    private float currentRotation = 0f;

    public void Initialize(UnitEntity attacker,
        UnitEntity target, 
        float value, 
        bool showValue, 
        string poolTag,
        GameObject hitEffectPrefab, 
        string hitEffectTag,
        AttackType attackType)
    {
        // ������(attacker)�� ���(target)�� Initialize �޼����� ���ڷ� �ݵ�� ���޵ǹǷ� null�� �� ���ٰ� ������ �� �ֽ��ϴ�.
        // ���� null Ȯ�� ���� �ٷ� Faction�� ���� �� �ֽ��ϴ�.
        if (attacker.Faction == target.Faction)
        {
            isHealing = true;
            this.showValue = true;
        }
        UnSubscribeFromEvents();

        this.attacker = attacker;
        this.target = target;
        this.value = value;
        this.showValue = showValue;
        this.poolTag = poolTag;
        this.attackType = attackType;
        lastKnownPosition = target.transform.position;
        shouldDestroy = false;

        this.hitEffectPrefab = hitEffectPrefab;
        this.hitEffectTag = hitEffectTag;

        vfx = GetComponentInChildren<VisualEffect>();

        if (vfx != null)
        {
            InitializeVFXDirection(); // ������ �ִ� VFX�� �ʱ� ������ ������
            vfx.Play();
        }

        // �� ������ target, attacker�� null�� �ƴ�
        target.OnDeathAnimationCompleted += OnTargetDestroyed;
        attacker.OnDeathAnimationCompleted += OnAttackerDestroyed;
     
        // �����ڿ� ����� ���ٸ� ���� ����
        //if (attacker.Faction == target.Faction)
        //{
        //    isHealing = true;
        //    this.showValue = true;
        //}
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
        if (target != null && attacker != null)
        {

            AttackSource attackSource = new AttackSource(
                attacker: attacker,
                position: transform.position,
                damage: value,
                type: attackType,
                isProjectile: true,
                hitEffectPrefab: hitEffectPrefab,
                hitEffectTag: hitEffectTag
            );

            // �� ��Ȳ
            if (isHealing)
            {
                // �� ����Ʈ�� �ǰ� ����Ʈ�� �����ϰ���
                target.TakeHeal(attackSource);
            }

            // ���� ���� ��Ȳ
            else if (attacker is Operator op && op.OperatorData.operatorClass == OperatorData.OperatorClass.Artillery)
            {
                CreateAreaOfDamage(transform.position, value, showValue, attackSource);
            }

            // ���� ����
            else
            {
                // ������� ������ �ϴ� ��쿡�� ������
                if (showValue == true)
                {
                    ObjectPoolManager.Instance!.ShowFloatingText(target.transform.position, value, false);
                }

                target.TakeDamage(attackSource);

            }
        }

        // �����ڰ� ������ų�, Ǯ�� ���� ������ ���
        if (shouldDestroy)
        {
            Destroy(gameObject);
        }
        else
        {
            ObjectPoolManager.Instance!.ReturnToPool(poolTag, gameObject);
        }
    }
    
    // ���� ������ �ϴ� ��� ����Ʈ�� �ݶ��̴��� ������
    private void CreateAreaOfDamage(Vector3 position, float damage, bool showValue, AttackSource attackSource)
    {
        // ���� ���� ����Ʈ ����
        if (hitEffectPrefab != null)
        {
            GameObject effectInstance = Instantiate(hitEffectPrefab, position, Quaternion.identity);
            Destroy(effectInstance, 2f); // 2�� �Ŀ� ����Ʈ ����. �Ʒ��� �ڵ�� �̰� ��ٸ��� �ʰ� �� ������.
        }

        // ���� ���� ��󿡰� ����� ����
        Collider[] hitColliders = Physics.OverlapSphere(position, 0.5f);
        
        foreach (var hitCollider in hitColliders)
        {
            BodyColliderController targetCollider = hitCollider.GetComponent<BodyColliderController>();
            if (targetCollider != null)
            {
                UnitEntity target = targetCollider.GetComponentInParent<UnitEntity>();
                if (target.Faction != Faction.Neutral &&
                    target.Faction != attacker!.Faction)
                {
                    if (showValue)
                    {
                        ObjectPoolManager.Instance!.ShowFloatingText(target.transform.position, damage, false);
                    }

                    // �ǰ� ����Ʈ�� �������� ����
                    target.TakeDamage(source: attackSource, playGetHitEffect: false);
                }
            }
        }
    }

    private void OnTargetDestroyed(UnitEntity unit)
    {
        if (target != null)
        {
            lastKnownPosition = target.transform.position;
            target.OnDeathAnimationCompleted -= OnTargetDestroyed;
            target = null;
        }
    }

    private void OnAttackerDestroyed(UnitEntity unit)
    {
        if (attacker != null)
        {
            shouldDestroy = true;
            attacker.OnDeathAnimationCompleted -= OnAttackerDestroyed;
            attacker = null;
        }
    }

    private void UnSubscribeFromEvents()
    {
        if (target != null)
        {
            target.OnDeathAnimationCompleted -= OnTargetDestroyed;
        }
        if (attacker != null)
        {
            attacker.OnDeathAnimationCompleted -= OnAttackerDestroyed; 
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
