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

    [Tooltip("碰撞检测扩展值（基于物体大小的额外缓冲，建议0.1-0.3）")]
    public float collisionBuffer = 0.2f;  // 增加检测缓冲，避免边缘重叠

    [Tooltip("需要避开的图层（避免与草块碰撞体碰撞的图层！）")]
    public LayerMask obstacleLayers;

    [Header("位置微调（防止与草块重叠）")]
    [Tooltip("Y轴基础偏移（相对于草块单元格高度的比例，0.5=单元格中心）")]
    [Range(0.3f, 1f)] public float yOffsetRatio = 0.6f;  // 基于单元格高度的比例偏移

    [Tooltip("额外Y轴偏移（世界单位，在比例偏移基础上增加）")]
    public float extraYOffset = 0.2f;

    [Tooltip("生成失败时的重试次数")]
    public int maxRetryCount = 5;  // 增加重试次数，提高成功率

    private List<Vector3> validSpawnPositions = new List<Vector3>();
    private int currentCount = 0;
    private Vector2 prefabSize;  // 缓存生成物体的碰撞体大小


    private void Start()
    {
        CheckReferences();
        CachePrefabSize();  // 缓存生成物体的尺寸

        if (grassTilemap != null && grassTile != null && spawnPrefab != null)
        {
            CollectValidSpawnPositions();
            StartCoroutine(SpawnCoroutine());
        }
        else
        {
            Debug.LogError("关键引用缺失：需要grassTilemap、grassTile和spawnPrefab");
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
            Debug.LogWarning("⚠️ 未设置Spawn Prefab，请拖入生成预制体！");

        if (obstacleLayers == 0)
            Debug.LogWarning("⚠️ 未设置Obstacle Layers，请勾选草块碰撞体所在图层（如Ground）");
    }

    /// <summary>
    /// 缓存生成预制体的碰撞体大小（用于精确碰撞检测）
    /// </summary>
    private void CachePrefabSize()
    {
        if (spawnPrefab == null) return;

        Collider2D collider = spawnPrefab.GetComponent<Collider2D>();
        if (collider != null)
        {
            // 计算预制体碰撞体的世界空间尺寸（考虑缩放）
            Vector3 lossyScale = spawnPrefab.transform.lossyScale;
            prefabSize = new Vector2(
                collider.bounds.size.x * lossyScale.x,
                collider.bounds.size.y * lossyScale.y
            );
            // 增加缓冲，避免边缘重叠
            prefabSize += Vector2.one * collisionBuffer;
        }
        else
        {
            Debug.LogWarning("⚠️ 生成预制体没有Collider2D，使用默认碰撞检测尺寸");
            prefabSize = Vector2.one * (0.8f + collisionBuffer);  // 默认尺寸
        }
    }

    /// <summary>
    /// 收集草块上方所有有效生成点（精确计算位置）
    /// </summary>
    private void CollectValidSpawnPositions()
    {
        validSpawnPositions.Clear();
        if (grassTilemap == null || grassTile == null) return;

        // 计算草块Tilemap的实际单元格尺寸（考虑缩放）
        Vector3 actualCellSize = grassTilemap.cellSize;
        actualCellSize.x *= grassTilemap.transform.lossyScale.x;
        actualCellSize.y *= grassTilemap.transform.lossyScale.y;

        BoundsInt bounds = grassTilemap.cellBounds;
        for (int x = bounds.min.x; x < bounds.max.x; x++)
        {
            for (int y = bounds.min.y; y < bounds.max.y; y++)
            {
                Vector3Int gridPos = new Vector3Int(x, y, 0);
                if (grassTilemap.GetTile(gridPos) == grassTile)
                {
                    // 计算草块单元格中心位置（世界坐标）
                    Vector3 cellCenter = grassTilemap.GetCellCenterWorld(gridPos);

                    // 计算生成位置：草块中心上方（基于比例+额外偏移）
                    Vector3 spawnPos = new Vector3(
                        cellCenter.x,  // X轴保持单元格中心
                        cellCenter.y + (actualCellSize.y * yOffsetRatio) + extraYOffset,  // Y轴偏移（关键优化）
                        0
                    );

                    // 验证位置是否有效（避开草块碰撞体）
                    if (IsPositionValid(spawnPos))
                    {
                        validSpawnPositions.Add(spawnPos);
                    }
                }
            }
        }

        Debug.Log($"已收集 {validSpawnPositions.Count} 个有效生成点（草块上方安全区域）");
    }

    /// <summary>
    /// 精确检查位置是否有碰撞（基于生成物体的实际大小）
    /// </summary>
    private bool IsPositionValid(Vector3 pos)
    {
        // 使用矩形检测，更贴合物体实际形状（比圆形更精确）
        Collider2D[] hits = Physics2D.OverlapBoxAll(
            pos,
            prefabSize,  // 基于物体大小的检测范围
            0f,
            obstacleLayers
        );
        return hits.Length == 0;  // 无碰撞则有效
    }

    /// <summary>
    /// 生成协程（带重试机制）
    /// </summary>
    private IEnumerator SpawnCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            if (currentCount >= maxCount || validSpawnPositions.Count == 0)
                continue;

            bool spawnSuccess = false;
            int retry = 0;
            while (retry < maxRetryCount && !spawnSuccess)
            {
                int randomIndex = Random.Range(0, validSpawnPositions.Count);
                Vector3 spawnPos = validSpawnPositions[randomIndex];

                // 再次验证位置（防止动态碰撞）
                if (IsPositionValid(spawnPos))
                {
                    GameObject instance = Instantiate(spawnPrefab, spawnPos, Quaternion.identity);
                    currentCount++;
                    StartCoroutine(WaitForInstanceDestroy(instance));
                    spawnSuccess = true;
                }
                else
                {
                    retry++;
                    Debug.LogWarning($"生成位置[{spawnPos}]有碰撞，重试第{retry}次...");
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
    /// Scene视图绘制生成点和检测范围（调试用）
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (validSpawnPositions == null || validSpawnPositions.Count == 0) return;

        // 绘制生成点和检测范围（矩形更直观）
        Gizmos.color = new Color(0, 1, 0, 0.5f);  // 半透明绿色
        foreach (var pos in validSpawnPositions)
        {
            // 绘制检测范围矩形
            Gizmos.DrawWireCube(pos, prefabSize);
            // 绘制生成点
            Gizmos.DrawSphere(pos, 0.1f);
        }
    }
}