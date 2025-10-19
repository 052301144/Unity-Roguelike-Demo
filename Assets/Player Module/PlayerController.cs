using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{

    [Header("Components")]
    public Rigidbody2D rb;
    public Collider2D bodyCollider;

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

    // 内部状态
    private float inputX;
    private bool wantJump;
    private bool isGrounded;
    private bool doubleJumpUsed;
    private int facing = 1; // 1 右, -1 左，用于表示角色的朝向
    
    // 跳跃状态管理
    private float jumpBufferTimer = 0f; // 跳跃缓冲计时器
    private float coyoteTimer = 0f; // 土狼时间计时器
    private float lastJumpTime = 0f; // 上次跳跃时间
    private bool wasGroundedLastFrame = false; // 上一帧是否在地面
    private float jumpStartY = 0f; // 跳跃开始时的Y位置
    private bool isJumping = false; // 是否正在跳跃

    void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
    }

    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (bodyCollider == null) bodyCollider = GetComponent<Collider2D>();
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
        float colliderWidth = bodyCollider.bounds.size.x;
        
        int wallHits = 0; // 计算墙壁命中次数
        string directionName = direction > 0 ? "右" : "左";
        
        Debug.Log($"[墙体检测] 开始检测{directionName}侧墙壁 - 角色位置: {transform.position}, 碰撞器边界: min={bodyCollider.bounds.min}, max={bodyCollider.bounds.max}");
        
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
                Debug.Log($"[墙体检测] 射线{i+1}命中墙壁 - 起点: {origin}, 距离: {hit.distance:F3}, 碰撞对象: {hit.collider.name}, 位置: {hit.point}");
                Debug.DrawRay(origin, new Vector2(direction, 0f) * wallCheckDistance, Color.red, 0.02f);
            }
            else
            {
                Debug.Log($"[墙体检测] 射线{i+1}未命中 - 起点: {origin}, 检测距离: {wallCheckDistance}");
                Debug.DrawRay(origin, new Vector2(direction, 0f) * wallCheckDistance, Color.green, 0.02f);
            }
        }
        
        // 如果大部分射线都命中墙壁，认为是真正的墙壁
        // 如果只有少数射线命中，可能是角落，允许通过
        bool hasWall = wallHits > wallCheckRays * 0.6f;
        Debug.Log($"[墙体检测] {directionName}侧检测结果 - 命中数: {wallHits}/{wallCheckRays}, 阈值: {wallCheckRays * 0.6f}, 有墙壁: {hasWall}");
        
        return hasWall;
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
        
        // 记录输入状态
        if (moveDirectionInt != 0)
        {
            string directionName = moveDirectionInt > 0 ? "右" : "左";
            Debug.Log($"[移动处理] 检测到{directionName}移动输入 - A:{pressingA}, D:{pressingD}, 当前朝向:{facing}");
        }
        
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
            Debug.Log($"[移动处理] 允许移动 - 目标速度: {targetVelX}, 移动速度: {moveSpeed}");
        }
        else if (moveDirection != 0f && wallAhead)
        {
            Debug.Log($"[移动处理] 被墙壁阻挡 - 无法移动");
        }
        else if (moveDirection == 0f)
        {
            Debug.Log($"[移动处理] 无移动输入 - 停止移动");
        }
        
        // 直接设置速度（固定速度移动）
        Vector2 velocity = rb.velocity;
        velocity.x = targetVelX;
        rb.velocity = velocity;
        
        // 更新朝向
        if (moveDirection > 0) facing = 1;
        else if (moveDirection < 0) facing = -1;
        
        // 记录最终状态
        if (moveDirectionInt != 0)
        {
            Debug.Log($"[移动处理] 最终状态 - 目标速度: {targetVelX}, 实际速度: {rb.velocity.x}, 朝向: {facing}, 有墙壁: {wallAhead}");
        }
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

    // 技能占位，目前只记录按键，后续扩展
    void OnDashSkill()
    {
        // 位移技能占位，直接向 facing 方向位移，距离短，瞬间移动
        float dashDistance = 2f;
        Vector3 target = transform.position + Vector3.right * facing * dashDistance;

        // 简单的位移实现，不考虑墙壁（实际项目应该考虑碰撞/冷却/无敌）
        transform.position = target;
        Debug.Log("Dash used (placeholder)");
    }

    void OnSkill1()
    {
        Debug.Log("Skill U used (placeholder)");
    }
    void OnSkill2()
    {
        Debug.Log("Skill I used (placeholder)");
    }
    void OnSkill3()
    {
        Debug.Log("Skill O used (placeholder)");
    }

    void Start()
    {
        // 确保组件存在
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (bodyCollider == null) bodyCollider = GetComponent<Collider2D>();

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
    }

    // Update is called once per frame
    void Update()
    {
        // 检测跳跃输入
        if (Input.GetKeyDown(KeyCode.K))
        {
            wantJump = true;
        }

        // 技能按键检测（只占位）
        if (Input.GetKeyDown(dashKey))
        {
            OnDashSkill();
        }
        if (Input.GetKeyDown(skill1Key))
        {
            OnSkill1();
        }
        if (Input.GetKeyDown(skill2Key))
        {
            OnSkill2();
        }
        if (Input.GetKeyDown(skill3Key))
        {
            OnSkill3();
        }
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
}
