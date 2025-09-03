using System.Collections.Generic;
using UnityEngine;


// 그리드 포지션은 좌측 상단이 (0, 0)이며, 
// 각각 오른쪽과 아래로 갈때 + 방향입니다.
public static class PositionCalculationSystem
{
    // 기준 방향이 왼쪽일 때, 회전에 따라 변화하는 그리드 포지션 값을 얻습니다.
    public static Vector2Int RotateGridOffset(Vector2Int offset, Vector3 direction)
    {
        if (direction == Vector3.left) return offset;
        if (direction == Vector3.right) return new Vector2Int(-offset.x, -offset.y);
        if (direction == Vector3.forward) return new Vector2Int(-offset.y, offset.x); // 2차원(y평면)으로 보면 위쪽
        if (direction == Vector3.back) return new Vector2Int(offset.y, -offset.x); // 2차원 기준 아래쪽
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