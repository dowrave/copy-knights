using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ���� �гο��� ���������� ���Ǵ� ��ɵ��� ��Ƽ� �����صӴϴ�
/// </summary>
public class UIHelper : MonoBehaviour
{
    [Header("Attack Range Tile Prefabs")]
    [SerializeField] private Image filledTilePrefab = default!;
    [SerializeField] private Image outlineTilePrefab = default!;
    [SerializeField] private Image highlightTilePrefab = default!;

    // �̱��� ����
    public static UIHelper? Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public class AttackRangeHelper
    {
        private List<Image> rangeTiles = new List<Image>();
        private RectTransform containerRect;
        private float centerOffset; // �����̳ʿ� ���� �߾� �׸����� x ��ġ
        private float tileSize;

        // �����ڿ��� ������ ������ �����ϰ� �����̳ʿ� �����¸� �޵��� ����
        public AttackRangeHelper(RectTransform container, float offset, float? tileSize)
        {
            containerRect = container;
            centerOffset = offset;

            // Ÿ�� ũ��� ������ �� ������, ���ٸ� �⺻ �������� ������ �̿�
            this.tileSize = tileSize ?? Instance!.filledTilePrefab.rectTransform.rect.width;
        }

        private Image CreateTile(Vector2Int gridPos, bool isCenter, bool isHighlight)
        {
            // �������� ���� Instance���� ������
            Image tilePrefab = isCenter ? Instance!.filledTilePrefab :
                             isHighlight ? Instance!.highlightTilePrefab :
                             Instance!.outlineTilePrefab;
            Image tile = Instantiate(tilePrefab, containerRect);

            // Ÿ�� ũ�� ����
            RectTransform tileRect = tile.GetComponent<RectTransform>();
            tileRect.sizeDelta = new Vector2(tileSize, tileSize);

            float interval = tileSize / 4f;
            float gridX = gridPos.x * (tileSize + interval);
            float gridY = gridPos.y * (tileSize + interval);

            tile.rectTransform.anchoredPosition = new Vector2(
                gridX - centerOffset,
                gridY
            );

            rangeTiles.Add(tile);
            return tile;
        }

        // �⺻ ���� ������ ǥ��
        public void ShowBasicRange(List<Vector2Int> attackableTiles)
        {
            ClearTiles();
            CreateCenterTile();

            foreach (Vector2Int pos in attackableTiles)
            {
                // ���� ���� - ������ ������ ���� ���� ������ ���� ������ ����
                Vector2Int convertedPos = new Vector2Int(-pos.x, -pos.y);
                if (convertedPos != Vector2Int.zero)
                {
                    CreateTile(convertedPos, false, false);
                }
            }
        }

        private void CreateCenterTile()
        {
            CreateTile(Vector2Int.zero, true, false);
        }

        public void ClearTiles()
        {
            foreach (var tile in rangeTiles)
            {
                Destroy(tile.gameObject);
            }
            rangeTiles.Clear();
        }

        // �⺻ ������ �ر� ������ �Բ� ǥ��
        public void ShowRangeWithUnlocks(List<Vector2Int> baseTiles, List<Vector2Int> additionalTiles)
        {
            ShowBasicRange(baseTiles);

            if (additionalTiles != null)
            {
                foreach (Vector2Int pos in additionalTiles)
                {
                    Vector2Int convertedPos = new Vector2Int(-pos.x, -pos.y);
                    CreateTile(convertedPos, false, true);
                }
            }
        }
    }

    public AttackRangeHelper CreateAttackRangeHelper(RectTransform container, float offset, float? tileSize = null)
    {
        return new AttackRangeHelper(container, offset, tileSize);
    }
}