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
        CurrentTile.ToggleWalkable(false); // 배치된 타일은 이동 불가능
        PathfindingManager.Instance.AddBarricade(this);
        OnBarricadeDeployed?.Invoke(this);
    }

    public override void Retreat()
    {
        Die();
    }

    protected override void Die()
    {
        CurrentTile.ToggleWalkable(true); // 현재 타일 이동 가능으로 변경
        PathfindingManager.Instance.RemoveBarricade(this); // 바리케이드 리스트에서 제거
        OnBarricadeRemoved?.Invoke(this); // 제거 이벤트 발생
        base.Die();
    }
}
