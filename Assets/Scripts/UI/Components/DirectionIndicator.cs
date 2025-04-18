using UnityEngine;

// Operator가 배치될 때 자식 오브젝트로 붙는 방향 이미지
public class DirectionIndicator : MonoBehaviour
{
    public void Initialize(Operator op)
    {
        // 위치 설정
        transform.position = op.transform.position + Vector3.down * 0.25f;

        // 방향 설정
        float zRot = 0f;
        if (op.FacingDirection == Vector3.left) zRot = 0;
        else if (op.FacingDirection == Vector3.right) zRot = 180;
        else if (op.FacingDirection == Vector3.forward) zRot = -90;
        else if (op.FacingDirection == Vector3.back) zRot = 90;

        // 부모 오브젝트에 영향을 받지 않는, 월드 기준의 rotation 값
        transform.rotation = Quaternion.Euler(90, 0, zRot);
    }

}
