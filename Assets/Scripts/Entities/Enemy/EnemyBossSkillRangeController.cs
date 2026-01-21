using UnityEngine;

public class EnemyBossSkillRangeController : MonoBehaviour
{
    [SerializeField] private SphereCollider skillRangeCollider = default!;
    private EnemyBoss owner;

    public void Initialize(EnemyBoss enemy)
    {
        owner = enemy;
        EnemyBossData bossData = owner.BossData;

        if (skillRangeCollider == null)
        {
            skillRangeCollider = GetComponent<SphereCollider>();
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
                owner.OnTargetEnteredSkillRange(deployable);
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
                owner.OnTargetExitedSkillRange(deployable);
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
                    skillRangeCollider, transform.position, transform.rotation, // 이 콜라이더의 정보
                    targetCollider, targetCollider.transform.position, targetCollider.transform.rotation, // 타겟 콜라이더의 정보
                    out Vector3 direction, out float dist // 출력 변수 : 사용하지 않음
                );

                if (isOverlapping)
                {
                    owner.OnTargetEnteredSkillRange(target);
                }
            }
        }
    }

    private void OnDisable()
    {
        DeployableUnitEntity.OnDeployed -= HandleNewlyDeployed;
    }
}