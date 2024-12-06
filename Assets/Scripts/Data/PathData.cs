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

    public bool IsValidForMap(Map map)
    {
        if (string.IsNullOrEmpty(targetMapId) || map == null) return false;

        if (targetMapId != map.Mapid) return false; 

        // 모든 노드가 맵 범위 안에 있나 확인 
        foreach (var node in nodes)
        {
            if (!map.IsValidGridPosition(node.gridPosition.x, node.gridPosition.y)) return false; 
        }

        return true; 
    }
}
