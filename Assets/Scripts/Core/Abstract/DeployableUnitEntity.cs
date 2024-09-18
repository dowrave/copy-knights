#nullable enable
using UnityEngine;
using UnityEngine.UIElements;

public class DeployableUnitEntity: UnitEntity, IDeployable
{
    [SerializeField]
    private DeployableUnitData deployableUnitData;
    public new DeployableUnitData Data { get => deployableUnitData; private set => deployableUnitData = value; }

    public DeployableUnitStats currentStats;

    // IDeployable 인터페이스 관련
    public bool IsDeployed { get; protected set; }
    public int InitialDeploymentCost { get; protected set; } // 최초 배치 코스트 - DeploymentCost는 게임 중 증가할 수 있음

    public Sprite? Icon => Data.Icon;

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

    protected virtual void Awake()
    {
        InitializeVisual();
    }

    public void Initialize(DeployableUnitData deployableUnitData)
    {
        InitializeDeployableData(deployableUnitData);
        InitializeUnitProperties();
        InitializeDeployableProperties(); 
    }

    private void InitializeDeployableData(DeployableUnitData deployableData)
    {
        this.deployableUnitData = deployableData;
        currentStats = deployableUnitData.stats;
    }

    /// <summary>
    /// 자식 오브젝트에 시각화를 담당하는 Model이 있다는 전제
    /// </summary>
    protected virtual void InitializeDeployableProperties()
    {
        IsDeployed = false; // 배치 비활성화
        IsPreviewMode = true; // 미리보기 활성화
        InitialDeploymentCost = currentStats.DeploymentCost; // 초기 배치 코스트 설정
        SetupPreviewMaterial(); // 미리보기 머티리얼 준비
    }

    /// <summary>
    /// 프리팹에서 필드들의 정보를 초기화. 실제 초기화는 Initialize에서 별도로 이뤄진다.
    /// </summary>
    public virtual void InitializeFromPrefab()
    {
        if (modelObject == null)
        {
            modelObject = transform.Find("Model").gameObject;
        }

        if (modelObject != null)
        {
            modelRenderer = modelObject.GetComponent<Renderer>();
        }
        // DeployableUnitData 초기화 (만약 SerializeField로 설정되어 있다면 이미 할당되어 있음)
        if (deployableUnitData != null)
        {
            currentStats = deployableUnitData.stats;
        }
    }

    public virtual void Deploy(Vector3 position)
    {
        if (!IsDeployed)
        {
            IsDeployed = true;
            IsPreviewMode = false;
            UpdateCurrentTile();

            transform.position = SetPosition(position);

            InitializeHP();
            UpdateVisuals();
        }
    }

    /// <summary>
    /// 타일 위에서의 실제 배치 위치 조정
    /// </summary>
    protected Vector3 SetPosition(Vector3 worldPosition)
    {
        return new Vector3(worldPosition.x, CurrentTile.GetHeight() / 2f, worldPosition.z);
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

        if (tile.data.terrain == TileData.TerrainType.Ground && Data.canDeployOnGround) return true;
        if (tile.data.terrain == TileData.TerrainType.Hill && Data.canDeployOnHill) return true;

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
        if (modelObject == null || modelRenderer == null)
        {
            return false;
        }
        return true;
    }

    /// <summary>
    /// 반투명화한 미리보기 머티리얼을 준비
    /// </summary>
    protected void SetupPreviewMaterial()
    {
        originalMaterial = modelRenderer.sharedMaterial; // 프리팹, 에셋에 직접 접근할 때는 sharedMaterial을 사용

        // 이건 또 왜 null임 ㅅㅂ
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

    /// <summary>
    /// Data, Stat이 엔티티마다 다르기 때문에 자식 메서드에서 재정의가 항상 필요
    /// </summary>
    protected override void InitializeUnitProperties()
    {
        // 현재 체력, 최대 체력 설정 - Deploy 이후로 빠짐

        // 현재 위치를 기반으로 한 타일 설정
        UpdateCurrentTile();

        Prefab = Data.prefab;
    }

    /// <summary>
    /// 시각화 요소들을 초기화
    /// </summary>
    protected virtual void InitializeVisual()
    {
        modelObject = transform.Find("Model").gameObject;
        modelRenderer = modelObject.GetComponent<Renderer>();

        if (ValidateModelStructure() == false)
        {
            Debug.LogError("DeployableUnitEntity의 머티리얼 설정이 이상해요");
            return;
        }

        SetupPreviewMaterial();
    }

    protected override void InitializeHP()
    {
        MaxHealth = currentStats.Health;
        CurrentHealth = MaxHealth;
    }
}
#nullable restore
