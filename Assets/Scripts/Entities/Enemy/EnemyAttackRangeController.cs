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
            attackRangeCollider.radius = enemyData.Stats.AttackRange;
        }
        else
        {
            // melee의 radius = 0으로 잡아도 매우 낮은 양수값을 지니는 것으로 보인다.
            // 저지되지 않아도 공격 대상을 선정하는 이슈가 있었다.
            // 콜라이더를 비활성화하는 게 답이다.
            attackRangeCollider.gameObject.SetActive(false);
        }

        DeployableUnitEntity.OnDeployed += HandleNewlyDeployed;
    }

    private void OnTriggerEnter(Collider other)
    {
        BodyColliderController targetCollider = other.GetComponent<BodyColliderController>();

        if (targetCollider != null && targetCollider.ParentUnit is DeployableUnitEntity deployable)
        {
            if (deployable != null && deployable.IsDeployed)
            {
                owner.OnTargetEnteredRange(deployable);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        BodyColliderController targetCollider = other.GetComponent<BodyColliderController>();

        if (targetCollider != null && targetCollider.ParentUnit is DeployableUnitEntity deployable)
        {
            if (deployable != null)
            {
                owner.OnTargetExitedRange(deployable);
            }
        }
    }

    // 공격 범위 내에 새롭게 배치된 요소가 있을 경우, 공격 범위 내에 넣을 수 있는지 체크
    private void HandleNewlyDeployed(DeployableUnitEntity target)
    {
        if (owner == null || !enabled) return;

        BodyColliderController targetColliderController = target.GetComponentInChildren<BodyColliderController>();
        if (targetColliderController != null)
        {
            Collider targetCollider = targetColliderController.BodyCollider;

            if (targetCollider != null)
            {
                // 두 콜라이더가 겹치는지 검사 수행
                bool isOverlapping = Physics.ComputePenetration(
                    attackRangeCollider, transform.position, transform.rotation, // 이 콜라이더의 정보
                    targetCollider, targetCollider.transform.position, targetCollider.transform.rotation, // 타겟 콜라이더의 정보
                    out Vector3 direction, out float dist // 출력 변수 : 사용하지 않음
                );

                if (isOverlapping)
                {
                    owner.OnTargetEnteredRange(target);
                }
            }
        }
    }

    private void OnDisable()
    {
        DeployableUnitEntity.OnDeployed -= HandleNewlyDeployed;
    }
}