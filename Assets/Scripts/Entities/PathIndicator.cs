using System.Collections;
using System.Collections.Generic;
using UnityEditor.Overlays;
using UnityEngine;
using System.Linq;

public class PathIndicator : MonoBehaviour
{
    private float speed = 10f;
    private float height = 0.3f; // 월드 좌표 기준 +y 위치

    // 경로 관련
    protected PathNavigator navigator;
    // protected PathData? pathData;
    protected List<PathNode> pathNodes;
    protected PathNode nextNode = default!;
    protected int nextNodeIndex; // 시작하자마자 1이 됨
    protected Vector3 nextNodeWorldPosition; // 다음 노드의 좌표
    protected Vector3 destinationPosition; // 목적지
    protected List<Vector3> currentPath = new List<Vector3>();
    protected bool isWaiting = false; // 단순히 위치에서 기다리는 상태
    protected bool stopAttacking = false; // 인위적으로 넣은 공격 가능 / 불가능 상태
    protected Barricade? targetBarricade;

    public PathNavigator Navigator => navigator;
    public PathNode NextNode => nextNode;
    public Vector3 NextNodeWorldPosition => nextNodeWorldPosition; 
    public Vector3 DestinationPosition => DestinationPosition; 


    public void Initialize(PathData pathData)
    {
        if (pathData == null) Logger.LogError("pathData가 전달되지 않음");

        pathNodes = pathData.Nodes;
        currentPath = pathNodes.Select(node => MapManager.Instance!.ConvertToWorldPosition(node.gridPosition) + Vector3.up * 0.5f).ToList();
        nextNodeIndex = 0; 

        // Enemy에 작성된 순서를 따라감
        SetupInitialPosition();
        InitializeCurrentPath();

        navigator = new PathNavigator(this, pathNodes, destinationPosition);
        navigator.OnPathUpdated += HandlePathUpdated;
        if (navigator.IsPathBlocked())
        {
            navigator.UpdatePath(transform.position);
        }

        UpdateNextNode();
    }

    private void OnEnable()
    {
        // navigator = new PathNavigator(this, destinationPosition);
        // navigator.OnPathUpdated += HandlePathUpdated;
        // if (navigator.IsPathBlocked())
        // {
        //     navigator.UpdatePath(transform.position);
        // }
    }

    private void OnDiable()
    {
        if (navigator != null)
        {
            navigator.OnPathUpdated -= HandlePathUpdated;
            navigator.Cleanup();
        }
    }

    protected void HandlePathUpdated(List<PathNode> newPathNodes)
    {
        pathNodes = newPathNodes;
        currentPath = pathNodes.Select(node => MapManager.Instance!.ConvertToWorldPosition(node.gridPosition) + Vector3.up * 0.5f).ToList();
        nextNodeIndex = 0; 
        navigator.UpdateNextNodeIndex(nextNodeIndex);
    }


    private void InitializeCurrentPath()
    {
        InstanceValidator.ValidateInstance(pathNodes);

        foreach (var node in pathNodes)
        {
            currentPath.Add(MapManager.Instance!.ConvertToWorldPosition(node.gridPosition) + Vector3.up * height);
        }

        destinationPosition = currentPath[currentPath.Count - 1]; // 목적지 설정
    }

    private void SetupInitialPosition()
    {
        if (pathNodes.Count > 0)
        {
            transform.position = MapManager.Instance!.ConvertToWorldPosition(pathNodes[0].gridPosition) +
                Vector3.up * height;
        }
    }

    private void Update()
    {
        if (StageManager.Instance!.CurrentGameState == GameState.Battle)
        {
            MoveAlongPath();
        }
    }

    private void MoveAlongPath()
    {
        // 대기 중일 때는 이동하지 않음
        if (isWaiting)
        {
            return;
        }

        if (CheckIfReachedDestination())
        {
            ReachDestination();
            return;
        }

        Move(nextNodeWorldPosition);

        if (Vector3.Distance(transform.position, nextNodeWorldPosition) < 0.05f)
        {
            if (Vector3.Distance(transform.position, destinationPosition) < 0.05f)
            {
                ReachDestination();
            }
            else if (nextNode != null && nextNode.waitTime > 0)
            {
                StartCoroutine(WaitAtNode(nextNode.waitTime));
            }
            else
            {
                UpdateNextNode();
            }
        }
    }

    // 마지막 타일의 월드 좌표 기준
    private bool CheckIfReachedDestination()
    {
        if (pathNodes.Count == 0) return false;

        Vector2Int lastNodeGridPos = pathNodes[pathNodes.Count - 1].gridPosition;
        Vector3 lastNodePosition = MapManager.Instance!.ConvertToWorldPosition(lastNodeGridPos) + Vector3.up * height;

        return Vector3.Distance(transform.position, lastNodePosition) < 0.05f;
    }

    private void ReachDestination()
    {
        ObjectPoolManager.Instance.ReturnToPool(ObjectPoolManager.PathIndicatorTag, gameObject);
        // Destroy(gameObject, 0.5f);
    }

    public void Move(Vector3 destination)
    {
        transform.position = Vector3.MoveTowards(transform.position, destination, speed * Time.deltaTime);
    }

    private void UpdateNextNode()
    {
        nextNodeIndex++;
        if (navigator == null) Logger.LogError("navigator가 null임");
        navigator.UpdateNextNodeIndex(nextNodeIndex);

        if (nextNodeIndex < pathNodes.Count)
        {
            nextNode = pathNodes[nextNodeIndex];
            nextNodeWorldPosition = MapManager.Instance!.ConvertToWorldPosition(nextNode.gridPosition) +
                Vector3.up * height;
        }
    }

    // 대기 중일 때 실행
    private IEnumerator WaitAtNode(float waitTime)
    {
        isWaiting = true;
        yield return new WaitForSeconds(waitTime);
        isWaiting = false;

        UpdateNextNode();
    }

    public void SetPathNodes(List<PathNode> newPathNodes)
    {
        pathNodes = newPathNodes;
    }

    public void SetCurrentPath(List<Vector3> newPath)
    {
        currentPath = newPath;
    }

}
