using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PathNode
{
    public string tileName = string.Empty;
    public Vector2Int gridPosition = new Vector2Int(0, 0);
    public float waitTime = 0f;
}

[CreateAssetMenu(fileName = "New Path Data", menuName = "Game/PathData")]
public class PathData: ScriptableObject
{
    [Header("Path Settings")]
    [SerializeField] private string targetMapId = string.Empty;
    public string TargetMapId => targetMapId;

    [Header("Path Nodes")]
    public List<PathNode> nodes = new List<PathNode>();
}
