using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GrassSpawner : MonoBehaviour
{
    [Header("生成设置（重要设置）")]
    [Tooltip("草块所在的Tilemap，获取所有绘制草块Tilemap的引用")]
    public Tilemap grassTilemap;

    [Tooltip("草块对应的Tile资源（用于识别草块）")]
    public TileBase grassTile;

    [Tooltip("要生成的预制体（如宝箱）")]
    public GameObject spawnPrefab;

    [Header("生成规则")]
    [Tooltip("生成间隔（秒）")]
    public float spawnInterval = 5f;

    [Tooltip("最多同时存在的数量")]
    public int maxCount = 5;

    [Tooltip("碰撞检测半径（建议0.5-1，越大越宽松）")]
    public float checkRadius = 0.6f;  // 检测半径（不是实际碰撞范围）

    [Tooltip("需要避开的图层（避免与草块碰撞体碰撞的图层！）")]
    public LayerMask obstacleLayers;  // 关键设置：勾选草块碰撞图层（如"Ground"）

    [Header("位置微调（防止与草块重叠）")]
    [Tooltip("额外的Y偏移量（建议0.3-0.5，确保远离草块碰撞体）")]
    public float extraYOffset = 0.35f;  // 适当增加Y偏移

    [Tooltip("生成失败时的重试次数（避免动态碰撞导致位置不可用）")]
    public int maxRetryCount = 3;  // 多次生成尝试

    private List<Vector3> validSpawnPositions = new List<Vector3>();
    private int currentCount = 0;

    private void Start()
    {
        CheckReferences();

        if (grassTilemap != null && grassTile != null)
        {
            CollectValidSpawnPositions();
            StartCoroutine(SpawnCoroutine());
        }
        else
        {
            Debug.LogError("关键引用缺失：需要grassTilemap和grassTile");
        }
    }

    /// <summary>
    /// 检查必要设置并提示
    /// </summary>
    private void CheckReferences()
    {
        if (grassTilemap == null)
            Debug.LogWarning("⚠️ 未设置Grass Tilemap，请拖入草块Tilemap引用");

        if (grassTile == null)
            Debug.LogWarning("⚠️ 未设置Grass Tile，请拖入草块Tile资源");

        if (spawnPrefab == null)
            Debug.LogWarning("⚠️ 未设置Spawn Prefab，请拖入宝箱预制体！");

        if (obstacleLayers == 0)
            Debug.LogWarning("⚠️ 未设置Obstacle Layers，请勾选草块碰撞体所在图层（如Ground）");
    }

    /// <summary>
    /// 收集草块上方所有有效生成点（强制碰撞检测）
    /// </summary>
    private void CollectValidSpawnPositions()
    {
        validSpawnPositions.Clear();
        if (grassTilemap == null || grassTile == null) return;

        BoundsInt bounds = grassTilemap.cellBounds;
        for (int x = bounds.min.x; x < bounds.max.x; x++)
        {
            for (int y = bounds.min.y; y < bounds.max.y; y++)
            {
                Vector3Int gridPos = new Vector3Int(x, y, 0);
                if (grassTilemap.GetTile(gridPos) == grassTile)
                {
                    // 在草块上方一格计算世界坐标（带偏移）
                    Vector3Int aboveGrid = new Vector3Int(x, y + 1, 0);
                    Vector3 worldPos = grassTilemap.CellToWorld(aboveGrid);
                    Vector3 centerOffset = new Vector3(
                        grassTilemap.cellSize.x / 2f,
                        grassTilemap.cellSize.y / 2f,
                        0
                    );
                    worldPos += centerOffset + new Vector3(0, extraYOffset, 0);

                    // 初次筛选：确保位置没有碰撞
                    if (IsPositionValid(worldPos))
                    {
                        validSpawnPositions.Add(worldPos);
                    }
                }
            }
        }

        Debug.Log($"已收集 {validSpawnPositions.Count} 个有效生成点（草块上方）");
    }

    /// <summary>
    /// 严格检查位置是否有碰撞体（避开草块碰撞）
    /// </summary>
    private bool IsPositionValid(Vector3 pos)
    {
        // 检测范围内是否有任何障碍物（包括草块碰撞体）
        Collider2D[] hits = Physics2D.OverlapCircleAll(pos, checkRadius, obstacleLayers);
        return hits.Length == 0;  // 没有任何碰撞体才为有效
    }

    /// <summary>
    /// 生成协程（带重试机制，避免动态碰撞导致位置不可用）
    /// </summary>
    private IEnumerator SpawnCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            if (currentCount >= maxCount || validSpawnPositions.Count == 0)
                continue;

            // 尝试多次生成（避免动态碰撞导致位置不可用）
            bool spawnSuccess = false;
            int retry = 0;
            while (retry < maxRetryCount && !spawnSuccess)
            {
                // 随机选择生成点
                int randomIndex = Random.Range(0, validSpawnPositions.Count);
                Vector3 spawnPos = validSpawnPositions[randomIndex];

                // 再次严格检查位置，防止动态碰撞
                if (IsPositionValid(spawnPos))
                {
                    // 生成预制体
                    GameObject instance = Instantiate(spawnPrefab, spawnPos, Quaternion.identity);
                    currentCount++;
                    StartCoroutine(WaitForInstanceDestroy(instance));
                    spawnSuccess = true;
                }
                else
                {
                    retry++;
                    Debug.LogWarning($"生成位置[{spawnPos}]有碰撞体，重试第{retry}次...");
                }
            }

            if (!spawnSuccess)
            {
                Debug.LogWarning($"尝试{maxRetryCount}次后失败，跳过本次生成");
            }
        }
    }

    /// <summary>
    /// 等待预制体销毁后减少计数
    /// </summary>
    private IEnumerator WaitForInstanceDestroy(GameObject instance)
    {
        while (instance != null)
            yield return null;

        if (currentCount > 0)
            currentCount--;
    }

    /// <summary>
    /// Scene视图下绘制生成点图示范围（调试用）
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (validSpawnPositions == null) return;

        Gizmos.color = Color.green;
        foreach (var pos in validSpawnPositions)
        {
            Gizmos.DrawWireSphere(pos, checkRadius);  // 显示检测范围
        }
    }
}
