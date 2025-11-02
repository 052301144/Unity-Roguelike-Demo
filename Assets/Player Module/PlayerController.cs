using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour, SM_ICharacterProvider, SM_IDamageable
{

    [Header("Components")]
    public Rigidbody2D rb;
    public Collider2D bodyCollider;
    public PhysicsMaterial2D zeroFrictionMaterial;  // 零摩擦材质，解决移动迟滞问题
    public Attribute attributeComponent;  // 属性组件（统一管理生命值、攻防等）
    
    [Header("Skill System")]
    public SM_SkillSystem skillSystem;  // 技能系统组件
    public Transform aimOrigin;         // 瞄准起点（通常是角色中心或武器位置）

    [Header("Collision Detection Settings")]
    [SerializeField] private bool useContinuousCollisionDetection = true;
    [SerializeField] private int groundCheckRays = 5; // 地面检测射线数量
    [SerializeField] private int wallCheckRays = 3;   // 墙壁检测射线数量
    [SerializeField] private float collisionPredictionTime = 0.1f; // 碰撞预测时间
    [SerializeField] private float maxSafeSpeed = 8f; // 最大安全速度，超过此速度可能穿模

    // 移动功能的参数
    [Header("Movement")]
    public float moveSpeed = 6f;          // 固定移动速度

    
    

    // 跳跃功能的参数
    [Header("Jump")]
    public float jumpForce = 20f;         // 跳跃力度
    public bool allowDoubleJump = false;  // 允许双跳
    public float highJumpMultiplier = 1.5f; // 高跳倍数
    
    [Header("Jump Detection Settings")]
    [SerializeField] private float jumpBufferTime = 0.2f; // 跳跃缓冲时间
    [SerializeField] private float coyoteTime = 0.1f; // 土狼时间（离开地面后仍可跳跃的时间）
    [SerializeField] private float jumpCooldown = 0.1f; // 跳跃冷却时间
    [SerializeField] private float minJumpHeight = 0.5f; // 最小跳跃高度
    [SerializeField] private bool useJumpBuffer = true; // 使用跳跃缓冲
    [SerializeField] private bool useCoyoteTime = true; // 使用土狼时间

    // 下落功能的参数（暂时未用，预留扩展）
    [Header("Fall")]
    public float fallSpeed = -10f;        // 固定下落速度，负值表示向下，暂时没有gravity
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.1f;
    public Vector2 groundCheckBoxSize = new Vector2(0.5f, 0.05f);

    // 检测角色前方是否有墙壁
    [Header("Wall Detection")]
    public float wallCheckDistance = 0.1f; // 前方墙壁检测射线长度
    public Vector2 wallCheckOffset = new Vector2(0.5f, 0f); // 偏移在角色中心点的偏移，水平方向

    // 技能功能预留占位
    [Header("Skills (placeholders)")]
    public KeyCode dashKey = KeyCode.L;   // 位移技能占位
    public KeyCode skill1Key = KeyCode.U;
    public KeyCode skill2Key = KeyCode.I;
    public KeyCode skill3Key = KeyCode.O;
    
    [Header("Attack Settings")]
    public bool allowAttackInAir = true;  // 是否允许在空中攻击

    // 内部状态
    private float inputX;
    private bool wantJump;
    private bool isGrounded;
    private bool doubleJumpUsed;
    private int facing = 1; // 1 右, -1 左，用于表示角色的朝向
    
    // 角色回复设置
    [Header("Regeneration")]
    public float healthRegenPerSec = 1f;  // 生命值回复速度（保留用于后续在Attribute中实现）
    
    // 跳跃状态管理
    private float jumpBufferTimer = 0f; // 跳跃缓冲计时器
    private float coyoteTimer = 0f; // 土狼时间计时器
    private float lastJumpTime = 0f; // 上次跳跃时间
    private bool wasGroundedLastFrame = false; // 上一帧是否在地面
    private float jumpStartY = 0f; // 跳跃开始时的Y位置
    private bool isJumping = false; // 是否正在跳跃

    // 动画相关
    [Header("Animation")]
    public Animator animator; // Animator挂载在子节点上，用于控制角色动画
    public Attack attackComponent; // 应该在Inspector拖拽赋值或者Awake中自动查找
    // 可视部分的根节点（用于动画，不直接翻转变换）
    public Transform visualRoot;
    // SpriteRenderer引用，用于翻转朝向
    private SpriteRenderer spriteRenderer;

    void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
    }

    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (bodyCollider == null) bodyCollider = GetComponent<Collider2D>();
        
        // 查找 Attribute 组件
        if (attributeComponent == null)
            attributeComponent = GetComponent<Attribute>();
        
        // 查找 Attack 组件
        if (attackComponent == null)
            attackComponent = GetComponent<Attack>();
        
        // Animator应该挂载在视觉子节点上
        if (animator == null && visualRoot != null) 
            animator = visualRoot.GetComponent<Animator>();
        // 如果还是没找到，尝试从子节点获取
        if (animator == null && transform.childCount > 0)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                animator = transform.GetChild(i).GetComponent<Animator>();
                if (animator != null) break;
            }
        }
        
        // 获取或查找SpriteRenderer用于翻转
        if (spriteRenderer == null && visualRoot != null)
            spriteRenderer = visualRoot.GetComponent<SpriteRenderer>();
        // 如果在视觉根节点没找到，尝试从子节点获取
        if (spriteRenderer == null && transform.childCount > 0)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                spriteRenderer = transform.GetChild(i).GetComponent<SpriteRenderer>();
                if (spriteRenderer != null) break;
            }
        }
        // 如果还是没找到，尝试从自己获取
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void CheckGround()
    {
        if (bodyCollider == null) { isGrounded = false; return; }

        // 使用多射线检测提高精度
        float skin = 0.02f;
        float checkDistance = groundCheckDistance + skin;
        float colliderWidth = bodyCollider.bounds.size.x;
        float colliderBottom = bodyCollider.bounds.min.y;
        
        bool hitGround = false;
        
        // 在角色底部创建多个检测点
        for (int i = 0; i < groundCheckRays; i++)
        {
            float xOffset = (i / (float)(groundCheckRays - 1) - 0.5f) * colliderWidth * 0.8f;
            Vector2 rayOrigin = new Vector2(bodyCollider.bounds.center.x + xOffset, colliderBottom);
            
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, checkDistance, groundLayer);
            
            if (hit.collider != null)
            {
                hitGround = true;
                Debug.DrawRay(rayOrigin, Vector2.down * checkDistance, Color.green, 0.02f);
            }
            else
            {
                Debug.DrawRay(rayOrigin, Vector2.down * checkDistance, Color.red, 0.02f);
            }
        }
        
        // 检查垂直速度
        bool verticalOk = rb == null ? true : rb.velocity.y <= 0.1f;
        isGrounded = hitGround && verticalOk;
        
        // 更新跳跃状态
        UpdateJumpState();
        
        if (isGrounded) 
        {
            doubleJumpUsed = false;
            isJumping = false;
        }
    }
    
    // 更新跳跃状态
    void UpdateJumpState()
    {
        // 更新土狼时间
        if (useCoyoteTime)
        {
            if (isGrounded)
            {
                coyoteTimer = coyoteTime;
            }
            else
            {
                coyoteTimer -= Time.fixedDeltaTime;
            }
        }
        
        // 更新跳跃缓冲
        if (useJumpBuffer)
        {
            if (jumpBufferTimer > 0)
            {
                jumpBufferTimer -= Time.fixedDeltaTime;
            }
        }
        
        // 更新跳跃移动状态
        UpdateJumpMovementState();
        
        // 记录上一帧的地面状态
        wasGroundedLastFrame = isGrounded;
    }
    
    // 更新跳跃移动状态
    void UpdateJumpMovementState()
    {
        // 检测开始跳跃
        if (isJumping && !wasGroundedLastFrame && isGrounded)
        {
            OnJumpStart();
        }
        
        // 检测结束跳跃（落地）
        if (isJumping && !isGrounded && wasGroundedLastFrame)
        {
            OnJumpEnd();
        }
        
        // 确保在地面时清除跳跃状态
        if (isGrounded && isJumping)
        {
            isJumping = false;
        }
    }
    
    
    // 跳跃结束时的处理
    void OnJumpEnd()
    {
        // 重置跳跃相关状态
        jumpStartY = 0f;
    }
    
    // 检查是否可以跳跃
    bool CanJump()
    {
        // 检查跳跃冷却
        if (Time.time - lastJumpTime < jumpCooldown)
            return false;
            
        // 检查是否在地面或土狼时间内
        bool canGroundJump = isGrounded || (useCoyoteTime && coyoteTimer > 0);
        
        // 检查双跳
        bool canDoubleJump = allowDoubleJump && !doubleJumpUsed && !isGrounded;
        
        return canGroundJump || canDoubleJump;
    }
    
    // 处理跳跃输入缓冲
    void HandleJumpInput()
    {
        if (wantJump)
        {
            if (useJumpBuffer)
            {
                jumpBufferTimer = jumpBufferTime;
            }
            wantJump = false;
        }
    }
    
    // 检查跳跃缓冲
    bool HasJumpBuffer()
    {
        return useJumpBuffer && jumpBufferTimer > 0;
    }

    // 改进的墙壁检测 - 考虑角落情况
    bool IsWallAhead()
    {
        // 使用多射线检测防止边缘穿模
        float colliderHeight = bodyCollider.bounds.size.y;
        float colliderCenterY = bodyCollider.bounds.center.y;
        
        int wallHits = 0; // 计算墙壁命中次数
        
        for (int i = 0; i < wallCheckRays; i++)
        {
            float yOffset = (i / (float)(wallCheckRays - 1) - 0.5f) * colliderHeight * 0.8f;
            Vector2 origin = new Vector2(
                bodyCollider.bounds.center.x + wallCheckOffset.x * facing,
                colliderCenterY + yOffset
            );
            
        RaycastHit2D hit = Physics2D.Raycast(origin, new Vector2(facing, 0f), wallCheckDistance, groundLayer);
            
            if (hit.collider != null)
            {
                wallHits++;
                Debug.DrawRay(origin, new Vector2(facing, 0f) * wallCheckDistance, Color.red, 0.02f);
            }
            else
            {
                Debug.DrawRay(origin, new Vector2(facing, 0f) * wallCheckDistance, Color.green, 0.02f);
            }
        }
        
        // 如果大部分射线都命中墙壁，认为是真正的墙壁
        // 如果只有少数射线命中，可能是角落，允许通过
        return wallHits > wallCheckRays * 0.6f;
    }
    
    // 根据移动方向检测墙壁
    bool IsWallAhead(int direction)
    {
        // 使用多射线检测防止边缘穿模
        float colliderHeight = bodyCollider.bounds.size.y;
        float colliderCenterY = bodyCollider.bounds.center.y;
        
        int wallHits = 0; // 计算墙壁命中次数
        
        for (int i = 0; i < wallCheckRays; i++)
        {
            float yOffset = (i / (float)(wallCheckRays - 1) - 0.5f) * colliderHeight * 0.8f;
            
            // 修正射线起点：从角色碰撞器边缘开始，而不是从中心偏移
            float colliderEdge = direction > 0 ? 
                bodyCollider.bounds.max.x : bodyCollider.bounds.min.x;
            
            Vector2 origin = new Vector2(
                colliderEdge,
                colliderCenterY + yOffset
            );
            
            RaycastHit2D hit = Physics2D.Raycast(origin, new Vector2(direction, 0f), wallCheckDistance, groundLayer);
            
            if (hit.collider != null)
            {
                wallHits++;
                Debug.DrawRay(origin, new Vector2(direction, 0f) * wallCheckDistance, Color.red, 0.02f);
            }
            else
            {
                Debug.DrawRay(origin, new Vector2(direction, 0f) * wallCheckDistance, Color.green, 0.02f);
            }
        }
        
        // 如果大部分射线都命中墙壁，认为是真正的墙壁
        // 如果只有少数射线命中，可能是角落，允许通过
        return wallHits > wallCheckRays * 0.6f;
    }
    
    // 预测性碰撞检测
    bool PredictWallCollision()
    {
        if (rb == null || bodyCollider == null) return false;
        
        // 计算下一帧的预测位置
        Vector2 predictedPosition = (Vector2)transform.position + rb.velocity * collisionPredictionTime;
        
        // 检查预测位置是否会碰撞
        float colliderHeight = bodyCollider.bounds.size.y;
        float colliderCenterY = bodyCollider.bounds.center.y;
        
        for (int i = 0; i < wallCheckRays; i++)
        {
            float yOffset = (i / (float)(wallCheckRays - 1) - 0.5f) * colliderHeight * 0.8f;
            Vector2 origin = new Vector2(
                predictedPosition.x + wallCheckOffset.x * facing,
                colliderCenterY + yOffset
            );
            
        RaycastHit2D hit = Physics2D.Raycast(origin, new Vector2(facing, 0f), wallCheckDistance, groundLayer);
            
            if (hit.collider != null)
            {
                return true;
            }
        }
        
        return false;
    }
    
    // 限制速度防止穿模
    Vector2 LimitVelocity(Vector2 velocity)
    {
        // 如果速度超过安全速度，进行限制
        if (Mathf.Abs(velocity.x) > maxSafeSpeed)
        {
            velocity.x = Mathf.Sign(velocity.x) * maxSafeSpeed;
        }
        
        return velocity;
    }


    void HandleMovement()
    {
        // 检测A/D按键输入
        bool pressingA = Input.GetKey(KeyCode.A);
        bool pressingD = Input.GetKey(KeyCode.D);
        
        // 计算移动方向
        float moveDirection = 0f;
        int moveDirectionInt = 0;
        if (pressingA && !pressingD)
        {
            moveDirection = -1f; // 向左
            moveDirectionInt = -1;
        }
        else if (pressingD && !pressingA)
        {
            moveDirection = 1f; // 向右
            moveDirectionInt = 1;
        }
        // 如果同时按下A和D，或者都没按，则停止移动
        
        // 检查墙壁碰撞 - 使用移动方向而不是朝向
        bool wallAhead = false;
        if (moveDirectionInt != 0)
        {
            wallAhead = IsWallAhead(moveDirectionInt);
        }
        
        // 计算目标速度
        float targetVelX = 0f;
        if (moveDirection != 0f && !wallAhead)
        {
            targetVelX = moveDirection * moveSpeed;
        }
        
        // ========== 优化的速度设置方式 ==========
        // 使用零摩擦材质后，直接设置velocity即可获得即时响应
        // 这样可以实现完美的方向切换，无迟滞
        Vector2 velocity = rb.velocity;
        velocity.x = targetVelX;
        rb.velocity = velocity;
        
        // 更新朝向（仅在有移动输入时更新）
        if (moveDirection > 0) facing = 1;
        else if (moveDirection < 0) facing = -1;
    }
    
    
    void HandleJump()
    {
        // 处理跳跃输入缓冲
        HandleJumpInput();

        // 检查是否有跳跃缓冲或直接跳跃输入
        bool shouldJump = HasJumpBuffer() || wantJump;
        
        if (shouldJump && CanJump())
        {
            DoJump();
            
            // 清除跳跃缓冲
            jumpBufferTimer = 0f;
            
            // 如果是双跳，标记已使用
            if (!isGrounded && allowDoubleJump && !doubleJumpUsed)
        {
            doubleJumpUsed = true;
            }
            
            // 更新跳跃状态
            lastJumpTime = Time.time;
            isJumping = true;
            jumpStartY = transform.position.y;
        }
        
        // 检查跳跃高度限制
        CheckJumpHeight();
    }
    

    void DoJump()
    {
        // 应用垂直速度，预留高跳倍数，实际实现时根据需求调整
        float appliedJump = jumpForce * highJumpMultiplier;
        
        // 如果是双跳，稍微减少力度
        if (!isGrounded && allowDoubleJump)
        {
            appliedJump *= 0.8f;
        }
        
        Debug.Log($"DoJump appliedJump={appliedJump}, isGrounded={isGrounded}, coyoteTimer={coyoteTimer:F2}");
        
        Vector2 v = rb.velocity;
        v.y = appliedJump;
        rb.velocity = v;
        
        // 清除土狼时间
        if (useCoyoteTime)
        {
            coyoteTimer = 0f;
        }
    }

    /*
    void HandleFallFix()
    {
        // ��������������Ҫ�̶������ٶȣ�ǿ�������ٶȣ������Ҫ��
        if (!isGrounded)
        {
            if (rb.velocity.y < fallSpeed) // fallSpeed �Ǹ�ֵ�����ͱ�ʾ��������
            {
                Vector2 v = rb.velocity;
                v.y = fallSpeed;
                rb.velocity = v;
            }
        }
    }
    */

    // 注意：技能现在由SM_SkillSystem处理
    // 如果需要自定义技能逻辑，可以在这里添加

    // ========== SM_ICharacterProvider 接口实现 ==========
    public Transform AimOrigin => aimOrigin != null ? aimOrigin : transform;
    public Vector2 AimDirection => new Vector2(facing, 0f); // 基于朝向的瞄准方向
    public float CurrentMP => skillSystem != null ? skillSystem.CurrentMP : 0f;
    public float MaxMP => skillSystem != null ? skillSystem.MaxMP : 0f;
    public bool ConsumeMP(float amount) => skillSystem != null ? skillSystem.ConsumeMP(amount) : false;
    
    // ========== SM_IDamageable 接口实现 ==========
    /// <summary>
    /// 技能系统伤害接口 - 转发到Attribute组件
    /// </summary>
    public void ApplyDamage(SM_DamageInfo info)
    {
        // 如果有Attribute组件，使用它处理伤害
        if (attributeComponent != null)
        {
            attributeComponent.ApplyDamage(info);
        }
        else
        {
            Debug.LogWarning("[PlayerController] 未找到Attribute组件，无法处理伤害");
        }
    }
    
    public Transform GetTransform() => transform;
    
    // ========== 角色状态管理 ==========
    /// <summary>
    /// 角色死亡处理 - 由Attribute组件触发
    /// </summary>
    private void OnDeath()
    {
        Debug.Log("[角色] 角色死亡！");
        // 禁用玩家控制
        enabled = false;
    }
    
    /// <summary>
    /// 生命值回复更新 - 使用Attribute组件
    /// </summary>
    private void UpdateHealth()
    {
        // 如果有Attribute组件且已配置回复速度
        if (attributeComponent != null && healthRegenPerSec > 0f)
        {
            float regenAmount = healthRegenPerSec * Time.deltaTime;
            // 累计回复量，当达到1点以上时才调用Heal
            // 这里简化处理：每帧检查回复是否大于0.1（避免过于频繁的日志输出）
            if (regenAmount >= 0.1f)
            {
                int healAmount = Mathf.RoundToInt(regenAmount);
                attributeComponent.Heal(healAmount);
            }
        }
    }
    
    // ========== 技能系统集成 ==========
    private void UpdateSkillAim()
    {
        // 更新技能系统的瞄准方向
        if (skillSystem != null)
        {
            skillSystem.SetAim(new Vector2(facing, 0f));
        }
    }


    void Start()
    {
        // 确保组件存在
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (bodyCollider == null) bodyCollider = GetComponent<Collider2D>();
        if (skillSystem == null) skillSystem = GetComponent<SM_SkillSystem>();
        // Animator在Awake中已初始化，这里不再重复获取
        
        // 注册Attribute组件的事件
        if (attributeComponent != null)
        {
            attributeComponent.OnDeath += OnDeath;
        }
        else
        {
            Debug.LogWarning("[PlayerController] 未找到Attribute组件，请添加Attribute组件到玩家对象");
        }

        if (rb == null)
        {
            Debug.LogError("[PlayerController] Rigidbody2D 未找到，禁用脚本", this);
            enabled = false;
            return;
        }
        if (bodyCollider == null)
        {
            Debug.LogError("[PlayerController] Collider2D 未找到，禁用脚本", this);
            enabled = false;
            return;
        }

        // 配置连续碰撞检测防止高速穿模
        if (useContinuousCollisionDetection)
        {
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
        else
        {
            rb.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
        }
        
        // ========== 解决移动迟滞问题的关键设置 ==========
        
        // 1. 设置插值模式，使移动更平滑
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        
        // 2. 冻结Z轴旋转，防止碰撞时旋转导致的移动异常
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        
        // 3. 创建并应用零摩擦物理材质（这是解决迟滞的关键）
        if (zeroFrictionMaterial == null)
        {
            // 如果未手动指定，自动创建零摩擦材质
            zeroFrictionMaterial = new PhysicsMaterial2D("ZeroFriction");
            zeroFrictionMaterial.friction = 0f;      // 摩擦力设为0
            zeroFrictionMaterial.bounciness = 0f;    // 弹性设为0
        }
        
        // 应用物理材质到角色的碰撞体
        if (bodyCollider != null)
        {
            bodyCollider.sharedMaterial = zeroFrictionMaterial;
            Debug.Log("[PlayerController] 已应用零摩擦材质，解决移动迟滞问题");
        }
        
        // 4. 确保重力缩放合适（使用Unity的物理2D重力）
        if (rb.gravityScale == 0)
        {
            rb.gravityScale = 3f; // 推荐值，可根据手感调整
            Debug.Log("[PlayerController] 设置重力缩放为 3.0");
        }

        // 验证参数值，防止异常行为
        moveSpeed = Mathf.Max(0.01f, moveSpeed);

        // 验证碰撞检测参数
        groundCheckRays = Mathf.Max(1, groundCheckRays);
        wallCheckRays = Mathf.Max(1, wallCheckRays);
        collisionPredictionTime = Mathf.Max(0.01f, collisionPredictionTime);
        maxSafeSpeed = Mathf.Max(moveSpeed, maxSafeSpeed);
        
        // 验证跳跃参数
        jumpBufferTime = Mathf.Max(0f, jumpBufferTime);
        coyoteTime = Mathf.Max(0f, coyoteTime);
        jumpCooldown = Mathf.Max(0f, jumpCooldown);
        minJumpHeight = Mathf.Max(0f, minJumpHeight);
        
        Debug.Log($"[PlayerController] 初始化完成 - CCD: {useContinuousCollisionDetection}, 地面射线: {groundCheckRays}, 墙壁射线: {wallCheckRays}, 移动速度: {moveSpeed}");
        Debug.Log($"[PlayerController] 跳跃设置 - 缓冲时间: {jumpBufferTime}, 土狼时间: {coyoteTime}, 冷却: {jumpCooldown}, 最小高度: {minJumpHeight}");
        Debug.Log($"[PlayerController] Animator状态 - {(animator != null ? "已找到" : "未找到")}");
    }

    // Update is called once per frame
    void Update()
    {
        // 检测跳跃输入
        if (Input.GetKeyDown(KeyCode.K))
        {
            wantJump = true;
        }
        // 攻击输入检测（示例用J键，可根据技能系统调整）
        if (Input.GetKeyDown(KeyCode.J))
        {
            TriggerAttackAnim();
        }
        // 动画参数同步，每帧更新
        UpdateAnimationParameters();
        
        // 更新技能瞄准方向
        UpdateSkillAim();
        
        // 更新生命值
        UpdateHealth();
        
        // 注意：技能按键检测现在由SM_SkillSystem自动处理
        // 如果需要自定义技能逻辑，可以在这里添加
    }

    void FixedUpdate()
    {
        CheckGround();
        
        // 更新跳跃状态
        UpdateJumpMovementState();
        
        // 处理移动
        HandleMovement();
        
        // 处理跳跃
        HandleJump();
        
        // 更新状态
        UpdateStates();
        
        // 更新计时器
        UpdateTimers();
        
        // 重置跳跃输入
        wantJump = false;
        // 可以根据物理状态同步动画参数（如需实时，建议用UpdateAnimationParameters()）
    }

    // 绘制检测范围（调试用）
    void OnDrawGizmosSelected()
    {
        if (bodyCollider == null) return;

        // 绘制地面检测射线
        float skin = 0.02f;
        float checkDistance = groundCheckDistance + skin;
        float colliderWidth = bodyCollider.bounds.size.x;
        float colliderBottom = bodyCollider.bounds.min.y;
        
        Gizmos.color = Color.cyan;
        for (int i = 0; i < groundCheckRays; i++)
        {
            float xOffset = (i / (float)(groundCheckRays - 1) - 0.5f) * colliderWidth * 0.8f;
            Vector3 rayOrigin = new Vector3(bodyCollider.bounds.center.x + xOffset, colliderBottom, transform.position.z);
            Vector3 rayEnd = rayOrigin + Vector3.down * checkDistance;
            
            Gizmos.DrawLine(rayOrigin, rayEnd);
        }
        
        // 绘制墙壁检测射线
        float colliderHeight = bodyCollider.bounds.size.y;
        float colliderCenterY = bodyCollider.bounds.center.y;
        
        Gizmos.color = Color.magenta;
        for (int i = 0; i < wallCheckRays; i++)
        {
            float yOffset = (i / (float)(wallCheckRays - 1) - 0.5f) * colliderHeight * 0.8f;
            Vector3 rayOrigin = new Vector3(
                bodyCollider.bounds.center.x + wallCheckOffset.x * facing,
                colliderCenterY + yOffset,
                transform.position.z
            );
            Vector3 rayEnd = rayOrigin + Vector3.right * facing * wallCheckDistance;
            
            Gizmos.DrawLine(rayOrigin, rayEnd);
        }
        
        // 绘制预测位置
        if (rb != null)
        {
            Vector3 predictedPos = transform.position + (Vector3)(rb.velocity * collisionPredictionTime);
        Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(predictedPos, 0.1f);
        }
        
        // 绘制速度限制范围
        if (rb != null && Mathf.Abs(rb.velocity.x) > maxSafeSpeed)
        {
            Gizmos.color = Color.red;
            Vector3 warningPos = transform.position + Vector3.up * 2f;
            Gizmos.DrawWireSphere(warningPos, 0.2f);
        }
        
        // 绘制跳跃状态
        if (isJumping)
        {
            Gizmos.color = Color.cyan;
            Vector3 jumpPos = transform.position + Vector3.up * 1.5f;
            Gizmos.DrawWireSphere(jumpPos, 0.15f);
        }
        
        // 绘制土狼时间状态
        if (useCoyoteTime && coyoteTimer > 0)
        {
        Gizmos.color = Color.yellow;
            Vector3 coyotePos = transform.position + Vector3.up * 1f;
            Gizmos.DrawWireCube(coyotePos, Vector3.one * 0.1f);
        }
        
        // 绘制跳跃缓冲状态
        if (useJumpBuffer && jumpBufferTimer > 0)
        {
            Gizmos.color = Color.green;
            Vector3 bufferPos = transform.position + Vector3.up * 0.5f;
            Gizmos.DrawWireCube(bufferPos, Vector3.one * 0.08f);
        }
        
        // 绘制移动状态（简化版本）
        if (isGrounded)
        {
            Gizmos.color = Color.green;
            Vector3 groundPos = transform.position + Vector3.up * 2.5f;
            Gizmos.DrawWireCube(groundPos, Vector3.one * 0.1f);
        }
    }
    
    
    // 更新状态
    void UpdateStates()
    {
        // 朝向在HandleMovement中已经更新，这里不需要额外处理
    }
    
    // 更新计时器
    void UpdateTimers()
    {
        // 更新跳跃缓冲计时器
        if (jumpBufferTimer > 0)
        {
            jumpBufferTimer -= Time.fixedDeltaTime;
        }
        
        // 更新土狼时间计时器
        if (coyoteTimer > 0)
        {
            coyoteTimer -= Time.fixedDeltaTime;
        }
        
        // 无输入计时器已移除，简化移动逻辑不需要
    }
    
    // 跳跃开始事件
    void OnJumpStart()
    {
        isJumping = true;
        jumpStartY = transform.position.y;
    }
    
    // 落地事件
    void OnLanding()
    {
        // 重置双跳状态
        doubleJumpUsed = false;
    }
    
    // 起飞事件
    void OnTakeOff()
    {
        // 起飞时不需要特殊处理
    }
    
    // 检查跳跃高度
    void CheckJumpHeight()
    {
        if (isJumping)
        {
            float currentHeight = transform.position.y - jumpStartY;
            
            // 如果达到最小跳跃高度且正在下降，结束跳跃状态
            if (currentHeight >= minJumpHeight && rb.velocity.y <= 0)
            {
                OnJumpEnd();
            }
        }
    }

    /// <summary>
    /// 动画参数同步：速度、跳跃、落地、攻击等状态推送到Animator
    /// </summary>
    private void UpdateAnimationParameters()
    {
        if (animator == null) return;
        // Speed 横向绝对速度，用于区分Idle/Walk
        animator.SetFloat("Speed", Mathf.Abs(rb != null ? rb.velocity.x : 0f));
        // 跳跃中（可拆出上升/悬空/落地等动画）
        animator.SetBool("IsJumping", isJumping && !isGrounded);
        // 落地Idle（可配合IsJumping做blend tree）
        animator.SetBool("IsGrounded", isGrounded);
        // 朝向（通过动画参数控制翻转，不再手动翻转transform）
        animator.SetBool("FacingRight", facing >= 0);
        animator.SetFloat("Facing", facing);
        
        // 使用SpriteRenderer翻转来控制朝向（Animator的Mirror对2D Sprite无效）
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = facing < 0; // 向左时翻转
        }
        
        // 核心：攻击动画trigger由输入接口专门触发（避免状态机错乱）
    }
    /// <summary>
    /// 触发攻击动画，可由技能系统/按键调用。后续可扩展为带武器动画参数。
    /// </summary>
    public void TriggerAttackAnim()
    {
        // 检查是否允许在空中攻击
        if (!allowAttackInAir && !isGrounded)
        {
            // Debug.Log("[攻击] 空中攻击已禁用");
            return;
        }
        
        // 检查攻击冷却（通过 Attack 组件）
        if (attackComponent != null && !attackComponent.CanAttack)
        {
            // Debug.Log("[攻击] 攻击冷却中");
            return;
        }
        
        if (attackComponent != null)
        {
            attackComponent.SetFacingDirection(facing); // 同步Attack朝向
            // 触发攻击动画触发器
            attackComponent.PerformAttack(); // 执行实际攻击
        }
        if (animator != null)
            animator.SetTrigger("Attack"); // 触发动画
    }
    
    // 清理事件订阅
    private void OnDestroy()
    {
        if (attributeComponent != null)
        {
            attributeComponent.OnDeath -= OnDeath;
        }
    }
}
