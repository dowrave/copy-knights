using UnityEngine;

[ExecuteAlways] // 에디터, 런타임 모두에서 스크립트 실행
public class Tile : MonoBehaviour
{
    public TileData data;
    public Vector2Int GridPosition { get; private set; }
    public bool IsOccupied { get; private set; }

    [SerializeField] private Material baseTileMaterial; // Inspector에서 할당함
    private Renderer tileRenderer;
    private MaterialPropertyBlock propBlock; // 머티리얼 속성을 오버라이드하는 경량 객체. 모든 타일이 동일한 머티리얼을 공유하되 색을 개별적으로 설정할 수 있다.

    // 길찾기 알고리즘을 위한 속성들
    public int GCost { get; set; }
    public int HCost { get; set; }
    public int FCost => GCost + HCost;
    public Tile Parent { get; set; }

    // 오브젝트 활성화마다 호출
    private void OnEnable()
    {
        Initialize();
    }

    // 인스펙터에서 컴포넌트 값이 변경될 때마다 호출 - 에디터에서 바로 확인 가능
    private void OnValidate()
    {
        Initialize();
    }

    private void Initialize()
    {
        // 자식 오브젝트 Cube의 Renderer를 가져온다.
        if (tileRenderer == null)
        {
            tileRenderer = GetComponentInChildren<Renderer>();
        }

        else
        {
            tileRenderer.sharedMaterial = baseTileMaterial;
        }

        propBlock = new MaterialPropertyBlock();
        UpdateVisuals();
    }

    public void SetTileData(TileData tileData, Vector2Int gridPosition)
    {
        data = tileData;
        GridPosition = gridPosition;
        SetScale();
        UpdateVisuals();
    }

    public void SetScale()
    {
        if (data == null) return;
        float height = (data.terrain == TileData.TerrainType.Hill) ? 0.5f : 0.1f;
        transform.localScale = new Vector3(transform.localScale.x * 0.98f, height, transform.localScale.z * 0.98f);
        transform.localPosition = new Vector3(transform.localPosition.x, height / 2f, transform.localPosition.z);
    }

    public float GetHeight()
    {
        return transform.localScale.y;
    }

    private void UpdateVisuals()
    {
        if (tileRenderer == null || data == null) return;

        tileRenderer.GetPropertyBlock(propBlock);
        propBlock.SetColor("_Color", data.tileColor);
        tileRenderer.SetPropertyBlock(propBlock);
        
    }


    public bool CanPlaceOperator()
    {
        return (IsOccupied == false) && (data.canPlaceOperator);
    }

    public void SetOccupied(bool occupied)
    {
        IsOccupied = occupied;
    }

    public void Highlight(Color color)
    {
        if (tileRenderer != null)
        {
            tileRenderer.GetPropertyBlock(propBlock);
            propBlock.SetColor("_Color", color);
            tileRenderer.SetPropertyBlock(propBlock);
        } 
    }

    public void ResetHighlight()
    {
        if (tileRenderer != null)
        {
            UpdateVisuals();
        }
    }
}
