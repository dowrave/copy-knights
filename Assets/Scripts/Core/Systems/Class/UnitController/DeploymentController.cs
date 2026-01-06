using UnityEngine; 
using System; 

public class DeploymentController: IReadableDeploymentController
{
    private readonly DeployableUnitEntity _owner; 
    private IDeployableData _data;

    private float _lastDeployTime; // float네?

    public bool IsDeployed { get; private set; }
    public bool IsPreviewMode { get; private set; }
    public Tile CurrentTile { get; private set; }

    public event Action<Tile> OnTileChanged = delegate { };
    public event Action<DeployableUnitEntity> OnDeployed = delegate { };
    public event Action<DeployableUnitEntity> OnUndeployed = delegate { };  

    // Awake에서 동작, _data는 _owner에 할당되지 않을 가능성이 높기 때문에 Initialize()에서 진행
    public DeploymentController(DeployableUnitEntity owner)
    {
        _owner = owner; 
    }

    public void Initialize(IDeployableData data)
    {
        _data = data;

        SetDeployState(false);
        // UpdateCurrentTile(); // 정말 필요한지 모르겠어서 주석 처리
    }

    public virtual void Deploy(Vector3 position)
    {
        if (!IsDeployed)
        {
            SetDeployState(true);
            UpdateCurrentTile();

            // 게임의 핵심 로직이므로 이벤트가 아니라 직접 전달함
            if (CurrentTile != null)
            {
                CurrentTile.SetOccupied(_owner);
            }

            SetPosition(position);

            _lastDeployTime = Time.time;

            OnDeployed?.Invoke(_owner);
        }
    }

    public void Undeploy()
    {
        IsDeployed = false;
        OnUndeployed?.Invoke(_owner);

        DeployableManager.Instance!.OnDeployableRemoved(_owner);
        if (CurrentTile != null)
        {
            CurrentTile.ClearOccupied();
        }
    }

    public void SetDeployState(bool isDeployed)
    {
        IsDeployed = isDeployed;
        IsPreviewMode = !isDeployed;
    }

    protected void SetPosition(Vector3 worldPosition)
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

    protected virtual void UpdateCurrentTile()
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

}