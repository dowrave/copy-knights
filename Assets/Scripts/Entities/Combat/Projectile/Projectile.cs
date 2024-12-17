using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ����ü ���� Ŭ����
public class Projectile : MonoBehaviour
{
    public float speed = 10f;
    private float value;
    private bool showValue;
    private AttackType attackType;
    private bool isHealing = false;
    private UnitEntity attacker;
    private UnitEntity target;
    private Vector3 lastKnownPosition; // ���������� �˷��� ���� ��ġ
    private string poolTag;
    public string PoolTag { get; private set; }
    private bool shouldDestroy;
    private GameObject hitEffectPrefab;
    public void Initialize(UnitEntity attacker,
        UnitEntity target, 
        AttackType attackType, 
        float value, 
        bool showValue, 
        string poolTag,
        GameObject hitEffectPrefab)
    {
        UnSubscribeFromEvents();

        this.attacker = attacker;
        this.target = target;
        this.attackType = attackType;
        this.value = value;
        this.showValue = showValue;
        this.poolTag = poolTag;
        this.hitEffectPrefab = hitEffectPrefab;
        lastKnownPosition = target.transform.position;
        shouldDestroy = false;

        target.OnDestroyed += OnTargetDestroyed;
        attacker.OnDestroyed += OnAttackerDestroyed;

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

        // ���������� �˷��� ��ġ�� �̵�
        Vector3 direction = (lastKnownPosition - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;

        // ��ǥ ���� ���� Ȯ��
        if (Vector3.Distance(transform.position, lastKnownPosition) < 0.1f)
        {
            OnReachTarget();
        }
    }

    /// <summary>
    /// ��ǥ ��ġ�� ���� �ÿ� ����
    /// </summary>
    private void OnReachTarget()
    {
        // Ÿ���� ����ִ� ���
        if (target != null)
        {
            if (isHealing)
            {
                // �� ����Ʈ�� �ǰ� ����Ʈ�� �����ϰ���
                target.TakeHeal(value, attacker, hitEffectPrefab);
            }
            else
            {
                // ������� ������ �ϴ� ��쿡�� ������
                if (showValue == true)
                {
                    ObjectPoolManager.Instance.ShowFloatingText(target.transform.position, value, false);
                }

                target.TakeDamage(attackType, value, attacker, hitEffectPrefab);

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

    private void OnTargetDestroyed()
    {
        if (target != null)
        {
            lastKnownPosition = target.transform.position;
            target.OnDestroyed -= OnTargetDestroyed;
            target = null; 
        }
    }

    private void OnAttackerDestroyed()
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

    // Ǯ���� ����� �� ȣ��� �޼���
    private void OnDisable()
    {
        UnSubscribeFromEvents();

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
