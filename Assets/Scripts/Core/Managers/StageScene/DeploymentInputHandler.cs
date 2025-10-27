using System.Collections.Generic;
using UnityEngine;

public class DeploymentInputHandler: MonoBehaviour
{
    public static DeploymentInputHandler Instance { get; private set; }

    private enum InputState { None, SelectingBox, SelectingTile, SelectingDirection }
    private InputState currentState = InputState.None;

    private DeployableManager deployManager;
    private Camera mainCamera;

    // 배치 유닛 관련 상태
    private DeployableInfo currentDeployableInfo;
    private DeployableUnitEntity currentDeployableEntity;
    private Tile currentHoveredTile;
    private Vector3 placementDirection = Vector3.zero; 

    // 동작 상태
    public bool IsSelectingBox { get; private set; } = false; // 하단 UI에서 오퍼레이터를 클릭한 상태
    public bool IsDraggingDeployable { get; private set; } = false; 
    public bool IsSelectingDirection { get; private set; } = false;
    public bool IsMousePressed { get; private set; } = false;
    public bool IsSelectingDeployedUnit { get; private set; } = false;

    private float minDirectionDistance;

    // 클릭 방지 시간 관련
    private float preventClickingTime = 0.1f;
    private float lastPlacementTime;

    public bool IsClickingPrevented => Time.time - lastPlacementTime < preventClickingTime;

    private bool justEndedDrag = false;
    public bool JustEndedDrag => justEndedDrag; 

    public System.Action OnDragStarted = delegate { };
    public System.Action OnDragEnded = delegate { };

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        deployManager = DeployableManager.Instance;
        mainCamera = Camera.main;
    }

    private void Update()
    {
        // 1. 하단 UI의 오퍼레이터 클릭 시 배치 가능한 타일들 하이라이트
        if (currentState == InputState.SelectingBox)
        {
            deployManager.HighlightAvailableTiles();
        }
        // 2. 오퍼레이터를 드래그 중일 때 (타일 설정 상태)
        else if (currentState == InputState.SelectingTile)
        {
            deployManager.UpdatePreviewDeployable();
        }
        // 3. 오퍼레이터의 방향을 정할 때 (방향 설정 상태)
        else if (currentState == InputState.SelectingDirection)
        {
            HandleDirectionSelection();
        }
    }

    private void LateUpdate()
    {
        // 프레임 마지막에 플래그를 리셋함
        if (justEndedDrag)
        {
            justEndedDrag = false; 
        }
    }

    public void StartDeploymentProcess(DeployableInfo info)
    {
        if (currentState != InputState.None)
        {
            deployManager.CancelPlacement();
        }

        // DeployableManager에 배치 세션 시작을 요청함
        if (deployManager.StartDeployableSelection(info))
        {
            currentState = InputState.SelectingBox;
            currentDeployableInfo = info;
            currentDeployableEntity = deployManager.CurrentDeployableEntity;

            // 시간을 느리게
            GameManagement.Instance.TimeManager.SetPlacementTimeScale();
        }
    }

    public void StartDeployableDragging(DeployableInfo info)
    {
        if (currentDeployableInfo == info)
        {
            currentState = InputState.SelectingTile;

            deployManager.CreatePreviewDeployable();

            GameManagement.Instance!.TimeManager.SetPlacementTimeScale();

            OnDragStarted?.Invoke();
        }
    }

    public void HandleDeployableDragging(DeployableInfo deployableInfo)
    {
        if (IsDraggingDeployable && currentDeployableInfo == deployableInfo)
        {
            deployManager.UpdatePreviewDeployable();
        }
    }

    public void EndDeployableDragging()
    {
        if (currentState == InputState.SelectingTile)
        {
            Tile? hoveredTile = deployManager.GetHoveredTile();

            if (hoveredTile != null && deployManager.CanPlaceOnTile(hoveredTile))
            {
                Debug.Log($"currentDeployableEntity : {currentDeployableEntity}");
                // 방향 설정이 필요한 경우 방향 설정 단계로 진입
                if (currentDeployableEntity is Operator op)
                {
                    currentState = InputState.SelectingDirection;
                    StartDirectionSelection(hoveredTile);
                    // 여기선 OnDragEnd 이벤트 발생을 StartDirectionSelection의 시작 시점으로 옮김
                    // InstageInfoPanel의 CancelPanel과 충돌이 일어남;
                }
                // 방향 설정이 필요 없다면 바로 배치
                else
                {
                    deployManager.DeployDeployable(hoveredTile, placementDirection);
                    OnDragEnded?.Invoke();
                }
            }
            else
            {
                deployManager.CancelDeployableSelection();
                StageUIManager.Instance!.HideDeployableInfo();
                OnDragEnded?.Invoke();
            }
        }
    }

    public void StartDirectionSelection(Tile tile)
    {
        OnDragEnded?.Invoke();

        InstanceValidator.ValidateInstance(currentDeployableEntity);

        // IsSelectingDirection = true;
        currentState = InputState.SelectingDirection;

        deployManager.ResetHighlights();
        
        currentHoveredTile = tile;

        deployManager.SetAboveTilePosition(currentDeployableEntity!, tile);
        deployManager.ShowDeployingUI(tile.transform.position + Vector3.up * 0.5f);
        deployManager.UpdatePreviewRotation(placementDirection);
    }

    // 방향 설정 관련 로직
    public void HandleDirectionSelection()
    {
        Debug.LogWarning("HandleDirectionSelection 동작 중");
        if (currentState == InputState.SelectingDirection && Input.GetMouseButtonDown(0))
        {
            deployManager.ResetHighlights();

            if (currentHoveredTile != null)
            {
                Vector3 dragVector = Input.mousePosition - mainCamera.WorldToScreenPoint(currentHoveredTile.transform.position);
                float dragDistance = dragVector.magnitude;
                Vector3 newDirection = deployManager.DetermineDirection(dragVector);

                placementDirection = newDirection;

                if (currentDeployableEntity != null && currentDeployableEntity is Operator op)
                {
                    op.SetDirection(placementDirection);
                    op.HighlightAttackRange();
                }

                deployManager.UpdatePreviewRotation(placementDirection);

                if (Input.GetMouseButtonUp(0))
                {
                    // 일정 거리 이상 커서 이동 시 배치
                    if (dragDistance > minDirectionDistance)
                    {
                        deployManager.DeployDeployable(currentHoveredTile, placementDirection);
                        IsSelectingDirection = false;
                        IsMousePressed = false;
                        lastPlacementTime = Time.time; // 배치 시간 기록
                    }
                    // 바운더리 이내라면 다시 방향 설정(클릭 X) 상태
                    else
                    {
                        IsMousePressed = false;
                        deployManager.ResetHighlights();
                    }
                }
            }
        }
    }

    public void SetIsMousePressed(bool state)
    {
        IsMousePressed = state;
    }

    public void SetIsSelectingDeployedUnit(bool state)
    {
        IsSelectingDeployedUnit = state;
    }

    public void SetMinDirectionDistance(float screenDiamondRadius)
    {
        minDirectionDistance = screenDiamondRadius / 2;
    }

    public void ResetState()
    {
        Debug.Log("배치 클릭 시스템 상태 초기화됨");
        IsSelectingBox = false;
        IsDraggingDeployable = false;
        IsSelectingDirection = false;
        IsMousePressed = false;
        IsSelectingDeployedUnit = false;

        currentDeployableInfo = null;
        currentDeployableEntity = null;
        currentHoveredTile = null;

        placementDirection = Vector3.zero;

        currentState = InputState.None;
    }
}