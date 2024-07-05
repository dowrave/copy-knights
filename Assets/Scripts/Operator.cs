using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Operator : Unit
{
    [SerializeField] // 필드 직렬화, Inspector에서 이 필드 숨기기
    public OperatorData data;

    [SerializeField, HideInInspector]
    public Vector3 facingDirection = Vector3.left;


    public int currentBlockedEnemies; // 현재 저지 수
    public int deploymentOrder { get; private set; } // 배치 순서

    [HideInInspector] public bool isBlocking = false; // 저지 중인가

    private void Start()
    {
        base.Initialize(data.baseStats);
    }

    public Vector2Int[] GetAttackableTiles()
    {
        return data.attackableTiles;
    }
    
    public void SetFacingDirection(Vector3 direction)
    {
        facingDirection = direction;
    }

    public override bool CanAttack(Vector3 targetPosition)
    {
        Vector2Int relativePosition = WorldToRelativeGridPosition(targetPosition);
        return System.Array.Exists(data.attackableTiles, tile => tile == relativePosition);
    }

    private Vector2Int WorldToRelativeGridPosition(Vector3 worldPosition)
    {
        // 월드 좌표 -> 상대적 그리드 좌표로 변환하는 로직
        return Vector2Int.zero;
    }
    

    public void SetDeploymentOrder(int order)
    {
        // 이 order라는 값을 관리할 오브젝트가 또 따로 필요함
        // 나중에 StageManager를 구현하든지 해서 거기다가 때려넣자
        deploymentOrder = order;
    }

    // --- 저지 관련 메서드들
    
    // 이 오퍼레이터가 적을 저지할 수 있는 상태인가?
    public bool CanBlockEnemy()
    {
        return isBlocking && currentBlockedEnemies < data.maxBlockableEnemies;
    }

    // 저지 가능하다면 현 저지수 + 1
    public void BlockEnemy()
    {
        if (CanBlockEnemy())
        {
            currentBlockedEnemies++;
        }
    }

    public void UnblockEnemy()
    {
        if (currentBlockedEnemies > 0)
        {
            currentBlockedEnemies--;
        }
    }

}
