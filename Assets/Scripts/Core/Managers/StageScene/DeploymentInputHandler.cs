using System.Collections.Generic;
using UnityEngine;

public class DeploymentInputHandler: MonoBehaviour
{
    public static DeploymentInputHandler Instance { get; private set; }

    public enum InputState { None, SelectingBox, SelectingTile, SelectingDirection }
    // private InputState CurrentState = InputState.None;
    public InputState CurrentState { get; private set; } = InputState.None;

    private DeployableManager deployManager;
    private Camera mainCamera;

    // 배치 유닛 관련 상태
    private DeployableInfo currentDeployableInfo;
    private DeployableUnitEntity currentDeployableEntity;
    private Tile currentHoveredTile;
    private Vector3 placementDirection = Vector3.zero; 

    // 동작 상태
    // 드래그하는 경우는 크게 2개임 - Box에서 꺼낼 때, 방향을 정할 때
    // CurrentState는 상태를, IsDragging은 동작을 감지하는 식으로 구현하겠음
    public bool IsDragging { get; private set; } = false; 
    public bool IsSelectingDeployedUnit { get; private set; } = false;

    // 배치에 필요한 최소 드래그 거리
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
        IsDragging = Input.GetMouseButtonDown(0) ? true : false;

        // 1. 하단 UI의 오퍼레이터 클릭 시 배치 가능한 타일들 하이라이트
        if (CurrentState == InputState.SelectingBox)
        {
            deployManager.HighlightAvailableTiles();
        }
        // 2. 오퍼레이터를 드래그 중일 때 (타일 설정 상태)
        else if (CurrentState == InputState.SelectingTile)
        {
            deployManager.UpdatePreviewDeployable();
        }
        // 3. 오퍼레이터의 방향을 정할 때 (방향 설정 상태)
        else if (CurrentState == InputState.SelectingDirection)
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
        if (CurrentState != InputState.None)
        {
            deployManager.CancelPlacement();
        }

        // DeployableManager에 배치 세션 시작을 요청함
        if (deployManager.StartDeployableSelection(info))
        {
            CurrentState = InputState.SelectingBox;
            currentDeployableInfo = info;

            // currentDeployableEntity = deployManager.CurrentDeployableEntity;
            // Debug.Log($"배치 시작 - currentDeployableEntity 설정됨 : {currentDeployableEntity}");


            // 시간을 느리게
            GameManagement.Instance.TimeManager.SetPlacementTimeScale();
        }
    }

    public void StartDeployableDragging(DeployableInfo info)
    {
        if (currentDeployableInfo == info)
        {
            CurrentState = InputState.SelectingTile;

            deployManager.CreatePreviewDeployable();

            // currentDeployableEntity는 CreatePreviewDeployable에서 처음으로 초기화됨(활성화되는 시점)
            currentDeployableEntity = deployManager.CurrentDeployableEntity;

            GameManagement.Instance!.TimeManager.SetPlacementTimeScale();

            // IsDragging = true;

            OnDragStarted?.Invoke();
        }
    }

    public void HandleDeployableDragging(DeployableInfo deployableInfo)
    {
        if (CurrentState == InputState.SelectingTile && currentDeployableInfo == deployableInfo)
        {
            deployManager.UpdatePreviewDeployable();
        }
    }

    public void EndDeployableDragging()
    {
        if (CurrentState == InputState.SelectingTile)
        {
            Tile? hoveredTile = deployManager.GetHoveredTile();

            if (hoveredTile != null && deployManager.CanPlaceOnTile(hoveredTile))
            {
                // 방향 설정이 필요한 경우 방향 설정 단계로 진입
                if (currentDeployableEntity is Operator op)
                {
                    CurrentState = InputState.SelectingDirection;
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

            // IsDragging = false;
        }
    }

    public void StartDirectionSelection(Tile tile)
    {
        OnDragEnded?.Invoke();

        InstanceValidator.ValidateInstance(currentDeployableEntity);

        // IsSelectingDirection = true;
        CurrentState = InputState.SelectingDirection;

        deployManager.ResetHighlights();
        
        currentHoveredTile = tile;

        deployManager.SetAboveTilePosition(currentDeployableEntity!, tile);
        deployManager.ShowDeployingUI(tile.transform.position + Vector3.up * 0.5f);
        deployManager.UpdatePreviewRotation(placementDirection);
    }

    // 방향 설정 관련 로직
    public void HandleDirectionSelection()
    {
        // 드래그 시작 후에만 방향 업데이트 & 마우스 버튼 Up 로직 실행
        if (!IsDragging && CurrentState != InputState.SelectingDirection) return;

        // 1. 드래그 중 - 매 프레임 방향 미리보기 업데이트
        Vector3 dragVector = Input.mousePosition - mainCamera.WorldToScreenPoint(currentHoveredTile.transform.position);
        Vector3 newDirection = DetermineDirection(dragVector);

        // 방향 변경 시에만 업데이트
        if (placementDirection != newDirection)
        {
            placementDirection = newDirection;
            if (currentDeployableEntity is Operator op)
            {
                op.SetDirection(placementDirection);
                op.HighlightAttackRange();
            }
            deployManager.UpdatePreviewRotation(placementDirection);
        }

        // 2. 마우스 버튼 Up - 배치 / 배치 취소 결정
        if (Input.GetMouseButtonUp(0))
        {
            float dragDistance = dragVector.magnitude;
            Debug.Log($"dragDistance : {dragDistance}");

            // 일정 거리 이상 커서 이동 시 배치
            if (dragDistance > minDirectionDistance)
            {
                // 배치 시 이 스크립트의 상태도 초기화됨
                deployManager.DeployDeployable(currentHoveredTile, placementDirection); 
                lastPlacementTime = Time.time;
            }

            // 일정 거리 이상 이동하지 않았다면 배치 로직 유지
            else
            {
                deployManager.ResetHighlights();
                IsDragging = false;
            }
        }

        // if (CurrentState == InputState.SelectingDirection && Input.GetMouseButtonDown(0))
        // {
        //     deployManager.ResetHighlights();

        //     if (currentHoveredTile != null)
        //     {
        //         Vector3 dragVector = Input.mousePosition - mainCamera.WorldToScreenPoint(currentHoveredTile.transform.position);
        //         float dragDistance = dragVector.magnitude;
        //         Vector3 newDirection = DetermineDirection(dragVector);

        //         placementDirection = newDirection;

        //         if (currentDeployableEntity != null && currentDeployableEntity is Operator op)
        //         {
        //             op.SetDirection(placementDirection);
        //             op.HighlightAttackRange();
        //         }

        //         deployManager.UpdatePreviewRotation(placementDirection);
        //         Debug.Log("방향 미리보기 수정됨");

        //         if (Input.GetMouseButtonUp(0))
        //         {
        //             Debug.Log("방향 선택 중 마우스 버튼 업 감지");
        //             Debug.Log($"dragDistance : {dragDistance}");
        //             Debug.Log($"minDirectionDistance : {minDirectionDistance}");

        //             // 일정 거리 이상 커서 이동 시 배치
        //             if (dragDistance > minDirectionDistance)
        //             {
        //                 deployManager.DeployDeployable(currentHoveredTile, placementDirection);
        //                 lastPlacementTime = Time.time; // 배치 시간 기록
        //             }
        //             // 바운더리 이내라면 다시 방향 설정(클릭 X) 상태
        //             else
        //             {
        //                 IsMousePressed = false;
        //                 deployManager.ResetHighlights();
        //             }
        //         }
        //     }
        // }
    }

    private Vector3 DetermineDirection(Vector3 dragVector)
    {
        float angle = Mathf.Atan2(dragVector.y, dragVector.x) * Mathf.Rad2Deg;
        if (angle < 45 && angle >= -45) return Vector3.right;
        if (angle < 135 && angle >= 45) return Vector3.forward;
        if (angle >= 135 || angle < -135) return Vector3.left;
        return Vector3.back;
    } 

    public void SetDraggingState(bool state)
    {
        IsDragging = state;
    }

    public void SetIsSelectingDeployedUnit(bool state)
    {
        IsSelectingDeployedUnit = state;
    }

    public void SetMinDirectionDistance(float screenDiamondRadius)
    {
        minDirectionDistance = screenDiamondRadius / 2;
        Debug.Log($"minDirectionDistance = {minDirectionDistance}");
    }

    public void StartDirectionDrag()
    {
        IsDragging = true;
    }

    public void ResetState()
    {
        Debug.Log("배치 클릭 시스템 상태 초기화됨");
        IsDragging = false;
        IsSelectingDeployedUnit = false;

        currentDeployableInfo = null;
        currentDeployableEntity = null;
        currentHoveredTile = null;

        placementDirection = Vector3.zero;

        CurrentState = InputState.None;
    }
}