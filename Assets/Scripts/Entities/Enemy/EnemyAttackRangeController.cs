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
            // melee의 radius = 0으로 잡아도 매우 낮은 양수값을 지니는 것으로 보인다.
            // 저지되지 않아도 공격 대상을 선정하는 이슈가 있었다.
            // 콜라이더를 비활성화하는 게 답이다.
            attackRangeCollider.gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        DeployableUnitEntity target = other.GetComponent<DeployableUnitEntity>();

        if (target != null)
        {
            Debug.Log($"[ENEMY SPHERE] {owner.name}의 공격 범위(Sphere)에 {target.name}가 들어옴");

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