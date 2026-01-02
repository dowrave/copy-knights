using System;
using UnityEngine;

public class Barricade : DeployableUnitEntity
{
    public static event Action<Barricade> OnBarricadeDeployed = delegate { };
    public static event Action<Barricade> OnBarricadeRemoved = delegate { };

    // CurrentTile이 deployableUnitEntity에서 할당되어서 오기 때문에, null 경고는 무시함

    public override void Deploy(Vector3 position)
    {
        base.Deploy(position);

        CurrentTile!.ToggleWalkable(false); // 배치된 타일은 이동 불가능
        PathfindingManager.Instance!.AddBarricade(this);
        OnBarricadeDeployed?.Invoke(this);
    }

    public override void Retreat()
    {
        UndeployBarricade();
        base.Retreat();
    }

    protected override void Die()
    {
        UndeployBarricade();
        base.Die();
    }

    private void UndeployBarricade()
    {
        CurrentTile!.ToggleWalkable(true); // 현재 타일 이동 가능으로 변경
        PathfindingManager.Instance!.RemoveBarricade(this); // 바리케이드 리스트에서 제거
        OnBarricadeRemoved?.Invoke(this); // 제거 이벤트 발생
    }
}
