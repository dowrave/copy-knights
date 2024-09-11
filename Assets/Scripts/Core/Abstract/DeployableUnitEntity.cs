using UnityEngine;

public class DeployableUnitEntity : UnitEntity, IDeployable
{
    private DeployableUnitData data;
    private DeployableUnitStats currentStats;

    // IDeployable 인터페이스 관련
    public bool IsDeployed { get; protected set; }
    public int DeploymentCost { get => currentStats.deploymentCost; set => currentStats.deploymentCost = value; }
    public float RedeployTime { get => currentStats.redeployTime; set => currentStats.redeployTime = value; }
    public int InitialDeploymentCost { get; protected set; } // 최초 배치 코스트 - DeploymentCost는 게임 중 증가할 수 있음

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
    protected GameObject modelObject;
    protected Renderer modelRenderer;
    protected Material originalMaterial;
    protected Material previewMaterial;

    public override void Initialize(UnitData unitData)
    {
        base.Initialize(unitData); // 이 클래스의 InitializeData가 호출됨
        InitializeDeployableProperties();
    }

    protected override void InitializeData(UnitData unitData)
    {
        if (unitData is DeployableUnitData deployableUnitData)
        {
            data = deployableUnitData;
            currentStats = data.stats;
        }
        else
        {
            Debug.LogError("들어온 데이터가 deployableUnitData가 아님!");
        }
    }


    /// <summary>
    /// DeployableUnitEntity 관련 초기화
    /// </summary>
    protected virtual void InitializeDeployableProperties()
    {
        IsDeployed = false;
        IsPreviewMode = true;
        InitialDeploymentCost = DeploymentCost;


        SetupPreviewMaterial(); // 미리보기 머티리얼 준비
        InitializeMaxHealth(); // 최대 체력 설정

        if (!ValidateModelStructure()) // 자식 오브젝트에 Model 오브젝트가 있는지 체크
        {
            Debug.LogError($"DeployableData가 아님, 초기화 중단");
            return;
        }

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

    /// <summary>
    /// 자식 오브젝트인 Model이 있는지 여부를 체크
    /// </summary>
    private bool ValidateModelStructure()
    {
        modelObject = transform.Find("Model")?.gameObject;
        if (modelObject == null)
        {
            return false;
        }

        modelRenderer = modelObject.GetComponent<Renderer>();
        return true;
    }

    /// <summary>
    /// 반투명화한 미리보기 머티리얼을 준비
    /// </summary>
    protected void SetupPreviewMaterial()
    {
        originalMaterial = modelRenderer.material;
        previewMaterial = new Material(originalMaterial);
        previewMaterial.SetFloat("_Mode", 3); // TransParent 모드로 설정
        Color previewColor = previewMaterial.color;
        previewColor.a = 0.5f;
        previewMaterial.color = previewColor;
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
            modelRenderer.material = previewMaterial;
        }

        else
        {
            // 실제 배치 모드일 때의 시각 설정
            modelRenderer.material = originalMaterial;
        }
    }

    public void SetPreviewTransparency(float alpha)
    {
        if (previewMaterial != null)
        { 
            Color color = previewMaterial.color;
            color.a = alpha;
            previewMaterial.color = color;
        }
    }
}
