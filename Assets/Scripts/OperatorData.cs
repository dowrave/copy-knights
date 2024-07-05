using UnityEngine;

[CreateAssetMenu(fileName = "New Operator Data", menuName = "Game/Operator Data")]
public class OperatorData : ScriptableObject
{
    public string operatorName;
    public UnitStats baseStats;
    public Vector2Int[] attackableTiles = { Vector2Int.zero };
    public bool canDeployGround;
    public bool canDeployHill;
    public int maxBlockableEnemies = 1;

    public float deploymentCost;
    public float reDeployTime = 70f;
}