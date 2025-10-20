using UnityEngine;
using UnityEngine.VFX;
using static ICombatEntity;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// ����ü ���� Ŭ����
public class Projectile : MonoBehaviour
{
    // ����Ʈ �Ҵ�
    [Header("VFX")]
    [SerializeField] private VisualEffect? vfxGraph;
    [SerializeField] private ParticleSystem? mainParticle;
    [SerializeField] private List<ParticleSystem> remainingParticles; // mainParticle�� ��������� ǥ�õǴ� ��ƼŬ

    [Header("Check if VFX needs Rotation")]
    [SerializeField] private bool needToRotate; // ������ ������ ��ƼŬ�� ��� üũ

    [Header("Speed")]
    public float speed = 5f;

    [Header("Wait Time Before VFX Stop")]
    // �ı��ǰų� Ǯ�� ���ư��� �� ��� �ð� - ����Ʈ�� �ٷ� ������� �ʰ� �ؼ� ���� ������� �ʰԲ� ��
    [SerializeField] private float WAIT_DISAPPEAR_TIME = 0.5f;

    [Header("Mesh Renderers")]
    [SerializeField] private List<MeshRenderer> renderers;

    private float value; // ����� or ����
    private bool showValue;
    private bool isHealing = false;
    private UnitEntity? attacker; // ���ư��� �߿� �ı��� �� ����
    private UnitEntity? target; // ���ư��� �߿� �ı��� �� ����
    private Vector3 lastKnownPosition = new Vector3(0f, 0f, 0f); // ���������� �˷��� ���� ��ġ
    private string poolTag = string.Empty;
    private string hitEffectTag = string.Empty;
    private bool shouldDestroy = false;
    private AttackType attackType;

    private Vector3 vfxBaseDirection = new Vector3(0f, 0f, 0f);

    private bool isReachedTarget = false;

    private SphereCollider sphereCollider;



    // VFX���� ��ü ȸ���� ���� ������ ���
    private float rotationSpeed = 360f; // �ʴ� ȸ�� ���� (�� ����)
    private float currentRotation = 0f;

    private void Awake()
    {
        // �� �� ���� �� ã�ƺ�
        if (vfxGraph == null && mainParticle == null)
        {
            mainParticle = GetComponentInChildren<ParticleSystem>();
            if (mainParticle == null)
            {
                vfxGraph = GetComponentInChildren<VisualEffect>();
            }
        }

        if (sphereCollider == null)
        {
            sphereCollider = GetComponent<SphereCollider>();
        }
    }

    public void Initialize(UnitEntity attacker,
        UnitEntity target,
        float value,
        bool showValue,
        string poolTag,
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

        this.hitEffectTag = hitEffectTag;

        // �޽��� ������ ��Ұ� �ִٸ� Ȱ��ȭ
        if (renderers != null)
        {
            foreach (MeshRenderer renderer in renderers)
            {
                renderer.enabled = true;
            }
        }

        if (needToRotate)
        {
            InitializeVFXDirection(); // ������ �ִ� VFX�� �ʱ� ������ ������
        }
        isReachedTarget = false;

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

        if (isReachedTarget) return;

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

        // ȸ�� ���� �ݿ�
        if (needToRotate)
        {
            UpdateVFXDirection(direction);
        }

        // ��ǥ ���� ���� Ȯ��
        // if (Vector3.Distance(transform.position, lastKnownPosition) < 0.1f)
        // ���� Ÿ���� ������� ������ �����
        if (target == null)
        {
            float reachDistance = sphereCollider.radius;
            if ((transform.position - lastKnownPosition).sqrMagnitude < reachDistance * reachDistance) // �̰� �� �����ܴ�
            {
                HandleHit(lastKnownPosition);
            }
        }
    }

    // ������ �ִ� VFX�� �ʱ� ���� ����
    private void InitializeVFXDirection()
    {
        if (vfxGraph != null && vfxGraph.HasVector3("BaseDirection"))
        {
            vfxBaseDirection = vfxGraph.GetVector3("BaseDirection").normalized;

            // �ʱ� ���� ���
            Vector3 initialDirection = (lastKnownPosition - transform.position).normalized;

            // �⺻ -> ��ǥ ���������� ȸ�� ����ؼ� VFX�� ����
            Quaternion rotation = Quaternion.FromToRotation(vfxBaseDirection, initialDirection);
            Vector3 eulerAngles = rotation.eulerAngles;

            // VFX�� �ʱ� ȸ�� ����
            if (vfxGraph.HasVector3("EulerAngle"))
            {
                vfxGraph.SetVector3("EulerAngle", eulerAngles);
            }
            if (vfxGraph.HasVector3("FlyingDirection"))
            {
                vfxGraph.SetVector3("FlyingDirection", initialDirection);
            }

            vfxGraph.Play();
        }
        else
        {
            // Update�� ���յǾ� ������ �ʱ�ȭ������ �� �� ����ְ���
            Vector3 direction = (lastKnownPosition - transform.position).normalized;
            Quaternion objectRotation = Quaternion.LookRotation(direction); // ����Ʈ�� +Z���� ���Ѵٰ� ����
            transform.rotation = objectRotation;

            mainParticle.Play(true);
        }
    }


    // ���� ���͸� �޾� VFX�� ���Ϸ� ������ ��ȯ�� �����Ѵ�
    private void UpdateVFXDirection(Vector3 directionVector)
    {
        if (vfxGraph != null)
        {
            if (vfxGraph.HasVector3("FlyingDirection"))
            {
                vfxGraph.SetVector3("FlyingDirection", directionVector);
            }

            // ����Ʈ�� ���⿡ ���� ȸ��
            if (vfxBaseDirection != null)
            {
                Quaternion directionRotation = Quaternion.FromToRotation(vfxBaseDirection, directionVector);
                Vector3 eulerAngles = directionRotation.eulerAngles;

                // ��ü���� ȸ���� ���� ����Ʈ���
                if (vfxGraph.HasBool("SelfRotation"))
                {
                    currentRotation += rotationSpeed * Time.deltaTime;
                    currentRotation %= 360f; // 360���� ���� �ʴ� ����ȭ

                    // ���� ������ ������ �ϴ� ��ü ȸ��
                    Quaternion axialRotation = Quaternion.AngleAxis(currentRotation, directionVector);

                    // ȸ�� ���� (���� ������ �߿���)
                    Quaternion finalRotation = axialRotation * directionRotation;
                    eulerAngles = finalRotation.eulerAngles;
                }


                if (vfxGraph.HasVector3("EulerAngle"))
                {
                    vfxGraph.SetVector3("EulerAngle", eulerAngles);
                }
            }
        }
        else if (mainParticle != null)
        {
            Vector3 direction = (lastKnownPosition - transform.position).normalized;
            Quaternion objectRotation = Quaternion.LookRotation(direction); // �׽�Ʈ
            transform.rotation = objectRotation;
        }
    }

    // ��ǥ ��ġ�� ���� �ÿ� ����
    private void HandleHit(Vector3 hitPosition)
    {
        if (isReachedTarget) return;
        isReachedTarget = true;

        // Ÿ���� ����ִ� ���
        if (target != null && attacker != null)
        {
            AttackSource attackSource = new AttackSource(
                attacker: attacker,
                // position: transform.position,
                position: hitPosition,
                damage: value,
                type: attackType,
                isProjectile: true,
                hitEffectTag: hitEffectTag,
                showDamagePopup: showValue
            );


            if (isHealing)
            {
                // �� ��Ȳ
                target.TakeHeal(attackSource);
            }
            else if (attacker is Operator op && op.OperatorData.operatorClass == OperatorData.OperatorClass.Artillery)
            {
                // ���� ���� ��Ȳ
                CreateAreaOfDamage(transform.position, value, showValue, attackSource);
            }
            else
            {
                // ���� ����
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
            Destroy(gameObject, WAIT_DISAPPEAR_TIME);
        }
        else
        {
            StartCoroutine(ReturnToPoolAfterSeconds(WAIT_DISAPPEAR_TIME));
        }
    }

    private IEnumerator ReturnToPoolAfterSeconds(float seconds)
    {

        if (vfxGraph != null)
        {
            vfxGraph.Reinit();
        }
        else if (mainParticle != null)
        {
            // �θ�-�ڽ� ���踦 �Ͻ������� ��� �θ� ��ƼŬ�� ������ ���絵 �ڽ� ��ƼŬ�� ��� ����ǰ� ��
            foreach (var ps in remainingParticles)
            {
                ps.transform.parent = null;
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting); // ��ƼŬ�� �߰� ������ ����
                // ps.Play() // ���� �ʿ� ��� �ּ� ó���غ�
            }

            // ������ �ʾƵ� �Ǵ� ��ƼŬ�� ��� ����
            mainParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            // �޽� �������� ������ ��ҵ��� �ִٸ� �̵��� ��� ��Ȱ��ȭ
            if (renderers != null)
            {
                foreach (MeshRenderer renderer in renderers)
                {
                    renderer.enabled = false;
                }
            }
        }

        yield return new WaitForSeconds(seconds);

        // �ٽ� �θ� ��ƼŬ�� �Ҵ�
        foreach (var ps in remainingParticles)
        {
            ps.transform.parent = mainParticle.transform;
        }

        ObjectPoolManager.Instance!.ReturnToPool(poolTag, gameObject);
    }

    // ���� ������ �ϴ� ��� ����Ʈ�� �ݶ��̴��� ������
    private void CreateAreaOfDamage(Vector3 position, float damage, bool showValue, AttackSource attackSource)
    {
        // ���� ���� ����Ʈ ����
        if (hitEffectTag != null)
        {
            GameObject effectInstance = ObjectPoolManager.Instance.SpawnFromPool(hitEffectTag, position, Quaternion.identity);
            // GameObject effectInstance = Instantiate(hitEffectPrefab, position, Quaternion.identity);
            // Destroy(effectInstance, 2f); // 2�� �Ŀ� ����Ʈ ����. �Ʒ��� �ڵ�� �̰� ��ٸ��� �ʰ� �� ������.
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
                    target.TakeDamage(source: attackSource);
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
        vfxGraph = GetComponentInChildren<VisualEffect>();
    }

    // Ǯ���� ����� �� ȣ��� �޼���
    private void OnDisable()
    {
        UnSubscribeFromEvents();

        if (vfxGraph != null)
        {
            vfxGraph.Stop();
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
    
    private void OnTriggerEnter(Collider other)
    {
        // �̹� ��ǥ�� �����߰ų�, Ÿ���� ������ ����
        if (isReachedTarget || target == null) return;

        // �浹�� ������Ʈ�� �� Ÿ������ Ȯ��
        BodyColliderController hitUnitCollider = other.GetComponent<BodyColliderController>();
        
        if (hitUnitCollider != null && // �ٸ� �ݶ��̴��� �Ÿ�
            hitUnitCollider.ParentUnit == target)
        {
            // OnReachTarget() ��� ���ο� ���� �Լ� ȣ��
            HandleHit(target.transform.position);
        }
        
    }
}
