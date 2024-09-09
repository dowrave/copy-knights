
using Unity.VisualScripting;
using UnityEngine;

public class DeployableUnitEntity : UnitEntity, IDeployable
{
    // IDeployable 인터페이스 관련
    public new DeployableUnitData data;
    public bool IsDeployed { get; protected set; }
    public int DeploymentCost => data.deploymentCost;
    public Sprite Icon => data.icon;


    // 미리보기 관련
    private bool isPreviewMode = false;
    public bool IsPreviewMode
    {
        get { return isPreviewMode; }
        set
        {
            isPreviewMode = value;
            UpdateVisuals();
        }
    }
    private MeshRenderer meshRenderer;
    private Material originalMaterial;
    private Material previewMaterial;
    private Renderer _renderer;
    public Renderer Renderer
    {
        get
        {
            if (_renderer == null)
            {
                _renderer = GetComponentInChildren<Renderer>();
            }
            return _renderer;
        }
    }

    public virtual void Initialize(DeployableUnitData deployableUnitData)
    {
        data = deployableUnitData;
        base.Initialize(deployableUnitData); // UnitData의 초기화는 여기서 처리됨
        
        meshRenderer = GetComponentInChildren<MeshRenderer>();
        
        IsDeployed = false;
        IsPreviewMode = true;
        InitializeDeployableProperties();
        
    }

    protected virtual void InitializeDeployableProperties()
    {

    }

    public virtual void Deploy(Vector3 position)
    {
        if (!IsDeployed)
        {
            IsDeployed = true;
            IsPreviewMode = false;

            transform.position = position;
            UpdateCurrentTile();
            UpdateVisuals();
        }
    }

    public virtual void Retreat()
    {
        if (IsDeployed)
        {
            IsDeployed = false;
            DeployableManager.Instance.OnDeployableRemoved(this);
            Destroy(gameObject);
        }
    }

    public virtual bool CanDeployOnTile(Tile tile)
    {
        if (IsInvalidTile(tile)) return false;

        if (tile.data.terrain == TileData.TerrainType.Ground && data.canDeployOnGround) return true;
        if (tile.data.terrain == TileData.TerrainType.Hill && data.canDeployOnHill) return true;

        return false;
    }

    private bool IsInvalidTile(Tile tile)
    {
        return tile == null || tile.IsOccupied || tile.data.isStartPoint || tile.data.isEndPoint;
    }

    public virtual void EnablePreviewMode()
    {
        IsPreviewMode = true;
    }

    public virtual void DisablePreviewMode()
    {
        IsPreviewMode = false;
    }

    public void UpdatePreviewPosition(Vector3 position)
    {
        if (IsPreviewMode)
        {
            transform.position = position;
        }
    }

    protected void PrepareTransparentMaterial()
    {
        
        if (meshRenderer != null)
        {
            originalMaterial = meshRenderer.material;
            previewMaterial = new Material(originalMaterial);
            previewMaterial.SetFloat("_Mode", 3); // TransParent 모드로 설정
            Color previewColor = previewMaterial.color;
            previewColor.a = 0.5f;
            previewMaterial.color = previewColor;
        }
    }

    protected virtual void ShowDeploymentUI()
    {

    }

    protected virtual void HideDeploymentUI()
    {

    }

    /// <summary>
    /// 배치된 유닛 클릭 시의 동작
    /// </summary>
    public virtual void OnClick()
    {
        // 배치된 오퍼레이터 & 미리보기 오퍼레이터를 클릭한 게 아닐 때
        if (IsDeployed && !IsPreviewMode && StageManager.Instance.currentState == GameState.Battle)
        {
            DeployableManager.Instance.CancelPlacement();

            // 미리보기 상태에선 동작하면 안됨
            if (IsPreviewMode == false)
            {
                UIManager.Instance.ShowDeployableInfo(this);
            }

            ShowActionUI();
        }
    }

    protected virtual void ShowActionUI()
    {
        DeployableManager.Instance.ShowActionUI(this);
        UIManager.Instance.ShowDeployableInfo(this);
    }

    private void UpdateVisuals()
    {
        if (IsDeployed && !IsPreviewMode && StageManager.Instance.currentState == GameState.Battle)
        {
            // 프리뷰 모드일 때의 시각 설정
            meshRenderer.material = previewMaterial;
        }

        else
        {
            // 실제 배치 모드일 때의 시각 설정
            meshRenderer.material = originalMaterial;
        }
    }

    public void SetPreviewTransparency(float alpha)
    {
        if (Renderer != null)
        {
            Material mat = Renderer.material;
            Color color = mat.color;
            color.a = alpha;
            mat.color = color;
        }
    }
}
