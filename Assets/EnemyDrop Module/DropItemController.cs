using System.Collections;
using UnityEngine;

/// <summary>
/// 掉落物品控制器 - 控制掉落在地面上的物品行为状态
/// 重写版本：修复显示问题和初始化顺序问题
/// </summary>
public class DropItemController : MonoBehaviour
{
    [Header("物品信息")]
    [SerializeField] private DropItem itemData;            // 物品数据对象引用
    [SerializeField] private DropItemType itemType;         // 物品类型枚举

    [Header("视觉效果")]
    [SerializeField] private SpriteRenderer spriteRenderer; // 渲染物品图像的精灵渲染器
    [SerializeField] private float hoverHeight = 0.2f;      // 悬浮效果的高度范围
    [SerializeField] private float hoverSpeed = 2f;          // 悬浮效果的移动速度

    [Header("拾取设置")]
    [SerializeField] private float pickupDelay = 0.5f;      // 生成后允许拾取的延迟时间

    // 公共属性
    public bool CanBePickedUp { get; private set; } = false;

    // 私有字段
    private float lifetime;
    private Vector3 startPosition;
    private bool isPickedUp = false;
    private bool isInitialized = false;

    void Awake()
    {
        // 自动查找SpriteRenderer组件
        EnsureSpriteRenderer();
        
        // 记录初始位置（在初始化之前记录，避免被Initialize覆盖）
        startPosition = transform.position;
    }

    void Start()
    {
        // 如果还没有初始化，确保SpriteRenderer有效
        if (!isInitialized)
        {
            EnsureSpriteRenderer();
            ValidateRendering();
        }
        
        // 启动延迟拾取的协程
        StartCoroutine(EnablePickupAfterDelay());
    }

    void Update()
    {
        if (isPickedUp || !isInitialized) return;

        // 悬浮动画（保持Z坐标为0）
        if (CanBePickedUp)
        {
            float newY = startPosition.y + Mathf.Sin(Time.time * hoverSpeed) * hoverHeight;
            transform.position = new Vector3(transform.position.x, newY, 0f);
        }
        else
        {
            // 即使不能拾取时，也确保Z坐标为0
            if (Mathf.Abs(transform.position.z) > 0.01f)
            {
                Vector3 pos = transform.position;
                pos.z = 0f;
                transform.position = pos;
            }
        }

        // 生命周期管理
        if (lifetime > 0)
        {
            lifetime -= Time.deltaTime;
            if (lifetime <= 0)
            {
                DestroyItem();
            }
        }
    }

    /// <summary>
    /// 初始化掉落物品 - 必须在实例化后立即调用
    /// </summary>
    public void Initialize(DropItem data, float lifeTime)
    {
        if (data == null)
        {
            Debug.LogError($"[DropItemController] {gameObject.name} 初始化失败：物品数据为null");
            return;
        }

        itemData = data;
        lifetime = lifeTime;
        itemType = data.itemType;
        gameObject.name = $"{data.itemName} (Drop)";

        // 确保SpriteRenderer已初始化
        EnsureSpriteRenderer();

        // 修复Z坐标问题 - 确保掉落物在正确的Z深度
        Vector3 pos = transform.position;
        pos.z = 0f; // 强制Z坐标为0，确保在2D相机视野内
        transform.position = pos;

        // 设置sprite - 这是关键修复
        SetupSprite();

        // 验证渲染设置
        ValidateRendering();

        isInitialized = true;

        // 重新记录位置（已修复Z坐标）
        startPosition = transform.position;

        Debug.Log($"[DropItemController] {gameObject.name} 初始化完成 - Sprite: {(spriteRenderer != null && spriteRenderer.sprite != null ? spriteRenderer.sprite.name : "NULL")}, Position: {transform.position}, SortingLayer: {SortingLayer.IDToName(spriteRenderer.sortingLayerID)}, SortingOrder: {spriteRenderer.sortingOrder}");
    }

    /// <summary>
    /// 确保SpriteRenderer组件存在
    /// </summary>
    private void EnsureSpriteRenderer()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (spriteRenderer == null)
            {
                Debug.LogError($"[DropItemController] {gameObject.name} 未找到SpriteRenderer组件！");
            }
        }
    }

    /// <summary>
    /// 设置Sprite - 修复显示问题的核心方法
    /// </summary>
    private void SetupSprite()
    {
        if (spriteRenderer == null)
        {
            Debug.LogError($"[DropItemController] {gameObject.name} SpriteRenderer为空，无法设置sprite");
            return;
        }

        // 优先级1：如果物品数据中指定了图标，使用指定的图标
        if (itemData != null && itemData.itemIcon != null)
        {
            spriteRenderer.sprite = itemData.itemIcon;
            Debug.Log($"[DropItemController] {gameObject.name} 使用itemData中的sprite: {itemData.itemIcon.name}");
            return;
        }

        // 优先级2：保持预制体的sprite（如果预制体本身有sprite）
        if (spriteRenderer.sprite != null)
        {
            Debug.Log($"[DropItemController] {gameObject.name} 使用预制体的sprite: {spriteRenderer.sprite.name}");
            return;
        }

        // 如果都没有，记录错误
        Debug.LogError($"[DropItemController] {gameObject.name} 警告：Sprite为空！物品将不可见。请检查：1)预制体的SpriteRenderer是否有sprite 2)DropItem的itemIcon是否设置");
        
        // 检查是否是占位符sprite
        if (spriteRenderer.sprite != null && spriteRenderer.sprite.name == "Square")
        {
            Debug.LogWarning($"[DropItemController] {gameObject.name} 使用的是占位符sprite 'Square'，这可能是Unity默认sprite。请确保使用正确的金币sprite资源。");
        }
    }

    /// <summary>
    /// 验证渲染设置，确保物品可见
    /// </summary>
    private void ValidateRendering()
    {
        if (spriteRenderer == null) return;

        // 确保SpriteRenderer启用
        if (!spriteRenderer.enabled)
        {
            Debug.LogWarning($"[DropItemController] {gameObject.name} SpriteRenderer被禁用，已自动启用");
            spriteRenderer.enabled = true;
        }

        // 确保有sprite
        if (spriteRenderer.sprite == null)
        {
            Debug.LogError($"[DropItemController] {gameObject.name} Sprite为空！物品将不可见！");
        }

        // 验证SortingLayer（确保不是无效的层）
        string sortingLayerName = SortingLayer.IDToName(spriteRenderer.sortingLayerID);
        if (string.IsNullOrEmpty(sortingLayerName))
        {
            Debug.LogWarning($"[DropItemController] {gameObject.name} 使用了无效的SortingLayer ID: {spriteRenderer.sortingLayerID}，已设置为Default");
            spriteRenderer.sortingLayerID = 0; // 设置为Default层
        }

        // 确保颜色不透明
        if (spriteRenderer.color.a < 1f)
        {
            Debug.LogWarning($"[DropItemController] {gameObject.name} SpriteRenderer颜色alpha值小于1，已设置为1");
            spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, 1f);
        }
    }

    private IEnumerator EnablePickupAfterDelay()
    {
        yield return new WaitForSeconds(pickupDelay);
        CanBePickedUp = true;
    }

    public void Pickup(GameObject picker)
    {
        if (!CanBePickedUp || isPickedUp || !isInitialized) return;

        isPickedUp = true;
        ApplyItemEffect();
        DestroyItem();
        
        Debug.Log($"拾取物品: {(itemData != null ? itemData.itemName : "Unknown")}");
    }

    private void ApplyItemEffect()
    {
        if (DropManager.Instance == null || itemData == null) return;

        int amount = itemData.GetDropQuantity();

        switch (itemType)
        {
            // 消耗品类 - 立即生效
            case DropItemType.Health:
                DropManager.Instance.TriggerHealthRestored(amount);
                break;
            case DropItemType.Mana:
                DropManager.Instance.TriggerManaRestored(amount);
                break;
            case DropItemType.Coin:
                DropManager.Instance.TriggerCoinCollected(amount);
                break;
            
            // 装备类 - 添加到背包
            case DropItemType.Weapon:
            case DropItemType.Equipment:
            case DropItemType.Consumable:
                // 创建物品数据并触发拾取事件
                ItemData pickedItem = new ItemData(itemData);
                if (itemType == DropItemType.Weapon)
                {
                    DropManager.Instance.TriggerWeaponPickedUp(pickedItem);
                }
                else if (itemType == DropItemType.Equipment)
                {
                    DropManager.Instance.TriggerEquipmentPickedUp(pickedItem);
                }
                else if (itemType == DropItemType.Consumable)
                {
                    DropManager.Instance.TriggerConsumablePickedUp(pickedItem);
                }
                break;
        }
    }

    private void DestroyItem()
    {
        if (DropManager.Instance != null)
        {
            DropManager.Instance.RemoveFromActiveDrops(gameObject);
        }
        Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (CanBePickedUp && !isPickedUp && other.CompareTag("Player"))
        {
            Pickup(other.gameObject);
        }
    }

    void OnDrawGizmos()
    {
        if (CanBePickedUp && !isPickedUp)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }
    }
}
