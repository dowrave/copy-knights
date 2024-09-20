using System;
using UnityEngine;

public class Barricade : DeployableUnitEntity
{
    public int health = 1;
    public int deploymentCost = 5;
    public Sprite icon;

    public Transform Transform => transform;


    private bool canDeployGround = true;
    private bool canDeployHill = false;

    public bool CanDeployGround => canDeployGround;
    public bool CanDeployHill => canDeployHill;

    public static event Action<Barricade> OnBarricadeDeployed;
    public static event Action<Barricade> OnBarricadeRemoved;

    public void Initialize()
    {
        InitializeDeployableProperties(); 
        // 기존 초기화 코드가 있다면 여기에 추가...
    }


    public override void Deploy(Vector3 position)
    {
        Vector3 newPosition = new Vector3(position.x, 0.1f, position.z);
        base.Deploy(newPosition);
        OnBarricadeDeployed?.Invoke(this);
    }

    public override void Retreat()
    {
        base.Retreat();
        OnBarricadeRemoved?.Invoke(this);
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        if (health <= 0)
        {
            Destroy(gameObject);
        }
    }
}
