using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Attack : MonoBehaviour
{
    [Header("攻击属性")]
    [SerializeField] private AttackType attackType = AttackType.Physical; // 攻击类型，默认物理攻击
    [SerializeField] private KeyCode attackKey = KeyCode.J; // 攻击按键，默认为J键
    [SerializeField] private float attackCooldown = 0.5f; // 攻击冷却时间
    [SerializeField] private LayerMask enemyLayer; // 敌人层级

    [Header("长方形攻击范围")]
    [SerializeField] private Transform attackPoint; // 攻击判定点
    [SerializeField] private Vector2 boxSize = new Vector2(2f, 1f); // 长方形攻击区域大小
    [SerializeField] private Vector2 attackOffset = Vector2.zero; // 攻击点偏移量
    [SerializeField] private float attackAngle = 0f; // 攻击区域旋转角度

    [Header("调试设置")]
    [SerializeField] private bool showAttackRangeInGame = true; // 运行时显示攻击范围
    [SerializeField] private Color debugBoxColor = Color.red; // 调试框颜色
    [SerializeField] private bool showDebugInfo = true; // 显示调试信息

    [Header("物理攻击属性")]
    [SerializeField] private float physicalCritRate = 0.1f; // 物理暴击率
    [SerializeField] private float physicalCritDamage = 1.5f; // 物理暴击伤害倍数

    [Header("元素攻击属性")]
    [SerializeField] private float elementalEffectChance = 0.7f; // 元素效果触发概率
    [SerializeField] private float fireDamageDuration = 3f; // 火元素持续伤害时间
    [SerializeField] private float iceFreezeDuration = 2f; // 冰元素冻结时间
    [SerializeField] private float windKnockbackForce = 5f; // 风元素击退力度
    [SerializeField] private float thunderChainRange = 3f; // 雷元素连锁范围

    // 攻击类型枚举
    public enum AttackType
    {
        Physical, // 物理攻击：无视防御，低暴击
        Fire,     // 火元素：持续伤害
        Wind,     // 风元素：击退效果
        Ice,      // 冰元素：冻结控制
        Thunder   // 雷元素：范围连锁
    }

    private float lastAttackTime; // 上次攻击时间
    private Attribute attackerAttribute; // 攻击者属性
    private int facingDirection = 1; // 人物朝向，1为右，-1为左
    private Material debugMaterial; // 调试材质
    private Mesh debugMesh; // 调试网格

    // 属性访问器
    public Vector2 AttackBoxSize => boxSize;
    public bool CanAttack => Time.time >= lastAttackTime + attackCooldown;
    public int CurrentAttack => attackerAttribute != null ? attackerAttribute.Attack : 0;
    public AttackType Type => attackType;
    public float AttackRange => boxSize.x;
    public int FacingDirection => facingDirection;

    // 事件
    public System.Action<GameObject, int, AttackType> OnAttackHit; // 攻击命中事件
    public System.Action<AttackType> OnAttackPerformed; // 攻击执行事件
    public System.Action<AttackType, GameObject> OnElementEffectApplied; // 元素效果应用事件

    // 攻击信息结构体
    public struct AttackInfo
    {
        public GameObject target;
        public int baseDamage;
        public AttackType type;
        public bool isCrit;
        public Vector3 hitPoint;
    }

    private void Awake()
    {
        attackerAttribute = GetComponent<Attribute>();

        // 获取初始朝向
        UpdateFacingDirection();

        // 如果攻击点未设置，使用角色自身位置
        if (attackPoint == null)
        {
            attackPoint = transform;
            Debug.LogWarning("攻击点未设置，使用角色自身位置作为攻击点");
        }

        // 初始化调试资源
        InitializeDebugResources();
    }

    private void InitializeDebugResources()
    {
        // 创建调试材质
        debugMaterial = new Material(Shader.Find("Sprites/Default"));
        debugMaterial.color = debugBoxColor;

        // 创建调试网格
        debugMesh = CreateBoxMesh();
    }

    private Mesh CreateBoxMesh()
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[4];
        Vector2[] uv = new Vector2[4];
        int[] triangles = new int[6];

        float halfWidth = boxSize.x * 0.5f;
        float halfHeight = boxSize.y * 0.5f;

        // 设置顶点
        vertices[0] = new Vector3(-halfWidth, -halfHeight, 0);
        vertices[1] = new Vector3(halfWidth, -halfHeight, 0);
        vertices[2] = new Vector3(halfWidth, halfHeight, 0);
        vertices[3] = new Vector3(-halfWidth, halfHeight, 0);

        // 设置UV
        uv[0] = new Vector2(0, 0);
        uv[1] = new Vector2(1, 0);
        uv[2] = new Vector2(1, 1);
        uv[3] = new Vector2(0, 1);

        // 设置三角形
        triangles[0] = 0;
        triangles[1] = 1;
        triangles[2] = 2;
        triangles[3] = 0;
        triangles[4] = 2;
        triangles[5] = 3;

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;

        return mesh;
    }

    private void Update()
    {
        // 更新人物朝向
        UpdateFacingDirection();

        // 注意：攻击输入现在由 PlayerController 统一处理
        // 攻击实际执行由动画事件触发（在 Attack-01 动画中设置事件）
        // 旧代码保留作为备用（可通过 Inspector 启用，但通常不需要）

    }

    // 更新人物朝向 
    private void UpdateFacingDirection()
    {
        // 不自动变向，不根据localScale和速度自动判断朝向
        // 只允许被PlayerController等外部同步
    }



    // 获取实际攻击点位置（考虑偏移和朝向）
    private Vector2 GetActualAttackPosition()
    {
        Vector2 basePosition = attackPoint != null ? (Vector2)attackPoint.position : (Vector2)transform.position;

        // 应用偏移量，X偏移会根据朝向调整
        Vector2 adjustedOffset = new Vector2(attackOffset.x * facingDirection, attackOffset.y);

        return basePosition + adjustedOffset;
    }

    // 获取攻击区域的旋转角度（考虑朝向）
    private float GetActualAttackAngle()
    {
        return attackAngle * facingDirection;
    }

    public void PerformAttack()
    {
        if (!CanAttack)
        {
            if (showDebugInfo)
            {
                Debug.Log($"攻击冷却中，剩余时间：{lastAttackTime + attackCooldown - Time.time:F2}秒");
            }
            return;
        }

        if (showDebugInfo)
        {
            Debug.Log($"执行攻击，朝向：{(facingDirection > 0 ? "右" : "左")}");
            Debug.Log($"攻击区域：位置{GetActualAttackPosition()}，大小{boxSize}，角度{GetActualAttackAngle()}度");
        }

        OnAttackPerformed?.Invoke(attackType);

        Collider2D[] hitEnemies = GetHitEnemies();

        if (showDebugInfo)
        {
            Debug.Log($"检测到{hitEnemies.Length}个敌人");
        }

        foreach (Collider2D enemy in hitEnemies)
        {
            if (enemy != null && enemy.gameObject != gameObject)
            {
                ProcessAttackHit(enemy.gameObject);
            }
        }

        lastAttackTime = Time.time;
    }

    // 在游戏运行时绘制攻击范围
    private void OnRenderObject()
    {
        if (!showAttackRangeInGame || debugMaterial == null || debugMesh == null)
            return;

        // 设置材质
        debugMaterial.SetPass(0);

        // 计算攻击区域的位置和旋转
        Vector3 attackPos = GetActualAttackPosition();
        Quaternion rotation = Quaternion.Euler(0, 0, GetActualAttackAngle());

        // 绘制网格
        Graphics.DrawMeshNow(debugMesh, attackPos, rotation);
    }

    [ContextMenu("切换攻击范围显示")]
    public void ToggleAttackRangeDisplay()
    {
        showAttackRangeInGame = !showAttackRangeInGame;
        Debug.Log($"攻击范围显示: {showAttackRangeInGame}");
    }

    [ContextMenu("切换调试信息")]
    public void ToggleDebugInfo()
    {
        showDebugInfo = !showDebugInfo;
        Debug.Log($"调试信息显示: {showDebugInfo}");
    }

    [ContextMenu("测试攻击方向")]
    private void TestAttackDirection()
    {
        Debug.Log($"当前朝向: {(facingDirection > 0 ? "右" : "左")}");
        Debug.Log($"攻击点位置: {GetActualAttackPosition()}");
        Debug.Log($"攻击区域角度: {GetActualAttackAngle()}度");
        Debug.Log($"LocalScale: {transform.localScale}");
    }

    [ContextMenu("强制向右")]
    private void ForceRight()
    {
        facingDirection = 1;
        Debug.Log($"强制设置为向右，facingDirection: {facingDirection}");
    }

    [ContextMenu("强制向左")]
    private void ForceLeft()
    {
        facingDirection = -1;
        Debug.Log($"强制设置为向左，facingDirection: {facingDirection}");
    }

    [ContextMenu("模拟向右攻击")]
    private void SimulateRightAttack()
    {
        ForceRight();
        PerformAttack();
    }

    [ContextMenu("模拟向左攻击")]
    private void SimulateLeftAttack()
    {
        ForceLeft();
        PerformAttack();
    }

    public void AttackTarget(GameObject target)
    {
        if (target == null || !CanAttack) return;

        OnAttackPerformed?.Invoke(attackType);
        ProcessAttackHit(target);
        lastAttackTime = Time.time;
    }

    // 获取命中的敌人（使用长方形区域检测）
    private Collider2D[] GetHitEnemies()
    {
        Vector2 actualAttackPosition = GetActualAttackPosition();
        float actualAttackAngle = GetActualAttackAngle();

        // 使用2D Box检测长方形区域
        Collider2D[] hitColliders = Physics2D.OverlapBoxAll(
            actualAttackPosition,
            boxSize,
            actualAttackAngle,
            enemyLayer
        );

        // 过滤掉自身
        List<Collider2D> validEnemies = new List<Collider2D>();
        foreach (Collider2D collider in hitColliders)
        {
            if (collider.gameObject != gameObject)
            {
                validEnemies.Add(collider);
            }
        }

        return validEnemies.ToArray();
    }

    [ContextMenu("测试攻击区域")]
    private void TestAttackArea()
    {
        Vector2 actualAttackPosition = GetActualAttackPosition();
        float actualAttackAngle = GetActualAttackAngle();

        Collider2D[] hits = Physics2D.OverlapBoxAll(actualAttackPosition, boxSize, actualAttackAngle, Physics2D.AllLayers);
        Debug.Log($"攻击区域内所有碰撞体: {hits.Length}");

        foreach (Collider2D hit in hits)
        {
            Debug.Log($"检测到: {hit.gameObject.name} (层级: {LayerMask.LayerToName(hit.gameObject.layer)})");
        }
    }

    [ContextMenu("强制设置测试层级")]
    private void ForceTestLayers()
    {
        enemyLayer = LayerMask.GetMask("Default", "Enemy");
        Debug.Log("已设置为检测Default和Enemy层级");
    }

    private void ProcessAttackHit(GameObject target)
    {
        if (target == null) return;

        int baseDamage = attackerAttribute != null ? attackerAttribute.Attack : 0;
        if (baseDamage <= 0) return;

        AttackInfo attackInfo = new AttackInfo
        {
            target = target,
            baseDamage = baseDamage,
            type = attackType,
            hitPoint = target.transform.position
        };

        int finalDamage = CalculateDamage(attackInfo);
        OnAttackHit?.Invoke(target, finalDamage, attackType);
        ApplyDamage(target, finalDamage, attackInfo);

        if (attackType != AttackType.Physical)
        {
            ApplyElementEffect(target, attackInfo);
        }
    }

    private int CalculateDamage(AttackInfo attackInfo)
    {
        int baseDamage = attackInfo.baseDamage;

        switch (attackType)
        {
            case AttackType.Physical:
                bool isCrit = Random.value < physicalCritRate;
                attackInfo.isCrit = isCrit;
                int physicalDamage = Mathf.RoundToInt(baseDamage * (isCrit ? physicalCritDamage : 1f));
                if (showDebugInfo)
                {
                    Debug.Log($"物理攻击: 基础{baseDamage}, 暴击{isCrit}, 最终{physicalDamage}");
                }
                return physicalDamage;

            default:
                Attribute targetAttribute = attackInfo.target.GetComponent<Attribute>();
                if (targetAttribute != null)
                {
                    float defenseMultiplier = Mathf.Clamp(1f - (targetAttribute.Defense * 0.01f), 0.1f, 1f);
                    int elementalDamage = Mathf.RoundToInt(baseDamage * defenseMultiplier);
                    if (showDebugInfo)
                    {
                        Debug.Log($"元素攻击: 基础{baseDamage}, 防御减伤{defenseMultiplier:P0}, 最终{elementalDamage}");
                    }
                    return elementalDamage;
                }
                return baseDamage;
        }
    }

    private void ApplyDamage(GameObject target, int damage, AttackInfo attackInfo)
    {
        Attribute targetAttribute = target.GetComponent<Attribute>();
        if (targetAttribute != null)
        {
            if (attackType == AttackType.Physical)
            {
                targetAttribute.TakeTrueDamage(damage, gameObject);
                if (showDebugInfo)
                {
                    Debug.Log($"物理攻击无视防御，造成 {damage} 点真实伤害");
                }
            }
            else
            {
                targetAttribute.TakeDamage(damage, gameObject);
                if (showDebugInfo)
                {
                    Debug.Log($"元素攻击经过防御减伤，造成 {damage} 点伤害");
                }
            }
        }
    }

    private void ApplyElementEffect(GameObject target, AttackInfo attackInfo)
    {
        if (Random.value > elementalEffectChance) return;

        switch (attackType)
        {
            case AttackType.Fire:
                ApplyFireEffect(target, attackInfo);
                break;
            case AttackType.Wind:
                ApplyWindEffect(target, attackInfo);
                break;
            case AttackType.Ice:
                ApplyIceEffect(target, attackInfo);
                break;
            case AttackType.Thunder:
                ApplyThunderEffect(target, attackInfo);
                break;
        }

        OnElementEffectApplied?.Invoke(attackType, target);
    }

    private void ApplyFireEffect(GameObject target, AttackInfo attackInfo)
    {
        BurnEffect burnEffect = target.GetComponent<BurnEffect>();
        if (burnEffect == null) burnEffect = target.AddComponent<BurnEffect>();

        int burnDamage = Mathf.RoundToInt(attackInfo.baseDamage * 0.3f);
        burnEffect.StartBurning(burnDamage, fireDamageDuration, gameObject);
        if (showDebugInfo)
        {
            Debug.Log($"火元素: {target.name} 开始燃烧，持续{fireDamageDuration}秒");
        }
    }

    // ---------- 已修改：风元素效果（优先调用 EnemyAI.ApplyWindKnockback） ----------
    private void ApplyWindEffect(GameObject target, AttackInfo attackInfo)
    {
        if (target == null) return;

        // 首先尝试通过 EnemyAI 的击退方法（推荐）
        EnemyAI enemyAI = target.GetComponent<EnemyAI>();
        if (enemyAI != null)
        {
            // 计算击退方向：如果攻击者在目标左侧，则击退朝右（fromRight = true）
            bool fromRight = transform.position.x < target.transform.position.x;
            enemyAI.ApplyWindKnockback(windKnockbackForce, fromRight);

            if (showDebugInfo)
                Debug.Log($"风元素 -> 通过 EnemyAI 对 {target.name} 触发击退 (force={windKnockbackForce}, fromRight={fromRight})");

            return;
        }

        // 如果目标没有 EnemyAI，就尽量只水平修改速度（不改变 Y），以防飞起
        Rigidbody2D targetRb = target.GetComponent<Rigidbody2D>();
        if (targetRb != null)
        {
            // 使用面向方向决定水平速度（facingDirection 为攻击者朝向）
            float horizontal = facingDirection * windKnockbackForce;
            Vector2 newVel = new Vector2(horizontal, targetRb.velocity.y); // 保留原 Y 速度
            targetRb.velocity = newVel;

            if (showDebugInfo)
                Debug.Log($"风元素 -> 对 {target.name} 直接设置水平速度 (vx={horizontal})");
        }
    }
    // ------------------------------------------------------------------------

    private void ApplyIceEffect(GameObject target, AttackInfo attackInfo)
    {
        FreezeEffect freezeEffect = target.GetComponent<FreezeEffect>();
        if (freezeEffect == null) freezeEffect = target.AddComponent<FreezeEffect>();

        freezeEffect.StartFreeze(iceFreezeDuration);
        if (showDebugInfo)
        {
            Debug.Log($"冰元素: {target.name} 被冻结{iceFreezeDuration}秒");
        }
    }

    private void ApplyThunderEffect(GameObject target, AttackInfo attackInfo)
    {
        Collider2D[] nearbyEnemies = Physics2D.OverlapCircleAll(target.transform.position, thunderChainRange, enemyLayer);
        int chainDamage = Mathf.RoundToInt(attackInfo.baseDamage * 0.5f);

        foreach (Collider2D enemy in nearbyEnemies)
        {
            if (enemy.gameObject != target && enemy.gameObject != gameObject)
            {
                Attribute enemyAttribute = enemy.GetComponent<Attribute>();
                if (enemyAttribute != null)
                {
                    enemyAttribute.TakeDamage(chainDamage, gameObject);
                    if (showDebugInfo)
                    {
                        Debug.Log($"雷元素连锁: {enemy.name} 受到{chainDamage}伤害");
                    }
                }
            }
        }

        if (showDebugInfo)
        {
            Debug.Log($"雷元素: 连锁攻击{nearbyEnemies.Length - 1}个目标");
        }
    }

    // 外部设置朝向的方法
    public void SetFacingDirection(int direction)
    {
        facingDirection = Mathf.Clamp(direction, -1, 1);
        // 注意：不再修改 transform.localScale，视觉翻转由 SpriteRenderer.flipX 控制
    }

    public void SetAttackType(AttackType newType) => attackType = newType;
    public void SetBoxSize(Vector2 newSize)
    {
        boxSize = new Vector2(Mathf.Max(0.1f, newSize.x), Mathf.Max(0.1f, newSize.y));
        // 更新调试网格
        if (debugMesh != null)
        {
            debugMesh = CreateBoxMesh();
        }
    }
    public void SetAttackCooldown(float newCooldown) => attackCooldown = Mathf.Max(0.1f, newCooldown);
    public void SetAttackKey(KeyCode newKey) => attackKey = newKey;
    public void SetAttackOffset(Vector2 newOffset) => attackOffset = newOffset;
    public void SetAttackAngle(float newAngle) => attackAngle = newAngle;

    public void ForceAttack()
    {
        lastAttackTime = Time.time - attackCooldown;
        PerformAttack();
    }

    // 在场景视图中绘制攻击范围
    private void OnDrawGizmosSelected()
    {
        // 绘制攻击范围
        Gizmos.color = attackType == AttackType.Physical ? Color.red : GetElementColor(attackType);

        Vector2 actualAttackPosition = GetActualAttackPosition();
        float actualAttackAngle = GetActualAttackAngle();

        // 绘制长方形攻击区域
        DrawGizmosBox(actualAttackPosition, boxSize, actualAttackAngle);

        // 绘制攻击点位置指示器
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(actualAttackPosition, 0.1f);

        if (attackType == AttackType.Thunder)
        {
            Gizmos.color = Color.yellow;
            DrawGizmosCircle(transform.position, thunderChainRange);
        }
    }

    // 绘制长方形攻击区域
    private void DrawGizmosBox(Vector3 center, Vector2 size, float angle)
    {
        Vector3[] corners = new Vector3[4];
        float halfWidth = size.x * 0.5f;
        float halfHeight = size.y * 0.5f;

        // 计算旋转后的四个角点
        Quaternion rotation = Quaternion.Euler(0, 0, angle);

        corners[0] = center + rotation * new Vector3(-halfWidth, -halfHeight, 0);
        corners[1] = center + rotation * new Vector3(halfWidth, -halfHeight, 0);
        corners[2] = center + rotation * new Vector3(halfWidth, halfHeight, 0);
        corners[3] = center + rotation * new Vector3(-halfWidth, halfHeight, 0);

        // 绘制四条边
        Gizmos.DrawLine(corners[0], corners[1]);
        Gizmos.DrawLine(corners[1], corners[2]);
        Gizmos.DrawLine(corners[2], corners[3]);
        Gizmos.DrawLine(corners[3], corners[0]);
    }

    // 绘制2D圆形的辅助方法
    private void DrawGizmosCircle(Vector3 center, float radius)
    {
        int segments = 32;
        float angle = 0f;

        Vector3 lastPoint = center + new Vector3(Mathf.Cos(0) * radius, Mathf.Sin(0) * radius, 0);

        for (int i = 1; i <= segments; i++)
        {
            angle = i * (2 * Mathf.PI / segments);
            Vector3 nextPoint = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
            Gizmos.DrawLine(lastPoint, nextPoint);
            lastPoint = nextPoint;
        }
    }

    private Color GetElementColor(AttackType type)
    {
        switch (type)
        {
            case AttackType.Fire: return Color.red;
            case AttackType.Wind: return Color.green;
            case AttackType.Ice: return Color.blue;
            case AttackType.Thunder: return Color.yellow;
            default: return Color.white;
        }
    }

    private void OnDestroy()
    {
        // 清理资源
        if (debugMaterial != null)
            Destroy(debugMaterial);
        if (debugMesh != null)
            Destroy(debugMesh);
    }
}


// 修改效果类以使用2D物理
public class BurnEffect : MonoBehaviour
{
    private float burnTimer;
    private int burnDamage;
    private float interval = 1f;
    private GameObject damageSource;

    public void StartBurning(int damage, float duration, GameObject source)
    {
        burnDamage = damage;
        burnTimer = duration;
        damageSource = source;
        StartCoroutine(BurnCoroutine());
    }

    private IEnumerator BurnCoroutine()
    {
        while (burnTimer > 0)
        {
            yield return new WaitForSeconds(interval);

            Attribute attribute = GetComponent<Attribute>();
            if (attribute != null && attribute.IsAlive)
            {
                attribute.TakeDamage(burnDamage, damageSource);
                Debug.Log($"燃烧伤害: {burnDamage}");
            }

            burnTimer -= interval;
        }

        Destroy(this);
    }
}

public class FreezeEffect : MonoBehaviour
{
    private float freezeTimer;
    private Vector2 originalVelocity;
    private Rigidbody2D rb;

    public void StartFreeze(float duration)
    {
        freezeTimer = duration;
        rb = GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            originalVelocity = rb.velocity;
            rb.velocity = Vector2.zero;
            rb.isKinematic = true;
        }

        StartCoroutine(FreezeCoroutine());
    }

    private IEnumerator FreezeCoroutine()
    {
        yield return new WaitForSeconds(freezeTimer);

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.velocity = originalVelocity;
        }

        Destroy(this);
    }
}
