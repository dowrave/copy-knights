using UnityEngine;

[CreateAssetMenu(fileName = "NewTileData", menuName ="Game/Tile Data")]
public class TileData : ScriptableObject
{
    public enum TerrainType
    {
        Ground,
        Hill,
        Empty
    }

    public string TileName = string.Empty;
    public TerrainType terrain;
    public bool isWalkable = true; // 적이 지나갈 수 있는가?
    public bool isDeployable = true; // 배치할 수 있는가?
    public bool isStartPoint = false;
    public bool isEndPoint = false;
    public Color tileColor = Color.gray;

    [Header("Tile Prefab")]
    public GameObject tilePrefab = default!;

    public bool hasPit = false; 

}
