using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Barricade Data", menuName = "Game/Barricade Data")]
public class BarricadeData : ScriptableObject
{
    public string barricadeName;
    public int health = 1;
    public int deploymentCost = 5;
    public float reDeployTime = 70f;

    public GameObject prefab;
    public Sprite icon;
}

public class Barricade : MonoBehaviour
{
    public BarricadeData data;
    private float currentHealth;

    public bool IsDeployed { get; private set; } = false;

    private Tile currentTile;

    public static event Action<Barricade> OnBarricadeDeployed;
    public static event Action<Barricade> OnBarricadeRetreated;


    public void Deploy(Vector3 position)
    {
        transform.position = position;
        IsDeployed = true;
        gameObject.SetActive(true);
    }

    public void Retreat()
    {
        IsDeployed = false;
        gameObject.SetActive(false);
        //OperatorManager.Instance.OnBarricadeRemoved(data);
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            Destroy();
        }
    }

    private void Destroy()
    {
        Retreat();
        Destroy(gameObject);
    }
}
