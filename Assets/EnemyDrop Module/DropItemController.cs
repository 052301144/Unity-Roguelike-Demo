using System.Collections; // 引入协程相关命名空间
using UnityEngine; // 引入Unity引擎命名空间

/// <summary>
/// 掉落物品控制器 - 控制单个掉落物品的行为和状态
/// </summary>
public class DropItemController : MonoBehaviour
{
    [Header("物品信息")] // 物品基本信息分组
    public DropItem itemData;            // 物品的数据配置
    public DropItemType itemType;        // 物品的类型

    [Header("视觉效果")] // 视觉表现相关分组
    public SpriteRenderer spriteRenderer; // 渲染物品图标的精灵渲染器
    public float hoverHeight = 0.2f;     // 悬浮动画的高度幅度
    public float hoverSpeed = 2f;        // 悬浮动画的速度

    [Header("拾取设置")] // 拾取相关设置分组
    public float pickupDelay = 0.5f;     // 生成后可以拾取的延迟时间

    // 公共属性 - 外部可以读取但不能设置
    public bool CanBePickedUp { get; private set; } = false; // 标记物品是否可以被拾取

    // 私有字段 - 内部状态管理
    private float lifetime;              // 物品剩余存在时间
    private Vector3 startPosition;       // 物品的初始位置（用于悬浮动画）
    private bool isPickedUp = false;     // 标记物品是否已被拾取

    /// <summary>
    /// Start方法 - 在对象首次启用时调用
    /// </summary>
    void Start()
    {
        // 记录初始位置
        startPosition = transform.position;
        // 启动延迟启用拾取的协程
        StartCoroutine(EnablePickupAfterDelay());

        // 设置物品的图标
        if (spriteRenderer != null && itemData != null && itemData.itemIcon != null)
        {
            // 将物品数据中的图标设置到精灵渲染器
            spriteRenderer.sprite = itemData.itemIcon;
        }
    }

    /// <summary>
    /// Update方法 - 每帧调用
    /// </summary>
    void Update()
    {
        // 如果物品已被拾取，不再执行任何更新逻辑
        if (isPickedUp) return;

        // 悬浮动画 - 只有在可以拾取时才执行
        if (CanBePickedUp)
        {
            // 使用正弦函数计算Y轴偏移，创建上下浮动的效果
            float newY = startPosition.y + Mathf.Sin(Time.time * hoverSpeed) * hoverHeight;
            // 更新物品位置，只改变Y轴
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }

        // 生命周期管理 - 如果设置了存在时间
        if (lifetime > 0)
        {
            // 减少剩余时间
            lifetime -= Time.deltaTime;
            // 如果时间耗尽，销毁物品
            if (lifetime <= 0)
            {
                DestroyItem(); // 调用销毁方法
            }
        }
    }

    /// <summary>
    /// 初始化物品
    /// </summary>
    /// <param name="data">物品数据配置</param>
    /// <param name="lifeTime">物品存在时间</param>
    public void Initialize(DropItem data, float lifeTime)
    {
        itemData = data;         // 设置物品数据
        lifetime = lifeTime;     // 设置存在时间
        itemType = data.itemType; // 设置物品类型

        // 设置游戏对象名称，便于在场景中识别
        gameObject.name = data.itemName + " (Drop)";
    }

    /// <summary>
    /// 延迟启用拾取的协程
    /// </summary>
    private IEnumerator EnablePickupAfterDelay()
    {
        // 等待指定的延迟时间
        yield return new WaitForSeconds(pickupDelay);
        // 启用拾取功能
        CanBePickedUp = true;
    }

    /// <summary>
    /// 拾取物品
    /// </summary>
    /// <param name="picker">拾取者的游戏对象</param>
    public void Pickup(GameObject picker)
    {
        // 检查物品是否可以被拾取且尚未被拾取
        if (!CanBePickedUp || isPickedUp) return;

        // 标记为已拾取，防止重复拾取
        isPickedUp = true;

        // 应用物品效果（恢复生命、魔法或增加金币）
        ApplyItemEffect();

        // 销毁物品对象
        DestroyItem();

        // 输出拾取日志
        Debug.Log("拾取物品: " + itemData.itemName);
    }

    /// <summary>
    /// 应用物品效果
    /// </summary>
    private void ApplyItemEffect()
    {
        // 检查掉落管理器是否存在
        if (DropManager.Instance == null) return;

        // 获取掉落数量
        int amount = itemData.GetDropQuantity();

        // 根据物品类型触发不同的事件
        switch (itemType)
        {
            case DropItemType.Health:
                // 触发生命恢复事件
                DropManager.Instance.TriggerHealthRestored(amount);
                break;

            case DropItemType.Mana:
                // 触发魔法恢复事件
                DropManager.Instance.TriggerManaRestored(amount);
                break;

            case DropItemType.Coin:
                // 触发金币收集事件
                DropManager.Instance.TriggerCoinCollected(amount);
                break;
        }
    }

    /// <summary>
    /// 销毁物品
    /// </summary>
    private void DestroyItem()
    {
        // 从掉落管理器的活动列表中移除
        if (DropManager.Instance != null)
        {
            DropManager.Instance.RemoveFromActiveDrops(gameObject);
        }

        // 销毁游戏对象
        Destroy(gameObject);
    }

    /// <summary>
    /// 2D碰撞体触发进入事件
    /// </summary>
    /// <param name="other">触发碰撞的其他碰撞体</param>
    void OnTriggerEnter2D(Collider2D other)
    {
        // 检查是否可以拾取、尚未被拾取且碰撞对象是玩家
        if (CanBePickedUp && !isPickedUp && other.CompareTag("Player"))
        {
            // 执行拾取操作
            Pickup(other.gameObject);
        }
    }

    /// <summary>
    /// 在Scene视图中绘制Gizmos（调试可视化）
    /// </summary>
    void OnDrawGizmos()
    {
        // 如果物品可以被拾取且尚未被拾取，绘制蓝色线框球体
        if (CanBePickedUp && !isPickedUp)
        {
            Gizmos.color = Color.blue; // 设置Gizmos颜色为蓝色
            // 在物品位置绘制线框球体，半径0.3单位
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }
    }
}