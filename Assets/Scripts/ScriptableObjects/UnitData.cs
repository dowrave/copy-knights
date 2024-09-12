using UnityEngine;

[CreateAssetMenu(fileName = "New Unit Data", menuName = "Game/Unit Data")]
public class UnitData : ScriptableObject
{
    public string entityName;
    public UnitStats stats;
    public GameObject prefab;
}