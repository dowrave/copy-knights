using UnityEngine;

[CreateAssetMenu(fileName = "New Deployable Unit Data", menuName = "Game/Deployable Unit Data")]
public class DeployableUnitData : ScriptableObject
{
    public string entityName;
    public DeployableUnitStats stats;
    public bool canDeployOnGround = false;
    public bool canDeployOnHill = false;
    public Sprite icon;
}