using System.Collections.Generic;
using UnityEngine;


// �׸��� �������� ���� ����� (0, 0)�̸�, 
// ���� �����ʰ� �Ʒ��� ���� + �����Դϴ�.
public static class PositionCalculationSystem
{
    // ���� ������ ������ ��, ȸ���� ���� ��ȭ�ϴ� �׸��� ������ ���� ����ϴ�.
    public static Vector2Int RotateGridOffset(Vector2Int offset, Vector3 direction)
    {
        if (direction == Vector3.left) return offset;
        if (direction == Vector3.right) return new Vector2Int(-offset.x, -offset.y);
        if (direction == Vector3.forward) return new Vector2Int(-offset.y, offset.x); // 2����(y���)���� ���� ����
        if (direction == Vector3.back) return new Vector2Int(offset.y, -offset.x); // 2���� ���� �Ʒ���
        return offset;
    }

    public static HashSet<Vector2Int> CalculateRange(IEnumerable<Vector2Int> offsets, Vector2Int center, Vector3 facingDirection)
    {
        HashSet<Vector2Int> skillRange = new HashSet<Vector2Int>();

        foreach (Vector2Int offset in offsets)
        {
            Vector2Int rotatedOffset = RotateGridOffset(offset, facingDirection);
            skillRange.Add(center + rotatedOffset);   
        }

        return skillRange;
    }
}