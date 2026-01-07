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

    private Vector3 dragStartPosition;
    private Vector3 dragEndPosition;

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

        minDirectionDistance = Screen.width * 0.1f;

        DeployableUnitEntity.OnDeployableSelected += HandleDeployableClicked;
    }

    private void Start()
    {
        deployManager = DeployableManager.Instance;
        mainCamera = Camera.main;
    }

    private void Update()
    {
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

            // 시간을 느리게
            // GameManagement.Instance.TimeManager.SetPlacementTimeScale();
            Time.timeScale = .2f;
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

            // GameManagement.Instance!.TimeManager.SetPlacementTimeScale();
            Time.timeScale = .2f;

            IsDragging = true;

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

            IsDragging = false;
        }
    }

    public void StartDirectionSelection(Tile tile)
    {
        OnDragEnded?.Invoke();

        InstanceValidator.ValidateInstance(currentDeployableEntity);

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
        if (CurrentState != InputState.SelectingDirection) return;

        // 클릭 중이 아닐 때는 타일 하이라이트 제거
        if (!IsDragging)
        {
            deployManager.ResetHighlights();
        }

        Vector3 basePosition = mainCamera.WorldToScreenPoint(currentHoveredTile.transform.position);
        Vector3 dragVector = Input.mousePosition - basePosition; // 스크린에 투사한 타일 위치 ~ 커서 위치

        // 마우스 버튼을 누르는 순간 드래그 시작
        if (Input.GetMouseButtonDown(0))
        {
            // 클릭이 된 지점이 마름모 밖이라면 배치 로직 취소
            if ((Input.mousePosition - basePosition).magnitude > minDirectionDistance)
            {
                Logger.Log($"기준 위치에서의 거리 : {(Input.mousePosition - basePosition).magnitude}");
                Logger.Log($"최소 거리 : {minDirectionDistance}");

                Logger.Log("클릭 시작 지점이 마름모 밖이므로 배치 로직이 취소됨");
                deployManager.CancelPlacement();
                return;
            }

            IsDragging = true;

            // 커서 방향으로 공격 범위 하이라이트 및 회전 설정(드래그 시작 & 드래그 중일 때 동작하는 로직)
            placementDirection = DetermineDirection(dragVector);
                if (currentDeployableEntity is Operator op)
                {
                    op.SetDirection(placementDirection);
                    op.HighlightAttackRange();
                }
            deployManager.UpdatePreviewRotation(placementDirection);
        }
        

        // 2. 드래그 중
        if (IsDragging && Input.GetMouseButton(0))
        {
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
        }

        // 3. 드래그 종료
        if (Input.GetMouseButtonUp(0))
        {
            if (!IsDragging) return;

            float dragDistance = dragVector.magnitude;

            // 일정 거리 이상 커서 이동 시 배치
            if (dragDistance > minDirectionDistance)
            {
                // 배치 시 이 스크립트의 상태도 초기화됨
                deployManager.DeployDeployable(currentHoveredTile, placementDirection);
                lastPlacementTime = Time.time;
            }

            // 일정 거리 이상 이동하지 않았다면 타일이 선택된 상태는 유지함
            else
            {
                deployManager.ResetHighlights();
                IsDragging = false;
            }
        }
    }

    private Vector3 DetermineDirection(Vector3 dragVector)
    {
        float angle = Mathf.Atan2(dragVector.y, dragVector.x) * Mathf.Rad2Deg;
        if (angle < 45 && angle >= -45) return Vector3.right;
        if (angle < 135 && angle >= 45) return Vector3.forward;
        if (angle >= 135 || angle < -135) return Vector3.left;
        return Vector3.back;
    } 

    public void HandleDeployableClicked(DeployableUnitEntity deployable)
    {
        IsSelectingDeployedUnit = true;
    }

    public void ResetState()
    {
        IsDragging = false;
        IsSelectingDeployedUnit = false;

        currentDeployableInfo = null;
        currentDeployableEntity = null;
        currentHoveredTile = null;

        placementDirection = Vector3.zero;

        CurrentState = InputState.None;
    }

    private void OnDisable()
    {
        DeployableUnitEntity.OnDeployableSelected -= HandleDeployableClicked;
    }
}