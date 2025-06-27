using UnityEngine;

// UnitEntity의 자식 오브젝트로 오며, 본체 콜라이더 충돌 이벤트를 처리 후 부모에게 전달한다.
[RequireComponent(typeof(Collider))]
public class BodyColliderController : MonoBehaviour
{
    [SerializeField] private UnitEntity owner;
    [SerializeField] private Collider bodyCollider;

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
    }

    // 이 컨트롤러의 콜라이더 활성화 상태 결정
    public void SetColliderState(bool enabled)
    {
        if (bodyCollider != null)
        {
            bodyCollider.enabled = enabled;
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
