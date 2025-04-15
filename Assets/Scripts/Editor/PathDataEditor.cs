using UnityEngine;
using UnityEditor;
using System; 
using System.Linq;
using UnityEditor.SceneManagement;

[CustomEditor(typeof(PathData))]
public class PathDataEditor : Editor
{
    private PathData? pathData;
    private bool isEditing = false;
    //private int controlID; // 이벤트 처리 관리하는 컨트롤 ID
    private Tool lastTool; // 편집 모드 진입 전의 툴 상태


    private void OnEnable()
    {
        pathData = (PathData)target;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUILayout.Space();

        if (GUILayout.Button(isEditing ? "Stop Editing" : "Start Editing"))
        {
            isEditing = !isEditing;
            if (isEditing)
            {
                lastTool = Tools.current;
                Tools.current = Tool.None;
                SceneView.duringSceneGui += CustomSceneGUI;
            }
            else
            {
                Tools.current = lastTool;
                SceneView.duringSceneGui -= CustomSceneGUI;
            }
            SceneView.RepaintAll();
        }

        if (isEditing)
        {
            EditorGUILayout.HelpBox("씬 뷰의 노드를 클릭해서 노드를 추가합니다. shift 키로 노드를 제거합니다.", MessageType.Info);
        }
    }

    private void CustomSceneGUI(SceneView sceneView)
    {
        if (!isEditing) return;

        // 마우스 클릭 - 씬 뷰에서 기본 컨트롤을 강제로 소비, 선택 변경을 방지함
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));


        // 이벤트 처리
        Event e = Event.current;
        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            bool isPrefabMode = PrefabStageUtility.GetCurrentPrefabStage() != null;
            GameObject pickedObject = null;

            // 모드에 따른 감지 로직
            if (isPrefabMode)
            {
                pickedObject = HandleUtility.PickGameObject(e.mousePosition, false);
            }
            else
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                {
                    pickedObject = hit.collider.gameObject;
                }
            }

            // 클릭된 타일을 얻음
            if (pickedObject != null)
            {
                Debug.Log($"picked Object : {pickedObject.name}");
                Tile? clickedTile = FindTileComponent(pickedObject);
                if (clickedTile != null)
                {
                    Debug.Log($"Found Tile : {clickedTile.name}, Grid Position : {clickedTile.GridPosition}");
                    // shift 누르면 취소
                    if (e.shift)
                    {
                        RemoveNearestNode(clickedTile.GridPosition);
                    }
                    // 아니라면 추가
                    else
                    {
                        AddNode(clickedTile);
                    }
                }
                e.Use();
            }
        }

        //DrawPath();
    }

    // 부모 오브젝트에서 Tile 컴포넌트를 찾음
    private Tile? FindTileComponent(GameObject hitObject)
    {
        // 먼저 히트된 오브젝트에서 Tile 컴포넌트 찾기
        Tile tile = hitObject.GetComponent<Tile>();

        // 없다면 부모에서 찾기
        if (tile == null && hitObject.transform.parent != null)
        {
            tile = hitObject.transform.parent.GetComponent<Tile>();
        }

        return tile;
    }

    private void AddNode(Tile tile)
    {
        if (pathData == null) return; // Add null check

        Undo.RecordObject(pathData, "Add Path Node");

        Debug.Log($"{tile.gameObject.name}");
        Debug.Log($"{tile.GridPosition}");

        pathData.nodes.Add(new PathNode
        {
            tileName = tile.name,
            gridPosition = tile.GridPosition,
            waitTime = 0f
        }
        );

        EditorUtility.SetDirty(pathData);
    }

    private void RemoveNearestNode(Vector2Int position)
    {
        InstanceValidator.ValidateInstance(pathData);

        if (pathData!.nodes.Count == 0) return;

        int nearestIndex = pathData.nodes
            .Select((node, index) => new { Node = node, Index = index })
            .OrderBy(x => Vector2Int.Distance(x.Node.gridPosition, position))
            .First().Index;

        Undo.RecordObject(pathData, "Remove Path Node");
        pathData.nodes.RemoveAt(nearestIndex);
        EditorUtility.SetDirty(pathData);
    }

    //private void DrawPath()
    //{
    //    if (pathData == null || pathData.nodes == null)
    //    {
    //        Debug.LogWarning("PathData나 노드가 null이라 DrawPath 생략");
    //        return;
    //    }
        
    //    try
    //    {
    //        for (int i = 0; i < pathData!.nodes.Count; i++)
    //        {
    //            PathNode node = pathData!.nodes[i];
    //            Vector3 nodePosition = MapManager.Instance!.ConvertToWorldPosition(node.gridPosition);
    //            Handles.color = Color.yellow;
    //            Handles.SphereHandleCap(0, nodePosition, Quaternion.identity, 0.2f, EventType.Repaint);
        
    //            if (i < pathData.nodes.Count - 1)
    //            {
    //                Vector3 nextPosition = MapManager.Instance.ConvertToWorldPosition(pathData!.nodes[i + 1].gridPosition);
    //                Handles.DrawLine(nodePosition, nextPosition);
    //            }

    //            Handles.Label(nodePosition + Vector3.up, $"$Node {i}: Wait {pathData!.nodes[i].waitTime}s");
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        Debug.LogError($"Error in DrawPath : {ex.Message}");
    //    }
    //    finally
    //    {
    //        // GUI 상태 정리
    //        Handles.EndGUI();
    //    }
    //}

    private void OnDisable()
    {
        if (isEditing)
        {
            SceneView.duringSceneGui -= CustomSceneGUI;
            Tools.current = lastTool;
        }
    }

}
