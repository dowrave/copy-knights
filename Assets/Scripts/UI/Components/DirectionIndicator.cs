using UnityEngine;

// Operator�� ��ġ�� �� �ڽ� ������Ʈ�� �ٴ� ���� �̹���
public class DirectionIndicator : MonoBehaviour
{
    public void Initialize(Operator op)
    {
        // ��ġ ����
        transform.position = op.transform.position + Vector3.down * 0.25f;

        // ���� ����
        float zRot = 0f;
        if (op.FacingDirection == Vector3.left) zRot = 0;
        else if (op.FacingDirection == Vector3.right) zRot = 180;
        else if (op.FacingDirection == Vector3.forward) zRot = -90;
        else if (op.FacingDirection == Vector3.back) zRot = 90;

        // �θ� ������Ʈ�� ������ ���� �ʴ�, ���� ������ rotation ��
        transform.rotation = Quaternion.Euler(90, 0, zRot);
    }

}
