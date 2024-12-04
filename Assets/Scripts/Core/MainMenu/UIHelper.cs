using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 여러 패널에서 공통적으로 사용되는 기능들을 모아서 구현해둡니다
/// </summary>
public class UIHelper : MonoBehaviour
{
    //[Header("Container Reference")]
    //[SerializeField] private RectTransform attackRangeContainer;

    [Header("Attack Range Tile Prefabs")]
    [SerializeField] private Image filledTilePrefab;
    [SerializeField] private Image outlineTilePrefab;
    [SerializeField] private Image highlightTilePrefab;


    // 싱글톤 구현
    public static UIHelper Instance { get; private set; }

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
        private float centerOffset;
        private float tileSize;

        // 생성자에서 프리팹 참조를 제거하고 컨테이너와 오프셋만 받도록 수정
        public AttackRangeHelper(RectTransform container, float offset)
        {
            containerRect = container;
            centerOffset = offset;
            // 타일 크기는 Instance의 프리팹에서 가져옴
            tileSize = Instance.filledTilePrefab.rectTransform.rect.width;
        }

        private Image CreateTile(Vector2Int gridPos, bool isCenter, bool isHighlight)
        {
            // 프리팹은 이제 Instance에서 가져옴
            Image tilePrefab = isCenter ? Instance.filledTilePrefab :
                             isHighlight ? Instance.highlightTilePrefab :
                             Instance.outlineTilePrefab;

            Image tile = GameObject.Instantiate(tilePrefab, containerRect);

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

        // 기본 공격 범위만 표시
        public void ShowBasicRange(List<Vector2Int> attackableTiles)
        {
            ClearTiles();
            CreateCenterTile();

            foreach (Vector2Int pos in attackableTiles)
            {
                // 방향 보정 - 원본이 왼쪽을 보는 것을 오른쪽 보는 것으로 변경
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

        private void ClearTiles()
        {
            foreach (var tile in rangeTiles)
            {
                GameObject.Destroy(tile.gameObject);
            }
            rangeTiles.Clear();
        }

        // 기본 범위와 해금 범위를 함께 표시
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
    public AttackRangeHelper CreateAttackRangeHelper(RectTransform container, float offset)
    {
        return new AttackRangeHelper(container, offset);
    }
}