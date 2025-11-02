using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GrassSpawner : MonoBehaviour
{
    [Header("核心引用（必须设置）")]
    [Tooltip("草块所在的Tilemap（场景中绘制草块的Tilemap对象）")]
    public Tilemap grassTilemap;

    [Tooltip("草块对应的Tile资源（用于识别草块）")]
    public TileBase grassTile;

    [Tooltip("要生成的预制体（如宝箱）")]
    public GameObject spawnPrefab;

    [Header("生成规则")]
    [Tooltip("生成间隔（秒）")]
    public float spawnInterval = 5f;

    [Tooltip("最大同时存在数量")]
    public int maxCount = 5;

    [Tooltip("碰撞检测半径（建议0.5-1，越大检测越严格）")]
    public float checkRadius = 0.6f;  // 增大检测半径，覆盖更多碰撞范围

    [Tooltip("需要避开的图层（必须包含草块碰撞体所在图层！）")]
    public LayerMask obstacleLayers;  // 关键：需勾选草块的碰撞图层（如"Ground"）

    [Header("位置微调（解决卡入草坪）")]
    [Tooltip("额外向上偏移量（建议0.3-0.5，确保远离草块碰撞体）")]
    public float extraYOffset = 0.35f;  // 适当增大基础偏移

    [Tooltip("生成失败时的重试次数（避免个别位置卡入）")]
    public int maxRetryCount = 3;  // 新增：重试机制

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
            Debug.LogError("核心引用缺失，请检查grassTilemap和grassTile！");
        }
    }

    /// <summary>
    /// 检查必要引用并提示
    /// </summary>
    private void CheckReferences()
    {
        if (grassTilemap == null)
            Debug.LogWarning("?? 未设置Grass Tilemap，请拖入草块Tilemap对象！");

        if (grassTile == null)
            Debug.LogWarning("?? 未设置Grass Tile，请拖入草块Tile资源！");

        if (spawnPrefab == null)
            Debug.LogWarning("?? 未设置Spawn Prefab，请拖入宝箱预制体！");

        if (obstacleLayers == 0)
            Debug.LogWarning("?? 未设置Obstacle Layers，请勾选草块碰撞体所在图层（如Ground）！");
    }

    /// <summary>
    /// 收集草块上方的有效生成点（强化碰撞检测）
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
                    // 计算草块上方一格的世界坐标（带偏移）
                    Vector3Int aboveGrid = new Vector3Int(x, y + 1, 0);
                    Vector3 worldPos = grassTilemap.CellToWorld(aboveGrid);
                    Vector3 centerOffset = new Vector3(
                        grassTilemap.cellSize.x / 2f,
                        grassTilemap.cellSize.y / 2f,
                        0
                    );
                    worldPos += centerOffset + new Vector3(0, extraYOffset, 0);

                    // 首次筛选：确保位置无碰撞
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
    /// 严格检查位置是否有碰撞体（包括草块自身）
    /// </summary>
    private bool IsPositionValid(Vector3 pos)
    {
        // 检测范围内是否有任何障碍物（包括草块碰撞体）
        Collider2D[] hits = Physics2D.OverlapCircleAll(pos, checkRadius, obstacleLayers);
        return hits.Length == 0;  // 无任何碰撞体才视为有效
    }

    /// <summary>
    /// 生成协程（新增重试机制，解决个别位置卡入）
    /// </summary>
    private IEnumerator SpawnCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            if (currentCount >= maxCount || validSpawnPositions.Count == 0)
                continue;

            // 尝试多次生成，避免个别位置卡入
            bool spawnSuccess = false;
            int retry = 0;
            while (retry < maxRetryCount && !spawnSuccess)
            {
                // 随机选择生成点
                int randomIndex = Random.Range(0, validSpawnPositions.Count);
                Vector3 spawnPos = validSpawnPositions[randomIndex];

                // 再次严格检查位置（防止动态碰撞）
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
                Debug.LogWarning($"连续{maxRetryCount}次生成失败，跳过本次生成");
            }
        }
    }

    /// <summary>
    /// 等待预制体销毁后更新计数
    /// </summary>
    private IEnumerator WaitForInstanceDestroy(GameObject instance)
    {
        while (instance != null)
            yield return null;

        if (currentCount > 0)
            currentCount--;
    }

    /// <summary>
    /// Scene视图绘制生成点和检测范围（方便调试）
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