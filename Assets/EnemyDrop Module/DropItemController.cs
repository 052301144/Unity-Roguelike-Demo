using System.Collections; // 引入协程命名空间
using UnityEngine; // 引入Unity引擎命名空间

/// <summary>
/// 掉落物品控制器 - 控制掉落在地面上的物品行为状态
/// </summary>
public class DropItemController : MonoBehaviour
{
    [Header("物品信息")] // 物品相关信息部分
    public DropItem itemData;            // 物品数据对象引用
    public DropItemType itemType;        // 物品类型枚举

    [Header("视觉效果")] // 视觉效果相关部分
    public SpriteRenderer spriteRenderer; // 渲染物品图像的精灵渲染器
    public float hoverHeight = 0.2f;     // 悬浮效果的高度范围
    public float hoverSpeed = 2f;        // 悬浮效果的移动速度

    [Header("拾取设置")] // 拾取相关配置部分
    public float pickupDelay = 0.5f;     // 生成后允许拾取的延迟时间

    // 公共属性 - 外部可以通过此属性获取拾取状态
    public bool CanBePickedUp { get; private set; } = false; // 标记物品是否可以被拾取

    // 私有字段 - 内部状态管理
    private float lifetime;              // 物品剩余存活时间
    private Vector3 startPosition;       // 物品的初始位置，用于悬浮动画计算
    private bool isPickedUp = false;     // 标记物品是否已被拾取

    void Awake()
    {
        // 如果没有手动指定SpriteRenderer，自动查找
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            
            // 如果没找到，尝试从子对象中查找
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
            
            // 如果还是没找到，记录警告
            if (spriteRenderer == null)
            {
                Debug.LogWarning($"[DropItemController] {gameObject.name} 未找到SpriteRenderer组件。请确保预制体上有SpriteRenderer组件。");
            }
        }
    }
    
    /// <summary>
    /// Start方法 - 在对象激活时调用
    /// </summary>
    void Start()
    {
        // 确保SpriteRenderer已初始化（如果Awake中未找到，再次尝试）
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }
    
        // 记录初始位置
        startPosition = transform.position;
        // 启动延迟允许拾取的协程
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
        // 如果物品已被拾取，不执行任何更新逻辑
        if (isPickedUp) return;

        // 悬浮动画 - 只在可以拾取时执行
        if (CanBePickedUp)
        {
            // 使用正弦函数计算Y轴偏移，产生上下浮动效果
            float newY = startPosition.y + Mathf.Sin(Time.time * hoverSpeed) * hoverHeight;
            // 更新物品位置，只改变Y轴
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }

        // 生命周期管理 - 如果设置了最大存活时间
        if (lifetime > 0)
        {
            // 减少剩余时间
            lifetime -= Time.deltaTime;
            // 如果时间到了，销毁物品
            if (lifetime <= 0)
            {
                DestroyItem(); // 销毁掉落物品
            }
        }
    }

    /// <summary>
    /// 初始化掉落物品
    /// </summary>
    /// <param name="data">物品数据对象</param>
    /// <param name="lifeTime">物品存活时间</param>
    public void Initialize(DropItem data, float lifeTime)
    {
        itemData = data;         // 设置物品数据
        lifetime = lifeTime;     // 设置存活时间
        itemType = data.itemType; // 设置物品类型

        // 设置游戏对象名称，便于在场景中识别
        gameObject.name = data.itemName + " (Drop)";
    }

    /// <summary>
    /// 延迟允许拾取的协程
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
        // 检查物品是否可以被拾取且未被拾取
        if (!CanBePickedUp || isPickedUp) return;

        // 标记为已拾取，防止重复拾取
        isPickedUp = true;

        // 应用物品效果（恢复生命值、魔法值、增加金币等）
        ApplyItemEffect();

        // 销毁掉落物品对象
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
                // 触发生命值恢复事件
                DropManager.Instance.TriggerHealthRestored(amount);
                break;

            case DropItemType.Mana:
                // 触发魔法值恢复事件
                DropManager.Instance.TriggerManaRestored(amount);
                break;

            case DropItemType.Coin:
                // 触发金币收集事件
                DropManager.Instance.TriggerCoinCollected(amount);
                break;
        }
    }

    /// <summary>
    /// 销毁掉落物品
    /// </summary>
    private void DestroyItem()
    {
        // 从管理器的活动列表中移除
        if (DropManager.Instance != null)
        {
            DropManager.Instance.RemoveFromActiveDrops(gameObject);
        }

        // 销毁游戏对象
        Destroy(gameObject);
    }

    /// <summary>
    /// 2D碰撞器触发进入事件
    /// </summary>
    /// <param name="other">触发碰撞的另一个碰撞器</param>
    void OnTriggerEnter2D(Collider2D other)
    {
        // 检查是否可以被拾取且未被拾取且碰撞对象是玩家
        if (CanBePickedUp && !isPickedUp && other.CompareTag("Player"))
        {
            // 执行拾取操作
            Pickup(other.gameObject);
        }
    }

    /// <summary>
    /// 在Scene视图中绘制Gizmos，用于可视化调试
    /// </summary>
    void OnDrawGizmos()
    {
        // 如果物品可以被拾取且未被拾取，绘制蓝色线框球体
        if (CanBePickedUp && !isPickedUp)
        {
            Gizmos.color = Color.blue; // 设置Gizmos颜色为蓝色
            // 在物品位置绘制线框球体，半径0.3单位
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }
    }
}
