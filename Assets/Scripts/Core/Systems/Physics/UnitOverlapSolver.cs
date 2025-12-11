using UnityEngine;


// 다른 유닛과 겹칠 때 살짝씩 밀어내는 로직 구현
public class UnitOverlapSolver: MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] protected bool isStaticUnit = false; // true시 밀려나지 않음
    [SerializeField] protected float bodyRadius = 0.01f; // 충돌 반경
    [SerializeField] protected float separationSpeed = 5f; // 밀어내는 힘의 세기
    [SerializeField] protected LayerMask unitLayer; // 감지할 레이어 - 각 유닛의 메인 콜라이더 Unit 레이어 지정했음

    private Collider[] hitColliders = new Collider[20];
    private int hitCount;

    private void LateUpdate()
    {
        if (isStaticUnit) return;

        ResolveOverlap();
    }

    private void ResolveOverlap()
    {
        // 주변 유닛 탐색
        // OverlapSphere과 비교했을 때, 메모리를 반복하면서 새로 할당하지 않기 때문에 가비지 컬렉션이 발생하지 않음
        hitCount = Physics.OverlapSphereNonAlloc(transform.position, bodyRadius * 2, hitColliders, unitLayer);

        Vector3 separationVector = Vector3.zero; 
        int separationCount = 0;

        for (int i = 0; i < hitCount; i++)
        {
            Collider other = hitColliders[i];

            // 상대 콜라이더는 켜져 있어야 함
            if (!other.enabled) continue;

            // 유닛 콜라이더는 자식 오브젝트로 둠 -> 부모에서 이 스크립트를 찾아야 함
            UnitOverlapSolver otherUnit = other.GetComponentInParent<UnitOverlapSolver>();

            if (otherUnit.gameObject == gameObject) continue;

            if (otherUnit != null)
            {
                // 상대가 배치 요소인데 미리보기 모드일 때는 처리하지 않음
                if (CheckConditionAboutDeployable(otherUnit)) continue;
                if (CheckConditionAboutEnemy(otherUnit)) continue;

                // Logger.Log($"다른 오브젝트 {otherUnit.gameObject.name}를 찾아서 충돌 로직 계산 시작");

                // 위치 계산 - 
                Vector3 direction = transform.position - other.transform.position; 
                float distance = direction.magnitude;

                // 겹치지 않은 경우 다음 유닛으로 진행
                float combinedRadius = bodyRadius + otherUnit.bodyRadius;
                if (distance >= combinedRadius) continue;

                // 거리가 0이면 랜덤 방향 설정
                if (distance == 0f)
                {
                    direction = Random.insideUnitSphere;
                    direction.y = 0;
                }

                direction.Normalize();

                // 상대에 따른 처리
                if (otherUnit.isStaticUnit)
                {
                    // 상대가 고정 유닛이면 나만 100% 힘으로 밀려남
                    separationVector += direction * (combinedRadius - distance) * 2f;
                }
                else
                {
                    // 상대가 이동 유닛이면 서로 밀려남(상대도 나에 대한 밀어내는 동작이 실행됨)
                    separationVector += direction;
                }

                separationCount++;
            }
        }

        // 밀어낼 힘이 있다면 위치 적용
        if (separationCount > 0)
        {
            // 부드럽게 밀어내기 - 떨림 방지를 위한 Time.deltaTime 적용
            Vector3 moveAmount = separationVector.normalized * separationSpeed * Time.deltaTime;
            moveAmount.y = 0;

            transform.position += moveAmount;
        }
    }

    // 충돌한 상대가 Deployable일때 조건 체크
    private bool CheckConditionAboutDeployable(UnitOverlapSolver otherUnit)
    {
        DeployableUnitEntity otherDeployable = otherUnit.GetComponent<DeployableUnitEntity>();
        if (otherDeployable != null && otherDeployable.IsPreviewMode) return true; // 미리보기 중에는 이 스크립트로 인한 처리 진행 X
        return false;
    }

    // Enemy의 경우, 저지당할 때만 동작함
    // 이동 중에도 동작하게끔 구현하면 멈춰있는 오브젝트 A의 위치로 이동하는 오브젝트 B가 진입할 때 B가 A를 밀어넣어버리는 현상이 발생함
    private bool CheckConditionAboutEnemy(UnitOverlapSolver otherUnit)
    {
        Enemy thisEnemy = GetComponent<Enemy>();
        Enemy otherEnemy = otherUnit.GetComponent<Enemy>();
        
        // 스크립트의 동작 조건 : 두 Enemy가 모두 저지당한 상태에서만 동작
        if (thisEnemy != null && 
            otherEnemy != null && 
            thisEnemy.BlockingOperator != null && 
            otherEnemy.BlockingOperator != null)
        {
            return true; 
        }

        return false;
    }
}