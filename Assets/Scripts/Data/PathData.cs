using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PathNode
{
    public string tileName;
    public Vector2Int gridPosition;
    public float waitTime;
}

[CreateAssetMenu(fileName = "New Path Data", menuName = "Game/PathData")]
public class PathData: ScriptableObject
{
    [Header("Path Settings")]
    [SerializeField] private string targetMapId;
    public string TargetMapId => targetMapId;

    [Header("Path Nodes")]
    public List<PathNode> nodes = new List<PathNode>();
}
