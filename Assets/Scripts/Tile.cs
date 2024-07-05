using UnityEngine;

public enum TileType
{
    Ground,
    Hill,
    Start,
    End,
    Empty
}


public class Tile : MonoBehaviour
{
    public TileType Type { get ; private set; }
    public Vector2Int GridPosition { get; private set; }

    public bool IsWalkable => Type != TileType.Hill && Type != TileType.Empty; // &&이 맞음. 

    public bool IsOccupied { get; private set; } // 해당 타일에 이미 배치가 되었는가

    private Renderer tileRenderer;
    //private Material originalMaterial;
    private Color originalColor;
    private static readonly int ColorProperty = Shader.PropertyToID("_Color");

    // 길찾기 알고리즘을 위한 속성들
    public int GCost { get; set; }
    public int HCost { get; set; }
    public int FCost => GCost + HCost;
    public Tile Parent { get; set; }

    private void Awake()
    {
        // 자식 오브젝트 Cube의 Renderer를 가져온다.
        tileRenderer = GetComponentInChildren<Renderer>();
        if (tileRenderer != null)
        {
            originalColor = tileRenderer.material.GetColor(ColorProperty);
        }
    }

    public void Initialize(TileType type, Vector2Int gridPosition, float height)
    {
        Type = type;
        GridPosition = gridPosition;
        SetHeight(height);
        UpdateVisuals();
    }

    public void SetHeight(float height)
    {
        transform.localScale = new Vector3(transform.localScale.x, height, transform.localScale.z);
        transform.localPosition = new Vector3(transform.localPosition.x, height / 2f, transform.localPosition.z);
    }

    private void UpdateVisuals()
    {
        if (tileRenderer == null) return;

        SetTileColor(GetColorForTileType());
    }

    private void SetTileColor(Color color)
    {
        if (tileRenderer != null && tileRenderer.material != null) 
        {
            tileRenderer.material.SetColor(ColorProperty, color);
        }
    }

    private Color GetColorForTileType()
    {
        return Type switch
        {
            TileType.Ground => Color.gray,
            TileType.Hill => Color.gray,
            TileType.Start => Color.red,
            TileType.End => Color.blue,
            _ => originalColor
        };
    }

    public bool CanPlaceOperator()
    {
        return (IsOccupied == false) && (Type == TileType.Ground || Type == TileType.Hill);
    }

    public void SetOccupied(bool occupied)
    {
        IsOccupied = occupied;
    }

    public void Highlight(Color color)
    {
        if (tileRenderer != null)
        {
            // 기존 머티리얼의 색 변경
            tileRenderer.material.color = color;
        } 
    }

    public void ResetHighlight()
    {
        if (tileRenderer != null)
        {
            Debug.Log("Tile.cs : ResetHighlight 작동");
            SetTileColor(GetColorForTileType());
        }
    }
}
