using System;
using UnityEngine;

public class Barricade : DeployableUnitEntity
{
    public Transform Transform => transform;

    public override bool CanDeployGround { get; set; } = true;
    public override bool CanDeployHill { get; set; } = false;

    public static event Action<Barricade> OnBarricadeDeployed;
    public static event Action<Barricade> OnBarricadeRemoved;

    public override void Deploy(Vector3 position)
    {
        base.Deploy(position);
        CurrentTile.ToggleWalkable(false); // ��ġ�� Ÿ���� �̵� �Ұ���
        PathfindingManager.Instance.AddBarricade(this);
        OnBarricadeDeployed?.Invoke(this);
    }

    public override void Retreat()
    {
        Die();
    }

    protected override void Die()
    {
        CurrentTile.ToggleWalkable(true); // ���� Ÿ�� �̵� �������� ����
        PathfindingManager.Instance.RemoveBarricade(this); // �ٸ����̵� ����Ʈ���� ����
        OnBarricadeRemoved?.Invoke(this); // ���� �̺�Ʈ �߻�
        base.Die();
    }
}