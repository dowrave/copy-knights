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

    [SerializeField] private string tileName = string.Empty;
    [SerializeField] private TerrainType terrain;
    [SerializeField] private bool isWalkable = true; // 적이 지나갈 수 있는가?
    [SerializeField] private bool isDeployable = true; // 배치할 수 있는가?
    [SerializeField] private bool isStartPoint = false;
    [SerializeField] private bool isEndPoint = false;
    [SerializeField] private Color tileColor = Color.gray;

    [Header("Tile Prefab")]
    [SerializeField] private GameObject tilePrefab = default!;

    [SerializeField] private bool hasPit = false; 

    // 일단 땜빵용으로 public 세터를 뒀음
    public string TileName 
    {
        get => tileName; 
        set
        {
            tileName = value;
        }
    }
    public TerrainType Terrain 
    {
        get => terrain;
        set
        {
            terrain = value;
        }
    }
    public Color TileColor => tileColor;
    public GameObject TilePrefab => tilePrefab;

    public bool IsWalkable => isWalkable;
    public bool IsDeployable => isDeployable;
    public bool IsStartPoint => isStartPoint;
    public bool IsEndPoint => isEndPoint;
    public bool HasPit => hasPit;

}
