using UnityEngine;

// UnitEntity의 자식 오브젝트로 오며, 본체 콜라이더 충돌 이벤트를 처리 후 부모에게 전달한다.
[RequireComponent(typeof(Collider))]
public class BodyColliderController : MonoBehaviour
{
    [SerializeField] private UnitEntity owner;
    [SerializeField] private Collider bodyCollider;

    public UnitEntity ParentUnit { get; private set; }
    public Collider BodyCollider => bodyCollider;

    void Awake()
    {
        // 부모에서 UnitEntity 컴포넌트를 찾아 소유자로 설정
        if (owner == null)
        {
            owner = GetComponentInParent<UnitEntity>();
        }
        if (bodyCollider == null)
        {
            bodyCollider = GetComponent<Collider>();
        }

        ParentUnit = GetComponentInParent<UnitEntity>();
    }

    // 이 컨트롤러의 콜라이더 활성화 상태 결정
    public void SetState(bool enabled)
    {
        if (bodyCollider != null)
        {
            bodyCollider.enabled = enabled;

            // 콜라이더가 켜지는 순간에는 수동 겹침 검사 실행
            if (enabled)
            {
                CheckForInitialOverlaps();
            }
        }
    }

    // 콜라이더가 활성화된 시점에 겹쳐져 있는 콜라이더를 찾아 `OnTriggerEnter`처럼 owner에게 전달한다.
    private void CheckForInitialOverlaps()
    {
        if (owner == null) return;

        // 콜라이더의 타입을 확인해 Overlap 함수를 사용한다.
        if (bodyCollider is BoxCollider box)
        {
            // BoxCollider와 충돌하는 콜라이더들을 찾음
            Collider[] overlappingColliders = Physics.OverlapBox(
                transform.position + box.center,
                Vector3.Scale(box.size, transform.lossyScale) / 2, // 스케일링을 고려한 실제 크기
                transform.rotation,
                -1, // 모든 레이어
                QueryTriggerInteraction.Collide // 트리거 콜라이더와도 충돌하도록 설정
            );

            foreach (var otherCollider in overlappingColliders)
            {
                // 자기 자신은 무시
                if (otherCollider == bodyCollider) return;

                // 감지된 콜라이더를 owner에게 전달
                owner.OnBodyTriggerEnter(otherCollider);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        owner?.OnBodyTriggerEnter(other);
    }

    private void OnTriggerExit(Collider other)
    {
        owner?.OnBodyTriggerExit(other);
    }
}
