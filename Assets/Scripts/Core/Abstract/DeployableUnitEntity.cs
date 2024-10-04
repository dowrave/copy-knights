#nullable enable
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UIElements;

public class DeployableUnitEntity: UnitEntity, IDeployable
{
    [SerializeField]
    private DeployableUnitData deployableUnitData;
    public new DeployableUnitData Data { get => deployableUnitData; private set => deployableUnitData = value; }

    [HideInInspector]
    public DeployableUnitStats currentStats;

    // IDeployable 인터페이스 관련
    public bool IsDeployed { get; protected set; }
    public int InitialDeploymentCost { get; protected set; } // 최초 배치 코스트 - DeploymentCost는 게임 중 증가할 수 있음

    public Sprite? Icon => Data.Icon;

    // 미리보기 관련
    protected bool isPreviewMode = false;
    public bool IsPreviewMode
    {
        get { return isPreviewMode; }
        protected set
        {
            isPreviewMode = value;
            UpdateVisuals();
        }
    }
    protected GameObject modelObject;
    protected Renderer modelRenderer;
    protected Material originalMaterial;
    protected Material previewMaterial;

    public virtual bool CanDeployGround { get; set; }
    public virtual bool CanDeployHill { get; set; }

    private float preventInteractingTime = 0.1f; // 마우스 클릭을 방지하는 시간
    private float lastDeployTime;

    protected virtual void Awake()
    {
        InitializeVisual();
        Faction = Faction.Ally; // 배치 가능한 요소는 모두 아군으로 간주
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

        // A ?? B : A가 null일 경우 B를 사용
        CanDeployGround = Data?.canDeployOnGround ?? false; 
        CanDeployHill = Data?.canDeployOnHill ?? false;

        InitialDeploymentCost = currentStats.DeploymentCost; // 초기 배치 코스트 설정
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
            base.UpdateCurrentTile();
            CurrentTile.SetOccupied(this);
            transform.position = SetPosition(position);
            InitializeHP();
            lastDeployTime = Time.time;
        }
    }

    /// <summary>
    /// 타일 위에서의 실제 배치 위치 조정
    /// </summary>
    protected Vector3 SetPosition(Vector3 worldPosition)
    {
        if (this is Barricade)
        {
            return worldPosition + Vector3.up * 0.1f;
        }
        else
        {
            return worldPosition + Vector3.up * 0.5f;
        }
    }

    public virtual void Retreat()
    {
        Die();
    }

    protected override void Die()
    {
        if (IsDeployed)
        {
            IsDeployed = false;
            DeployableManager.Instance.OnDeployableRemoved(this);
            if (CurrentTile != null)
            {
                CurrentTile.ClearOccupied(); // 타일에 배치된 요소 제거
            }

            base.Die();
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
        originalMaterial = new Material(modelRenderer.sharedMaterial); // 복사해서 저장 (참조에 의한 변형 가능성 때문에)
        modelRenderer.material = originalMaterial;

        // 프리뷰 머티리얼 설정
        previewMaterial = new Material(originalMaterial);
        previewMaterial.SetFloat("_Mode", 3); // TransParent 모드로 설정
        previewMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha); // 소스 블렌딩 모드 설정. 알파값을 사용해 블렌딩
        previewMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha); // 대상 블렌딩 모드 설정. (1 - 소스 알파값)으로 블렌딩
        previewMaterial.SetInt("_ZWrite", 0); // Z버퍼 비활성화. 투명 객체는 사용하지 않는다고 함
        previewMaterial.DisableKeyword("_ALPHATEST_ON"); // 알파 테스트 모드 비활성화
        previewMaterial.EnableKeyword("_ALPHABLEND_ON"); // 알파 블렌딩 모드 활성화
        previewMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON"); //알파 프리멀티플라이 모드 비활성화
        previewMaterial.renderQueue = 3000; // 렌더 큐 설정. 불투명 객체들 뒤에 그려지게 함

        Color previewColor = previewMaterial.color;
        previewColor.a = 0.8f;
        previewMaterial.color = previewColor;
    }

    /// <summary>
    /// 배치된 유닛 클릭 시의 동작
    /// 배치 완료 시에 커서가 배치 가능한 유닛 위에 있는 상황이라면 동작할 수 있음
    /// </summary>
    public virtual void OnClick()
    {
        // 커서가 배치 가능한 유닛 위에 배치 직후에 있는 상황
        if (Time.time - lastDeployTime < preventInteractingTime)
        {
            DeployableManager.Instance.CancelPlacement();
            return;
        }

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
        if (IsPreviewMode)
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

    /// <summary>
    /// Data, Stat이 엔티티마다 다르기 때문에 자식 메서드에서 재정의가 항상 필요
    /// </summary>
    protected override void InitializeUnitProperties()
    {
        // 현재 체력, 최대 체력 설정 - Deploy 메서드 참조

        // 현재 위치를 기반으로 한 타일 설정
        base.UpdateCurrentTile();
        Prefab = Data.prefab;
    }

    /// <summary>
    /// 시각화 요소들을 초기화
    /// </summary>
    protected virtual void InitializeVisual()
    {
        modelObject = transform.Find("Model").gameObject;
        modelRenderer = modelObject.GetComponent<Renderer>();
        SetupPreviewMaterial();
    }

    protected override void InitializeHP()
    {
        MaxHealth = currentStats.Health;
        CurrentHealth = MaxHealth;
    }
}

#nullable restore
