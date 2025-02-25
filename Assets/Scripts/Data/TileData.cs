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
    public bool isWalkable = true; // ���� ������ �� �ִ°�?
    public bool isDeployable = true; // ��ġ�� �� �ִ°�?
    public bool isStartPoint = false;
    public bool isEndPoint = false;
    public Color tileColor = Color.gray;

    [Header("Tile Prefab")]
    public GameObject tilePrefab;

    public bool hasPit = false; 

}
