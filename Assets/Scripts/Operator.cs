using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Operator : Unit
{
    [SerializeField] // �ʵ� ����ȭ, Inspector���� �� �ʵ� �����
    public OperatorData data;

    [SerializeField, HideInInspector]
    public Vector3 facingDirection = Vector3.left;


    public int currentBlockedEnemies; // ���� ���� ��
    public int deploymentOrder { get; private set; } // ��ġ ����

    [HideInInspector] public bool isBlocking = false; // ���� ���ΰ�

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
        // ���� ��ǥ -> ����� �׸��� ��ǥ�� ��ȯ�ϴ� ����
        return Vector2Int.zero;
    }
    

    public void SetDeploymentOrder(int order)
    {
        // �� order��� ���� ������ ������Ʈ�� �� ���� �ʿ���
        // ���߿� StageManager�� �����ϵ��� �ؼ� �ű�ٰ� ��������
        deploymentOrder = order;
    }

    // --- ���� ���� �޼����
    
    // �� ���۷����Ͱ� ���� ������ �� �ִ� �����ΰ�?
    public bool CanBlockEnemy()
    {
        return isBlocking && currentBlockedEnemies < data.maxBlockableEnemies;
    }

    // ���� �����ϴٸ� �� ������ + 1
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
