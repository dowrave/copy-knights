using UnityEngine; 
using System; 

public class DeploymentController: IReadableDeploymentController
{
    private readonly DeployableUnitEntity _owner; 
    private IDeployableData _data;

    private float _lastDeployTime; // float네?
    // private float preventInteractingTime = 0.1f;

    public bool IsDeployed { get; private set; } = false;
    public bool IsPreviewMode { get; private set; }
    public Tile CurrentTile { get; private set; }
    public int DeploymentOrder { get; private set; } = 0; 

    public event Action<Tile> OnTileChanged = delegate { };
    // public event Action<DeployableUnitEntity> OnDeployed = delegate { };
    // public event Action<DeployableUnitEntity> OnUndeployed = delegate { };  

    // Awake에서 동작, _data는 _owner에 할당되지 않을 가능성이 높기 때문에 Initialize()에서 진행
    public DeploymentController(DeployableUnitEntity owner)
    {
        _owner = owner; 
    }

    public void Initialize(IDeployableData data)
    {
        _data = data;

        SetDeployState(false);
        DeploymentOrder = 0;
        // UpdateCurrentTile(); // 정말 필요한지 모르겠어서 주석 처리
    }

    public bool Deploy(Vector3 position, Vector3? facingDirection = null)
    {
        if (!IsDeployed)
        {
            SetPosition(position);
            SetDirection(facingDirection);
            SetDeployState(true);
            UpdateCurrentTile();

            // 게임의 핵심 로직이므로 이벤트가 아니라 직접 전달함
            if (CurrentTile != null)
            {
                CurrentTile.SetOccupied(_owner);
            }

            _lastDeployTime = Time.time;

            return true;
        }

        return false;
    }

    // 배치 해제 관련 로직 담당
    public bool Undeploy()
    {
        if (IsDeployed)
        {
            IsDeployed = false;
            if (CurrentTile != null)
            {
                CurrentTile.ClearOccupied();
            }
            return true;
        }
        return false;
    }

    public void SetDeployState(bool isDeployed)
    {
        IsDeployed = isDeployed;
        IsPreviewMode = !isDeployed;
    }

    // FacingDirection 필드는 컨테이너에서 관리한다. 여기선 설정만 해준다
    private void SetDirection(Vector3? facingDirection)
    {
        if (facingDirection == Vector3.zero) return;

        if (facingDirection != null)
        {
            // ?을 쓸 경우에 발생하는 이슈
            // 부모 컨테이너는 Vector3을 받기 때문에 null 체크를 했더라도 Value를 꺼내줘야 함;
            _owner.SetDirection(facingDirection.Value);
        }

    }

    private void SetPosition(Vector3 worldPosition)
    {
        if (CurrentTile != null)
        {
            if (_owner is Operator)
            {
                _owner.transform.position = worldPosition + Vector3.up * (CurrentTile.GetHeightScale() / 2 + 0.5f);
            }
            else
            {
                _owner.transform.position = worldPosition + Vector3.up * (CurrentTile.GetHeightScale() / 2);
            }
        }
    }

    

    private void UpdateCurrentTile()
    {
        Vector3 position = _owner.transform.position;
        Tile? newTile = MapManager.Instance!.GetTileAtWorldPosition(position);

        if (newTile != null && newTile != CurrentTile)
        {
            CurrentTile = newTile;
        }
        else
        {
            Logger.LogError("newTile이 없거나 기존 타일과 동일함");
            return;
        }
    }

    public virtual bool CanDeployOnTile(Tile tile)
    {
        if (IsInvalidTile(tile)) return false;

        if (tile.TileData.Terrain == TileData.TerrainType.Ground && _data.CanDeployOnGround) return true;
        if (tile.TileData.Terrain == TileData.TerrainType.Hill && _data.CanDeployOnHill) return true;

        return false;
    }

    private bool IsInvalidTile(Tile tile)
    {
        return tile == null ||
        tile.TileData.IsStartPoint ||
        tile.TileData.IsEndPoint;
    }

    public void SetDeploymentOrder(int order)
    {
        DeploymentOrder = order;
    }

    public void OnClick()
    {
        float preventInteractingTime = 0.1f;

        // 커서를 뗀 시점에 다시 클릭되는 현상 방지
        if (Time.time - _lastDeployTime < preventInteractingTime)
        {
            DeployableManager.Instance!.CancelPlacement();
            return;
        }

        // 배치된 유닛 클릭
        if (IsDeployed &&
            !IsPreviewMode &&
            StageManager.Instance!.CurrentGameState == GameState.Battle)
        {
            DeployableManager.Instance!.CancelPlacement();

            if (IsPreviewMode == false)
            {
                _owner.NotifySelected();
                // DeploymentInputHandler.Instance!.SetIsSelectingDeployedUnit(true);
                // StageManager.Instance!.SlowState = true;
                // StageUIManager.Instance!.ShowDeployedInfo(_owner);
                // DeployableManager.Instance!.ShowActionUI(_owner);
                // StageUIManager.Instance!.ShowDeployedInfo(_owner);
            }
        }
    }

}