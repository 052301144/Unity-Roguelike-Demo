using UnityEngine;

/// <summary>
/// 简单宝箱：开启后按掉落表生成掉落物。
/// </summary>
public class Chest : MonoBehaviour
{
    [Header("Drop Settings")]
    [SerializeField] private DropTable dropTable;
    [SerializeField] private Transform dropPoint; // 可选，未指定则使用宝箱位置
    [SerializeField] private bool openOnce = true;

    [Header("Animation/VFX (optional)")]
    [SerializeField] private Animator animator;
    [SerializeField] private string openTrigger = "Open";

    private bool opened;

    /// <summary>
    /// 外部调用开启（交互/动画事件）。
    /// </summary>
    public void Open()
    {
        if (opened && openOnce) return;
        opened = true;

        if (animator != null && !string.IsNullOrEmpty(openTrigger))
        {
            animator.SetTrigger(openTrigger);
        }

        SpawnDrops();
    }

    private void SpawnDrops()
    {
        if (dropTable == null)
        {
            Debug.LogWarning($"[Chest] {name} 未配置掉落表，跳过掉落。");
            return;
        }

        if (DropManager.Instance == null)
        {
            Debug.LogWarning("[Chest] DropManager 未找到，无法生成掉落。");
            return;
        }

        Vector3 pos = dropPoint != null ? dropPoint.position : transform.position;
        DropManager.Instance.SpawnDropsFromTable(dropTable, pos);
    }

    /// <summary>
    /// 可选：重置宝箱状态（如需重复开启的宝箱）。
    /// </summary>
    public void ResetChest()
    {
        opened = false;
        if (animator != null && !string.IsNullOrEmpty(openTrigger))
        {
            animator.ResetTrigger(openTrigger);
        }
    }
}
