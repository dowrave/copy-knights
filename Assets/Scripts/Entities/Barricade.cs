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
        CurrentTile.ToggleWalkable(false); // ��ġ �� ���� Ÿ�� �̵� �Ұ�
        OnBarricadeDeployed?.Invoke(this);
        PathfindingManager.Instance.AddBarricade(this);
    }

    public override void Retreat()
    {
        base.Retreat();
        CurrentTile.ToggleWalkable(true); // �� �� ���� Ÿ�� �̵� ����
        OnBarricadeRemoved?.Invoke(this);
        PathfindingManager.Instance.RemoveBarricade(this);
    }

}
