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
    public float maxSpeed = 6f;           // 最大速度
    public float accel = 40f;             // 加速度，按下按键时速度增加
    public float decel = 80f;             // 减速度，松开按键时减速停止
    public float groundMoveSpeed = 6f;    // 固定移动速度，用于空中移动
    [Tooltip("占位符: 如果启用 will use accel/decel to reach maxSpeed")]
    public bool useAcceleration = true;

    [Header("Advanced Movement Control")]
    [SerializeField] private float fastDecel = 120f; // 快速减速度，用于快速停止
    [SerializeField] private float stopThreshold = 0.1f; // 停止阈值，低于此速度直接设为0
    [SerializeField] private bool useFastDeceleration = true; // 使用快速减速
    [SerializeField] private float decelMultiplier = 2f; // 减速度倍数，相对于加速度
    
    [Header("Jump Movement Control")]
    [SerializeField] private bool clearHorizontalSpeedOnJump = false; // 跳跃时清除水平速度（改为false，让跳跃继承地面速度）
    [SerializeField] private bool clearHorizontalSpeedOnLanding = true; // 落地时清除水平速度
    [SerializeField] private float landingSpeedClearThreshold = 0.5f; // 落地速度清除阈值
    [SerializeField] private bool inheritGroundSpeedOnJump = true; // 跳跃时继承地面速度
    
    [Header("Movement State Settings")]
    [SerializeField] private bool preserveAirSpeed = true; // 保持空中速度
    [SerializeField] private float airSpeedDecay = 0.95f; // 空中速度衰减系数
    [SerializeField] private float landingSpeedThreshold = 0.5f; // 落地速度阈值
    [SerializeField] private float landingTransitionTime = 0.2f; // 落地过渡时间

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
    
    // 移动状态管理
    private float preservedAirSpeed = 0f; // 保持的空中速度
    private float landingTransitionTimer = 0f; // 落地过渡计时器
    private bool isLandingTransition = false; // 是否在落地过渡中
    private bool wasInAirLastFrame = false; // 上一帧是否在空中
    
    // 移动控制状态
    private bool hasInput = false; // 是否有输入
    private float noInputTimer = 0f; // 无输入计时器
    private bool isStopping = false; // 是否正在停止
    
    // 输入状态跟踪
    private bool wasMovingLastFrame = false; // 上一帧是否在移动
    private float lastInputX = 0f; // 上一帧的输入值
    private bool inputChanged = false; // 输入是否发生变化
    
    // 精确按键状态跟踪
    private bool wasPressingA = false; // 上一帧是否按下A
    private bool wasPressingD = false; // 上一帧是否按下D
    private bool keyReleased = false; // 是否有按键被释放
    
    // 按键优先级系统状态
    private bool aWasPressedLast = false; // A是否最后被按下
    private bool dWasPressedLast = false; // D是否最后被按下
    
    // 跳跃移动控制状态
    private float jumpStartHorizontalSpeed = 0f; // 跳跃开始时的水平速度
    private bool wasJumpingLastFrame = false; // 上一帧是否在跳跃
    
    // 空中速度控制
    private float airSpeedPreservation = 0.98f; // 空中速度保持系数
    private float airAccelerationMultiplier = 0.7f; // 空中加速度倍数
    
    // 跳跃方向锁定
    private bool lockJumpDirection = true; // 是否锁定跳跃方向
    private float lockedJumpDirection = 0f; // 锁定的跳跃方向
    private float jumpDirectionLockTime = 0.3f; // 跳跃方向锁定时间
    private float jumpDirectionLockTimer = 0f; // 跳跃方向锁定计时器
    
    // 速度平滑控制
    private float lastTargetVelocity = 0f; // 上一帧的目标速度
    private float velocityChangeThreshold = 5f; // 速度变化阈值
    
    // 变向控制
    private bool enableInstantDirectionChange = true; // 是否启用立即变向
    private float directionChangeThreshold = 0.1f; // 变向检测阈值（降低阈值，更容易触发）
    private float directionChangeCooldown = 0.03f; // 变向冷却时间，防止颤动（进一步缩短冷却时间）
    private float lastDirectionChangeTime = 0f; // 上次变向时间
    
    // 角落卡住检测和处理
    private float stuckTimer = 0f; // 卡住计时器
    private float stuckThreshold = 0.5f; // 卡住阈值（秒）
    private Vector2 lastPosition = Vector2.zero; // 上一帧位置
    private float stuckCheckDistance = 0.1f; // 卡住检测距离

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
        
        // 更新移动状态
        UpdateMovementState();
        
        // 更新跳跃移动状态
        UpdateJumpMovementState();
        
        // 记录上一帧的地面状态
        wasGroundedLastFrame = isGrounded;
        wasInAirLastFrame = !isGrounded;
        wasJumpingLastFrame = isJumping;
    }
    
    // 更新跳跃移动状态 - 改进版本
    void UpdateJumpMovementState()
    {
        // 检测开始跳跃
        if (isJumping && !wasJumpingLastFrame)
        {
            OnJumpStart();
        }
        
        // 检测结束跳跃（落地或高度限制）
        if (isJumping && wasJumpingLastFrame && (isGrounded || !isJumping))
        {
            OnJumpEnd();
        }
        
        // 确保在地面时清除跳跃状态
        if (isGrounded && isJumping)
        {
            isJumping = false;
            Debug.Log("地面检测 - 清除跳跃状态");
        }
        
        // 更新跳跃方向锁定计时器
        if (jumpDirectionLockTimer > 0)
        {
            jumpDirectionLockTimer -= Time.fixedDeltaTime;
            if (jumpDirectionLockTimer <= 0)
            {
                Debug.Log("跳跃方向锁定结束");
            }
        }
    }
    
    
    // 跳跃结束时的处理
    void OnJumpEnd()
    {
        float currentHorizontalSpeed = rb.velocity.x;
        
        // 如果启用落地时清除水平速度
        if (clearHorizontalSpeedOnLanding)
        {
            // 检查是否需要清除速度
            // 1. 没有输入
            // 2. 输入发生变化（从有输入变为无输入）
            // 3. 有按键被释放
            // 4. 速度低于阈值
            bool shouldClearSpeed = !hasInput || inputChanged || keyReleased || Mathf.Abs(currentHorizontalSpeed) < landingSpeedClearThreshold;
            
            if (shouldClearSpeed)
            {
                Vector2 velocity = rb.velocity;
                velocity.x = 0f;
                rb.velocity = velocity;
                Debug.Log($"跳跃结束 - 清除水平速度: {currentHorizontalSpeed:F2} -> 0 (无输入: {!hasInput}, 输入变化: {inputChanged}, 按键释放: {keyReleased})");
            }
            else
            {
                Debug.Log($"跳跃结束 - 保持水平速度: {currentHorizontalSpeed:F2}");
            }
        }
        else
        {
            Debug.Log($"跳跃结束 - 保持水平速度: {currentHorizontalSpeed:F2}");
        }
        
        // 重置跳跃相关状态
        jumpStartHorizontalSpeed = 0f;
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

    // 检测是否卡在角落 - 改进版本
    bool IsStuckInCorner()
    {
        Vector2 currentPosition = transform.position;
        float distanceMoved = Vector2.Distance(currentPosition, lastPosition);
        
        // 检查是否满足卡住条件
        bool isMovingSlowly = distanceMoved < stuckCheckDistance;
        bool isOnGround = isGrounded;
        bool hasInputButNotMoving = hasInput && isMovingSlowly;
        
        if ((isMovingSlowly && isOnGround) || hasInputButNotMoving)
        {
            stuckTimer += Time.fixedDeltaTime;
            
            // 如果卡住时间超过阈值
            if (stuckTimer > stuckThreshold)
            {
                Debug.Log($"检测到卡在角落: 移动距离 {distanceMoved:F3}, 卡住时间 {stuckTimer:F2}s, 有输入: {hasInput}");
                return true;
            }
        }
        else
        {
            // 重置卡住计时器
            stuckTimer = 0f;
        }
        
        lastPosition = currentPosition;
        return false;
    }
    
    // 尝试脱离角落 - 改进版本
    void TryUnstuckFromCorner()
    {
        Debug.Log("尝试脱离角落...");
        
        // 检查当前输入状态
        bool hasMovementInput = Mathf.Abs(inputX) > 0.01f;
        
        if (hasMovementInput)
        {
            // 如果有输入，尝试向相反方向移动
            Vector2 velocity = rb.velocity;
            velocity.x = -facing * 3f; // 向相反方向移动
            velocity.y = Mathf.Max(velocity.y, 2f); // 稍微向上
            rb.velocity = velocity;
            Debug.Log($"有输入 - 向相反方向移动: {-facing}");
        }
        else
        {
            // 如果没有输入，尝试向上推
            Vector2 velocity = rb.velocity;
            velocity.y = Mathf.Max(velocity.y, 5f); // 给一个向上的力
            rb.velocity = velocity;
            
            // 稍微向后移动
            Vector2 position = transform.position;
            position.x -= facing * 0.15f; // 向后移动一点
            transform.position = position;
            Debug.Log("无输入 - 向上推并向后移动");
        }
        
        // 重置卡住计时器
        stuckTimer = 0f;
        
        Debug.Log("已尝试脱离角落");
    }

    void HandleMovement()
    {
        // 暴力A/D按键检测机制 - 只在加速度模式下生效
        if (useAcceleration)
        {
            bool pressingA = Input.GetKey(KeyCode.A);
            bool pressingD = Input.GetKey(KeyCode.D);
            float currentVelocityX = rb.velocity.x;
            
            // 情况1：没有检测到A或D - 立即强制重置水平速度为0
            if (!pressingA && !pressingD)
            {
                Vector2 velocity = rb.velocity;
                velocity.x = 0f;
                rb.velocity = velocity;
                Debug.Log("暴力检测 - 无A/D输入，强制停止");
                return;
            }
            
            // 情况2：只按A - 立即向左移动
            if (pressingA && !pressingD)
            {
                if (currentVelocityX > 0)
                {
                    // 如果当前向右移动，立即反向（继承速度）
                    float reversedSpeed = -currentVelocityX;
                    Vector2 velocity = rb.velocity;
                    velocity.x = reversedSpeed;
                    rb.velocity = velocity;
                    Debug.Log($"暴力检测 - A键反向移动: {currentVelocityX:F2} -> {reversedSpeed:F2}");
                }
                else
                {
                    // 如果当前向左或停止，正常向左移动
                    Vector2 velocity = rb.velocity;
                    velocity.x = -groundMoveSpeed;
                    rb.velocity = velocity;
                    Debug.Log($"暴力检测 - A键正常移动: {currentVelocityX:F2} -> {-groundMoveSpeed:F2}");
                }
                return;
            }
            
            // 情况3：只按D - 立即向右移动
            if (pressingD && !pressingA)
            {
                if (currentVelocityX < 0)
                {
                    // 如果当前向左移动，立即反向（继承速度）
                    float reversedSpeed = -currentVelocityX;
                    Vector2 velocity = rb.velocity;
                    velocity.x = reversedSpeed;
                    rb.velocity = velocity;
                    Debug.Log($"暴力检测 - D键反向移动: {currentVelocityX:F2} -> {reversedSpeed:F2}");
                }
                else
                {
                    // 如果当前向右或停止，正常向右移动
                    Vector2 velocity = rb.velocity;
                    velocity.x = groundMoveSpeed;
                    rb.velocity = velocity;
                    Debug.Log($"暴力检测 - D键正常移动: {currentVelocityX:F2} -> {groundMoveSpeed:F2}");
                }
                return;
            }
            
            // 情况4：同时按A和D - 立即停止
            if (pressingA && pressingD)
            {
                Vector2 velocity = rb.velocity;
                velocity.x = 0f;
                rb.velocity = velocity;
                Debug.Log("暴力检测 - 同时按A/D，立即停止");
                return;
            }
        }
        
        // 非加速度模式或加速度模式下的备用逻辑
        // 如果没有输入，直接停止移动
        if (!hasInput)
        {
            Vector2 velocity = rb.velocity;
            velocity.x = 0f;
            rb.velocity = velocity;
            Debug.Log("HandleMovement - 无输入，直接停止");
            return;
        }
        
        // 检查是否卡在角落
        if (IsStuckInCorner())
        {
            TryUnstuckFromCorner();
            return; // 跳过正常移动处理
        }
        
        // 检查当前和预测的墙壁碰撞
        bool wallAhead = IsWallAhead();
        bool predictedWallAhead = PredictWallCollision();

        float targetVelX = 0f;
        
        // 计算目标速度
        if (!wallAhead && !predictedWallAhead)
        {
            // 根据输入和状态计算目标速度
            targetVelX = CalculateTargetVelocity();
        }
        else
        {
            // 如果检测到墙壁，根据输入方向决定是否停止
            if (inputX * facing > 0)
            {
                // 朝向墙壁移动，停止
                targetVelX = 0f;
            }
            else if (inputX * facing < 0)
            {
                // 远离墙壁移动，允许移动
                targetVelX = CalculateTargetVelocity();
            }
            else
            {
                // 无输入或朝向墙壁，停止
                targetVelX = 0f;
            }
        }

        float currentVX = rb.velocity.x;
        float newVX = currentVX;

        if (!useAcceleration)
        {
            // 直接设定速度，固定移动模式
            newVX = targetVelX;
        }
        else
        {
            // 使用加速度/减速度平滑过渡到目标速度
            newVX = CalculateNewVelocity(currentVX, targetVelX);
        }

        // 应用速度限制防止穿模
        Vector2 newVelocity = new Vector2(newVX, rb.velocity.y);
        newVelocity = LimitVelocity(newVelocity);
        
        rb.velocity = newVelocity;
    }
    
    // 计算按键优先级输入 - 真正的优先级系统
    float CalculatePriorityInput(bool pressingA, bool pressingD)
    {
        // 如果在跳跃且启用了方向锁定，使用锁定的方向
        if (isJumping && lockJumpDirection && jumpDirectionLockTimer > 0)
        {
            Debug.Log($"跳跃方向锁定中: {lockedJumpDirection}");
            return lockedJumpDirection;
        }
        
        // 情况1：只按A
        if (pressingA && !pressingD)
        {
            return -1f; // 向左
        }
        
        // 情况2：只按D
        if (pressingD && !pressingA)
        {
            return 1f; // 向右
        }
        
        // 情况3：同时按A和D - 使用优先级系统
        if (pressingA && pressingD)
        {
            // 如果A最后被按下，优先A
            if (aWasPressedLast)
            {
                Debug.Log("同时按下A和D，A优先");
                return -1f;
            }
            // 如果D最后被按下，优先D
            else if (dWasPressedLast)
            {
                Debug.Log("同时按下A和D，D优先");
                return 1f;
            }
            // 如果都没有记录，默认停止
            else
            {
                Debug.Log("同时按下A和D，无优先级记录，停止");
                return 0f;
            }
        }
        
        // 情况4：都没按
        return 0f;
    }

    // 计算目标速度 - 重写版本，彻底修复停止问题
    float CalculateTargetVelocity()
    {
        // 如果没有输入，直接返回0
        if (!hasInput)
        {
            return 0f;
        }
        
        // 如果有输入，计算目标速度
        float targetVel = inputX * groundMoveSpeed;
        
        // 速度平滑处理，防止突变（只在有输入时进行）
        if (useAcceleration && hasInput)
        {
            float velocityChange = Mathf.Abs(targetVel - lastTargetVelocity);
            if (velocityChange > velocityChangeThreshold)
            {
                // 如果速度变化过大，进行平滑过渡
                float smoothFactor = velocityChangeThreshold / velocityChange;
                targetVel = Mathf.Lerp(lastTargetVelocity, targetVel, smoothFactor);
                Debug.Log($"速度变化过大，平滑处理: {lastTargetVelocity:F2} -> {targetVel:F2}");
            }
        }
        
        lastTargetVelocity = targetVel;
        return targetVel;
    }
    
    // 计算新的速度 - 重写版本，实现立即继承速度反向移动
    float CalculateNewVelocity(float currentVX, float targetVelX)
    {
        // 如果没有输入，直接返回0
        if (!hasInput)
        {
            return 0f;
        }
        
        // 如果目标速度为0且当前速度很小，直接设为0
        if (Mathf.Abs(targetVelX) < 0.01f && Mathf.Abs(currentVX) < stopThreshold)
        {
            return 0f;
        }
        
        // 计算速度差值
        float velocityDiff = targetVelX - currentVX;
        
        // 如果速度差值很小，直接返回目标速度
        if (Mathf.Abs(velocityDiff) < 0.01f)
        {
            return targetVelX;
        }
        
        // 检查是否变向（目标速度与当前速度方向相反）
        bool isDirectionChange = (targetVelX > 0 && currentVX < 0) || (targetVelX < 0 && currentVX > 0);
        
        // 立即反向移动机制 - 直接反转当前速度
        if (isDirectionChange && hasInput && isGrounded)
        {
            // 检查变向冷却时间
            bool canChangeDirection = Time.time - lastDirectionChangeTime > directionChangeCooldown;
            
            if (canChangeDirection && Mathf.Abs(currentVX) > directionChangeThreshold)
            {
                // 立即反向：直接反转当前速度，保持速度大小
                float reversedSpeed = -currentVX;
                lastDirectionChangeTime = Time.time;
                Debug.Log($"立即反向移动 - 反转当前速度: {currentVX:F2} -> {reversedSpeed:F2}");
                return reversedSpeed;
            }
        }
        
        // 确定加速度
        float acceleration = accel;
        
        // 在空中时减少加速度
        if (!isGrounded)
        {
            acceleration *= airAccelerationMultiplier;
        }
        
        // 变向时使用更高的加速度
        if (isDirectionChange && hasInput)
        {
            acceleration *= 4f; // 变向时4倍加速度
            Debug.Log($"变向加速 - 使用高加速度: {acceleration:F2}");
        }
        
        // 计算新速度
        float sign = Mathf.Sign(velocityDiff);
        float accelerationAmount = acceleration * Time.fixedDeltaTime;
        
        // 确保不会超过目标速度
        float maxChange = Mathf.Abs(velocityDiff);
        accelerationAmount = Mathf.Min(accelerationAmount, maxChange);
        
        float newVX = currentVX + sign * accelerationAmount;
        
        // 限制最大速度
            newVX = Mathf.Clamp(newVX, -maxSpeed, maxSpeed);
        
        return newVX;
    }
    
    // 获取减速度
    float GetDeceleration()
    {
        if (!useFastDeceleration)
        {
            return decel;
        }
        
        // 使用快速减速度
        float baseDecel = decel;
        
        // 如果无输入时间较长，使用更快的减速度
        if (noInputTimer > 0.1f)
        {
            baseDecel = fastDecel;
        }
        
        // 确保减速度至少是加速度的倍数
        float minDecel = accel * decelMultiplier;
        return Mathf.Max(baseDecel, minDecel);
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
        maxSpeed = Mathf.Max(0.01f, maxSpeed);
        accel = Mathf.Max(0f, accel);
        decel = Mathf.Max(0f, decel);
        groundMoveSpeed = Mathf.Clamp(groundMoveSpeed, 0f, maxSpeed);

        // 验证碰撞检测参数
        groundCheckRays = Mathf.Max(1, groundCheckRays);
        wallCheckRays = Mathf.Max(1, wallCheckRays);
        collisionPredictionTime = Mathf.Max(0.01f, collisionPredictionTime);
        maxSafeSpeed = Mathf.Max(maxSpeed, maxSafeSpeed);
        
        // 验证跳跃参数
        jumpBufferTime = Mathf.Max(0f, jumpBufferTime);
        coyoteTime = Mathf.Max(0f, coyoteTime);
        jumpCooldown = Mathf.Max(0f, jumpCooldown);
        minJumpHeight = Mathf.Max(0f, minJumpHeight);
        
        // 验证移动参数
        airSpeedDecay = Mathf.Clamp(airSpeedDecay, 0.1f, 1f);
        landingSpeedThreshold = Mathf.Max(0f, landingSpeedThreshold);
        landingTransitionTime = Mathf.Max(0f, landingTransitionTime);
        
        // 验证高级移动控制参数
        fastDecel = Mathf.Max(accel, fastDecel);
        stopThreshold = Mathf.Max(0.01f, stopThreshold);
        decelMultiplier = Mathf.Max(1f, decelMultiplier);
        
        // 验证跳跃移动控制参数
        landingSpeedClearThreshold = Mathf.Max(0f, landingSpeedClearThreshold);

        Debug.Log($"[PlayerController] 初始化完成 - CCD: {useContinuousCollisionDetection}, 地面射线: {groundCheckRays}, 墙壁射线: {wallCheckRays}, 最大安全速度: {maxSafeSpeed}");
        Debug.Log($"[PlayerController] 跳跃设置 - 缓冲时间: {jumpBufferTime}, 土狼时间: {coyoteTime}, 冷却: {jumpCooldown}, 最小高度: {minJumpHeight}");
        Debug.Log($"[PlayerController] 移动设置 - 保持空中速度: {preserveAirSpeed}, 空中衰减: {airSpeedDecay}, 落地阈值: {landingSpeedThreshold}, 过渡时间: {landingTransitionTime}");
        Debug.Log($"[PlayerController] 高级移动 - 快速减速: {useFastDeceleration}, 快速减速度: {fastDecel}, 停止阈值: {stopThreshold}, 减速度倍数: {decelMultiplier}");
        Debug.Log($"[PlayerController] 跳跃移动 - 跳跃清除: {clearHorizontalSpeedOnJump}, 落地清除: {clearHorizontalSpeedOnLanding}, 清除阈值: {landingSpeedClearThreshold}, 继承地面速度: {inheritGroundSpeedOnJump}");
    }

    // Update is called once per frame
    void Update()
    {
        // 检测A、D按键的精确状态
        bool pressingA = Input.GetKey(KeyCode.A);
        bool pressingD = Input.GetKey(KeyCode.D);
        
        // 检测按键按下和释放
        bool aPressed = !wasPressingA && pressingA;
        bool dPressed = !wasPressingD && pressingD;
        bool aReleased = wasPressingA && !pressingA;
        bool dReleased = wasPressingD && !pressingD;
        keyReleased = aReleased || dReleased;
        
        // 更新按键优先级状态（跳跃时暂停更新）
        if (!isJumping || !lockJumpDirection || jumpDirectionLockTimer <= 0)
        {
            if (aPressed)
            {
                aWasPressedLast = true;
                dWasPressedLast = false;
                Debug.Log("A键按下，设置为优先");
            }
            if (dPressed)
            {
                dWasPressedLast = true;
                aWasPressedLast = false;
                Debug.Log("D键按下，设置为优先");
            }
        }
        else
        {
            Debug.Log("跳跃中，暂停按键优先级更新");
        }
        
        // 更新按键状态
        wasPressingA = pressingA;
        wasPressingD = pressingD;
        
        // 按键优先级系统 - 实现您要求的移动逻辑
        float newInputX = CalculatePriorityInput(pressingA, pressingD);

        // 检测输入变化（使用更宽松的阈值避免误判）
        inputChanged = Mathf.Abs(newInputX - lastInputX) > 0.1f;
        inputX = newInputX;
        lastInputX = newInputX;

        // 检测输入状态
        hasInput = Mathf.Abs(inputX) > 0.01f;
        wasMovingLastFrame = hasInput;
        
        // 更新无输入计时器
        if (!hasInput)
        {
            noInputTimer += Time.deltaTime;
        }
        else
        {
            noInputTimer = 0f;
            isStopping = false;
        }

        // 更新朝向（只在有明确输入时更新，避免快速切换时卡住）
        if (inputX > 0) facing = 1;
        else if (inputX < 0) facing = -1;
        // 注意：当inputX为0时，不改变facing，保持当前朝向

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
        if (Input.GetKeyDown(KeyCode.K)) Debug.Log("[Input] K pressed");

    }

    void FixedUpdate()
    {
        CheckGround();
        
        // 更新跳跃状态
        UpdateJumpMovementState();
        
        // 更新移动状态
        UpdateMovementState();
        
        // 处理移动
        HandleMovement();
        
        // 处理跳跃
        HandleJump();
        
        // 更新状态
        UpdateStates();
        
        // 更新计时器
        UpdateTimers();
        
        // 更新位置记录（用于卡住检测）
        lastPosition = transform.position;
        
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
        
        // 绘制移动状态
        if (preserveAirSpeed && Mathf.Abs(preservedAirSpeed) > 0.1f)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f); // 橙色
            Vector3 speedPos = transform.position + Vector3.up * 2.5f;
            Gizmos.DrawWireCube(speedPos, Vector3.one * 0.12f);
        }
        
        // 绘制落地过渡状态
        if (isLandingTransition)
        {
            Gizmos.color = Color.white;
            Vector3 transitionPos = transform.position + Vector3.up * 3f;
            float scale = landingTransitionTimer / landingTransitionTime;
            Gizmos.DrawWireCube(transitionPos, Vector3.one * 0.1f * scale);
        }
        
        // 绘制停止状态
        if (isStopping)
        {
            Gizmos.color = new Color(1f, 0f, 0f); // 红色
            Vector3 stopPos = transform.position + Vector3.up * 3.5f;
            Gizmos.DrawWireCube(stopPos, Vector3.one * 0.08f);
        }
        
        // 绘制无输入状态
        if (!hasInput && noInputTimer > 0.1f)
        {
            Gizmos.color = new Color(0.5f, 0.5f, 0.5f); // 灰色
            Vector3 noInputPos = transform.position + Vector3.up * 4f;
            Gizmos.DrawWireCube(noInputPos, Vector3.one * 0.06f);
        }
        
        // 绘制跳跃移动状态
        if (isJumping && clearHorizontalSpeedOnJump)
        {
            Gizmos.color = new Color(0f, 1f, 1f); // 青色
            Vector3 jumpClearPos = transform.position + Vector3.up * 4.5f;
            Gizmos.DrawWireCube(jumpClearPos, Vector3.one * 0.05f);
        }
        
        // 绘制落地清除状态
        if (clearHorizontalSpeedOnLanding && isGrounded && !hasInput)
        {
            Gizmos.color = new Color(1f, 0f, 1f); // 洋红色
            Vector3 landingClearPos = transform.position + Vector3.up * 5f;
            Gizmos.DrawWireCube(landingClearPos, Vector3.one * 0.04f);
        }
        
        // 绘制输入变化状态
        if (inputChanged)
        {
            Gizmos.color = new Color(1f, 1f, 0f); // 黄色
            Vector3 inputChangePos = transform.position + Vector3.up * 5.5f;
            Gizmos.DrawWireCube(inputChangePos, Vector3.one * 0.03f);
        }
        
        // 绘制继承地面速度状态
        if (inheritGroundSpeedOnJump && isJumping)
        {
            Gizmos.color = new Color(0f, 1f, 0f); // 绿色
            Vector3 inheritPos = transform.position + Vector3.up * 6f;
            Gizmos.DrawWireCube(inheritPos, Vector3.one * 0.02f);
        }
        
        // 绘制按键释放状态
        if (keyReleased)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f); // 橙色
            Vector3 keyReleasePos = transform.position + Vector3.up * 6.5f;
            Gizmos.DrawWireCube(keyReleasePos, Vector3.one * 0.015f);
        }
        
        // 绘制按键优先级系统状态（简化版本）
        if (hasInput)
        {
            // 输入状态 - 蓝色
            Gizmos.color = Color.blue;
            Vector3 inputPos = transform.position + Vector3.up * 7f;
            Gizmos.DrawWireCube(inputPos, Vector3.one * 0.02f);
            
            // 方向指示 - 箭头
            Gizmos.color = inputX > 0 ? Color.green : Color.yellow;
            Vector3 arrowPos = transform.position + Vector3.up * 7.5f;
            Vector3 arrowDir = Vector3.right * inputX * 0.1f;
            Gizmos.DrawLine(arrowPos, arrowPos + arrowDir);
        }
    }
    
    // 更新移动状态
    void UpdateMovementState()
    {
        // 更新落地过渡状态
        if (isLandingTransition)
        {
            landingTransitionTimer -= Time.fixedDeltaTime;
            if (landingTransitionTimer <= 0)
            {
                isLandingTransition = false;
                Debug.Log("落地过渡结束");
            }
        }
        
        // 检测落地
        if (!wasInAirLastFrame && !isGrounded)
        {
            OnTakeOff();
        }
        else if (wasInAirLastFrame && isGrounded)
        {
            OnLanding();
        }
        
        wasInAirLastFrame = !isGrounded;
    }
    
    // 更新状态
    void UpdateStates()
    {
        // 更新朝向
        if (inputX > 0) facing = 1;
        else if (inputX < 0) facing = -1;
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
        
        // 更新无输入计时器
        if (!hasInput)
        {
            noInputTimer += Time.fixedDeltaTime;
        }
        else
        {
            noInputTimer = 0f;
        }
    }
    
    // 跳跃开始事件
    void OnJumpStart()
    {
        isJumping = true;
        jumpStartY = transform.position.y;
        jumpStartHorizontalSpeed = rb.velocity.x;
        
        // 锁定跳跃方向
        if (lockJumpDirection)
        {
            lockedJumpDirection = inputX;
            jumpDirectionLockTimer = jumpDirectionLockTime;
            Debug.Log($"跳跃开始，锁定方向: {lockedJumpDirection}");
        }
        
        Debug.Log($"跳跃开始 - Y位置: {jumpStartY:F2}, 水平速度: {jumpStartHorizontalSpeed:F2}");
    }
    
    // 落地事件
    void OnLanding()
    {
        Debug.Log("检测到落地");
        
        // 清除保持的空中速度
        preservedAirSpeed = 0f;
        
        // 开始落地过渡
        if (landingTransitionTime > 0)
        {
            isLandingTransition = true;
            landingTransitionTimer = landingTransitionTime;
        }
        
        // 重置双跳状态
        doubleJumpUsed = false;
    }
    
    // 起飞事件
    void OnTakeOff()
    {
        Debug.Log("检测到起飞");
        
        // 保存当前水平速度
        if (preserveAirSpeed)
        {
            preservedAirSpeed = rb.velocity.x;
            Debug.Log($"保存空中速度: {preservedAirSpeed:F2}");
        }
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
