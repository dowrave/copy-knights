using System.Collections;
using System.Collections.Generic;
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
    public string TileName;
    public TerrainType terrain;
    public bool isWalkable = true; // 적이 지나갈 수 있는가?
    public bool canPlaceOperator = true; // 오퍼레이터를 배치할 수 있는가?
    public bool isStartPoint = false;
    public bool isEndPoint = false;
    public Color tileColor = Color.gray;

    public bool hasPit = false; 
    public bool isDeployable = false;

}
