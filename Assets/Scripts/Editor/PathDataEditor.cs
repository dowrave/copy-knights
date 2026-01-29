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

    private SerializedProperty nodesProperty; 


    private void OnEnable()
    {
        pathData = (PathData)target;
        nodesProperty = serializedObject.FindProperty("nodes"); // PathData의 변수 이름 nodes
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
                Tile? clickedTile = FindTileComponent(pickedObject);
                if (clickedTile != null)
                {
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

        serializedObject.Update();

        // 배열 끝에 새 요소 추가
        int newIndex = nodesProperty.arraySize;
        nodesProperty.InsertArrayElementAtIndex(newIndex);

        // 새로 추가된 요소 가져오기
        SerializedProperty newElement = nodesProperty.GetArrayElementAtIndex(newIndex);

        // PathNodes의 각 필드 설정
        newElement.FindPropertyRelative("tileName").stringValue = tile.name;
        newElement.FindPropertyRelative("gridPosition").vector2IntValue = tile.GridPosition;
        newElement.FindPropertyRelative("waitTime").floatValue = 0f;

        serializedObject.ApplyModifiedProperties();
    }

    private void RemoveNearestNode(Vector2Int position)
    {
        InstanceValidator.ValidateInstance(pathData);

        if (pathData!.Nodes.Count == 0) return;

        // 가장 가까운 노드의 인덱스 찾기
        int nearestIndex = pathData.Nodes
            .Select((node, index) => new { Node = node, Index = index })
            .OrderBy(x => Vector2Int.Distance(x.Node.gridPosition, position))
            .First().Index;

        serializedObject.Update();

        // 해당 인덱스의 요소 제거
        nodesProperty.DeleteArrayElementAtIndex(nearestIndex);
        serializedObject.ApplyModifiedProperties();
    }
    
    private void OnDisable()
    {
        if (isEditing)
        {
            SceneView.duringSceneGui -= CustomSceneGUI;
            Tools.current = lastTool;
        }
    }

}
