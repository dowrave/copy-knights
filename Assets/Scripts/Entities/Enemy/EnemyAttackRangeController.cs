using UnityEngine;

public class EnemyAttackRangeController : MonoBehaviour
{
    [SerializeField] private SphereCollider attackRangeCollider = default!;
    private Enemy owner;

    public void Initialize(Enemy enemy)
    {
        owner = enemy;
        EnemyData enemyData = owner.BaseData;

        if (attackRangeCollider == null)
        {
            attackRangeCollider = GetComponent<SphereCollider>();
        }

        // 콜라이더 = 공격 범위 반경 설정
        if (enemyData.AttackRangeType == AttackRangeType.Ranged)
        {
            attackRangeCollider.radius = enemyData.stats.AttackRange;
        }
        else
        {
            attackRangeCollider.radius = 0;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        DeployableUnitEntity target = other.GetComponent<DeployableUnitEntity>();

        if (target != null)
        {
            owner.OnTargetEnteredRange(target);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        DeployableUnitEntity target = other.GetComponent<DeployableUnitEntity>();
        if (target != null)
        {
            owner.OnTargetExitedRange(target);
        }
    }
}