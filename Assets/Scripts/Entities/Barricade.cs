using System;
using UnityEngine;

public class Barricade : DeployableUnitEntity
{
    public static event Action<Barricade> OnBarricadeDeployed = delegate { };
    public static event Action<Barricade> OnBarricadeRemoved = delegate { };

    // CurrentTile�� deployableUnitEntity���� �Ҵ�Ǿ ���� ������, null ���� ������

    public override void Deploy(Vector3 position)
    {
        base.Deploy(position);

        CurrentTile!.ToggleWalkable(false); // ��ġ�� Ÿ���� �̵� �Ұ���
        PathfindingManager.Instance!.AddBarricade(this);
        OnBarricadeDeployed?.Invoke(this);
    }

    public override void Retreat()
    {
        Die();
    }

    protected override void Die()
    {
        CurrentTile!.ToggleWalkable(true); // ���� Ÿ�� �̵� �������� ����
        PathfindingManager.Instance!.RemoveBarricade(this); // �ٸ����̵� ����Ʈ���� ����
        OnBarricadeRemoved?.Invoke(this); // ���� �̺�Ʈ �߻�
        base.Die();
    }

    protected override float CalculateActualDamage(AttackType attacktype, float incomingDamage)
    {
        return incomingDamage; 
    }
}
