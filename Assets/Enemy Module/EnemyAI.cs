using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyAI : MonoBehaviour
{
    [Header("移动参数")]
    public float moveSpeed = 3f;
    public float chaseSpeed = 4.5f;
    public float wallCheckDistance = 0.2f;
    public LayerMask wallLayer;

    [Header("检测与攻击参数")]
    public float attackRange = 2f;
    public float attackDelay = 0.5f;
    public int attackDamage = 10;

    // ===== 新增：攻击类型支持（圆形 / Box） =====
    public enum AttackMode { Circle, Box }
    [Header("攻击模式")]
    public AttackMode attackMode = AttackMode.Circle; // 默认为圆形攻击

    [Header("拳头Box攻击设置（仅在 attackMode == Box 时生效）")]
    public Vector2 boxOffset = new Vector2(1f, 0f); // 相对于 attackPoint（或敌人根）的偏移（本地/世界取决于实现）
    public Vector2 boxSize = new Vector2(1.2f, 0.8f); // box 大小
    public float boxAngle = 0f; // 旋转角度（一般为0）
    [Tooltip("是否在敌人转向时自动翻转Box攻击范围")]
    public bool flipBoxWithEnemy = true; // ✅ 新增：控制Box是否随敌人翻转

    [Header("圆形攻击设置（仅在 attackMode == Circle 时生效）")]
    public Vector2 circleOffset = Vector2.zero; // 相对于 attackPoint 的偏移
    public Transform detectionPoint;
    public Transform attackPoint;
    public Transform wallCheckLeft;
    public Transform wallCheckRight;

    [Header("玩家设置")]
    public Transform player;

    [Header("检测区域形状 (椭圆)")]
    public float detectionWidth = 10f;
    public float detectionHeight = 5f;
    public LayerMask playerLayer;

    [Header("视线检测参数")]
    public float sightCheckDistance = 10f;
    public LayerMask sightBlockingLayers; // 应该包含wallLayer
    public Transform sightCheckPoint; // 视线检测起点（可选，如眼睛位置）
    [Header("高级视线检测")]
    public bool useAdvancedSightCheck = true; // 是否使用多角度视线检测
    [Header("调试绘制")]
    public bool drawSightGizmos = true; // 控制是否绘制视线Gizmos

    [Header("击退参数")]
    public float windKnockbackDuration = 0.3f;
    public float windKnockbackForce = 8f;

    [Header("动画设置")]
    public string attackAnimationName = "Attack";

    [Header("动画参数名称")]
    public string walkParamName = "isWalk";
    public string attackParamName = "Attack";
    public string hurtParamName = "isHurt"; // ✅ 修复：默认值改为 isHurt
    public string deadParamName = "Dead";

    [Header("动画对象引用")]
    public Transform animationChild;

    [Header("受击动画设置")]
    public float hurtAnimationDuration = 0.3f; // 受击动画持续时间

    // ===== 新增：AB点巡逻参数 =====
    [Header("AB点巡逻设置")]
    [Tooltip("是否启用AB点巡逻")]
    public bool useABPatrol = false; // 新增：控制是否使用AB点巡逻
    [Tooltip("巡逻点A")]
    public Transform patrolPointA;
    [Tooltip("巡逻点B")]
    public Transform patrolPointB;
    [Tooltip("到达点的误差范围")]
    public float reachThreshold = 0.5f; // 增加阈值，避免抽搐
    [Tooltip("在到达点停留的时间")]
    public float waitTimeAtPoint = 1f;
    [Tooltip("减速距离 - 在距离目标点多远时开始减速")]
    public float slowingDistance = 2f; // 新增：减速距离

    private Rigidbody2D rb;
    private bool isAttacking = false;
    private bool isChasing = false;
    private bool facingRight = true;
    private bool hitWall = false;
    [HideInInspector] public bool isKnockedBack = false;
    private float flipThreshold = 0.5f;

    // ✅ 新增：受击状态变量
    private bool isHurting = false;
    private float hurtTimer = 0f;

    // ✅ 新增：死亡状态标志
    private bool isDead = false;

    // ===== 新增：AB点巡逻状态变量 =====
    private Transform currentPatrolTarget; // 当前巡逻目标点
    private bool isWaitingAtPoint = false; // 是否在点等待
    private float waitTimer = 0f; // 等待计时器
    private bool hasReachedPoint = false; // 标记是否已经到达过当前点
    private Vector2 lastPosition; // 记录上一帧位置，用于检测是否卡住
    private float stuckTimer = 0f; // 卡住计时器
    private const float STUCK_TIME_THRESHOLD = 2f; // 卡住时间阈值

    private SpriteRenderer sprite;
    private Animator anim;
    private Attribute enemyAttributes;

    // 动画参数
    private int animIsWalk;
    private int animIsAttack;
    private int animIsHurt;
    private int animIsDead;

    // 转向冷却时间，防止频繁转向
    private float lastFlipTime = 0f;
    private float flipCooldown = 0.5f;

    // 记录初始缩放值
    private Vector3 originalScale;

    // 攻击动画相关变量
    private bool attackAnimationPlaying = false;
    private float attackAnimationTime = 0f;

    // 动画状态跟踪
    private Dictionary<string, bool> animationStates = new Dictionary<string, bool>();

    // 实际检测到的参数名
    private string actualAttackParamName = "";
    private string actualHurtParamName = ""; // ✅ 新增：实际检测到的受击参数名

    // 玩家属性组件缓存和根对象查找
    private Attribute playerAttributes;
    private Transform playerRoot;

    // ✅ 新增：动画状态跟踪
    private string currentAnimationState = "Idle";

    // ✅ 新增：玩家碰撞体缓存
    private BoxCollider2D playerCollider;

    // ✅ 新增：获取玩家碰撞框中心位置的属性
    private Vector2 PlayerColliderCenter
    {
        get
        {
            if (playerCollider != null)
            {
                // 使用bounds.center获取世界空间中的碰撞框中心
                return playerCollider.bounds.center;
            }
            // 如果没有碰撞体，回退到使用Transform位置
            if (playerRoot != null && playerRoot != player)
            {
                return playerRoot.position;
            }
            return player != null ? player.position : Vector2.zero;
        }
    }

    // ✅ 新增：获取敌人视觉位置（用于Gizmos绘制和调试）
    private Vector2 EnemyVisualPosition
    {
        get
        {
            // 如果有视觉对象，使用视觉对象的位置
            if (sprite != null && sprite.transform != null)
            {
                return sprite.transform.position;
            }
            // 否则使用根对象位置
            return transform.position;
        }
    }

    // ✅ 新增：获取当前Box偏移（考虑翻转）
    private Vector2 CurrentBoxOffset
    {
        get
        {
            if (flipBoxWithEnemy && !facingRight)
            {
                // 当敌人朝左时，翻转Box的X偏移
                return new Vector2(-boxOffset.x, boxOffset.y);
            }
            return boxOffset;
        }
    }

    // ✅ 新增：获取当前Box角度（考虑翻转）
    private float CurrentBoxAngle
    {
        get
        {
            if (flipBoxWithEnemy && !facingRight)
            {
                // 当敌人朝左时，翻转Box的角度（如果需要）
                return -boxAngle;
            }
            return boxAngle;
        }
    }

    // ✅ 新增：编辑器刷新支持
#if UNITY_EDITOR
    private void OnValidate()
    {
        // 确保数值合理
        attackRange = Mathf.Max(0.1f, attackRange);
        moveSpeed = Mathf.Max(0.1f, moveSpeed);
        chaseSpeed = Mathf.Max(0.1f, chaseSpeed);
        wallCheckDistance = Mathf.Max(0.01f, wallCheckDistance);
        
        // 延迟调用以确保组件已初始化
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null) return;
            
            // 刷新场景视图
            UnityEditor.SceneView.RepaintAll();
            
            // 输出调试信息
            Debug.Log($"🔄 EnemyAI 配置已更新 - 攻击模式: {attackMode}");
        };
    }
#endif

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // 修复：从子对象获取 SpriteRenderer 和 Animator
        if (animationChild != null)
        {
            sprite = animationChild.GetComponent<SpriteRenderer>();
            anim = animationChild.GetComponent<Animator>();
        }
        else
        {
            // 如果没有手动指定，尝试自动查找
            sprite = GetComponentInChildren<SpriteRenderer>();
            anim = GetComponentInChildren<Animator>();
        }

        enemyAttributes = GetComponent<Attribute>();

        // 记录初始缩放值
        if (sprite != null)
        {
            originalScale = sprite.transform.localScale;
        }
        else
        {
            originalScale = transform.localScale;
        }

        // ✅ 修复：先检测实际参数名，再初始化动画参数哈希
        DetectAnimationParameters();

        // 初始化动画参数哈希 - 只有在参数存在时才初始化
        InitializeAnimationParameters();

        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.freezeRotation = true;

        // ✅ 修复：改进玩家对象查找逻辑
        FindAndSetupPlayer();

        // ✅ 新增：视线层设置
        if (sightBlockingLayers == 0)
        {
            // 默认包含墙体层
            sightBlockingLayers = wallLayer;
        }

        // ✅ 新增：注册死亡事件监听
        if (enemyAttributes != null)
        {
            enemyAttributes.OnDeath += OnEnemyDeath;
            // ✅ 新增：注册受伤事件监听
            enemyAttributes.OnTakeDamage += OnEnemyTakeDamage;
        }

        // ===== 新增：AB点巡逻初始化 =====
        if (useABPatrol)
        {
            InitializeABPatrol();
        }

        // 确保初始朝向正确
        ApplyFacingDirection();

        // 记录初始位置
        lastPosition = transform.position;

        // ✅ 新增：验证组件获取情况
        Debug.Log($"🎯 EnemyAI初始化完成 - Animator: {anim != null}, SpriteRenderer: {sprite != null}, Attribute: {enemyAttributes != null}, 玩家Attribute: {playerAttributes != null}, 玩家根对象: {playerRoot?.name ?? "未找到"}");

        // ✅ 新增：验证玩家位置修复和碰撞框检测
        if (playerCollider != null)
        {
            Debug.Log($"✅ 玩家碰撞框检测已启用 - Size: {playerCollider.size}, Offset: {playerCollider.offset}, 将使用bounds.center进行检测");
        }
        else if (playerRoot != null && playerRoot != player)
        {
            Debug.Log($"⚠️ 玩家位置修复已启用（但未找到碰撞体）- 使用根对象'{playerRoot.name}'而不是子对象'{player.name}'进行检测");
        }
    }

    private void OnDestroy()
    {
        // ✅ 新增：取消事件注册，防止内存泄漏
        if (enemyAttributes != null)
        {
            enemyAttributes.OnDeath -= OnEnemyDeath;
            // ✅ 新增：取消受伤事件注册
            enemyAttributes.OnTakeDamage -= OnEnemyTakeDamage;
        }
    }

    // ===== 新增：AB点巡逻初始化方法 =====
    private void InitializeABPatrol()
    {
        if (patrolPointA == null || patrolPointB == null)
        {
            Debug.LogError("❌ AB点巡逻已启用，但未设置巡逻点A或B！");
            useABPatrol = false;
            return;
        }

        // 设置初始巡逻目标为点A
        currentPatrolTarget = patrolPointA;
        hasReachedPoint = false; // 重置到达标记
        Debug.Log($"🔄 AB点巡逻已初始化 - 点A: {patrolPointA.position}, 点B: {patrolPointB.position}");
    }

    // ===== 修复：改进的AB点巡逻方法 =====
    private void ABPatrol()
    {
        if (isWaitingAtPoint)
        {
            // 在点等待期间停止移动
            rb.velocity = new Vector2(0, rb.velocity.y);
            waitTimer += Time.deltaTime;

            // 等待时间结束后切换到下一个点
            if (waitTimer >= waitTimeAtPoint)
            {
                isWaitingAtPoint = false;
                waitTimer = 0f;
                hasReachedPoint = false; // 重置到达标记
                SwitchPatrolTarget();
                Debug.Log($"🔄 等待结束，切换到下一个巡逻点: {currentPatrolTarget.name}");
            }
            return;
        }

        // 计算到目标点的方向和距离
        Vector2 toTarget = currentPatrolTarget.position - transform.position;
        float distanceToTarget = Mathf.Abs(toTarget.x);
        float direction = Mathf.Sign(toTarget.x);

        // 检查是否到达目标点 - 修复：使用更严格的到达条件
        if (!hasReachedPoint && distanceToTarget <= reachThreshold)
        {
            // 到达目标点，开始等待
            rb.velocity = new Vector2(0, rb.velocity.y);
            isWaitingAtPoint = true;
            hasReachedPoint = true; // 标记已经到达
            stuckTimer = 0f; // 重置卡住计时器
            Debug.Log($"✅ 到达巡逻点 {currentPatrolTarget.name}，距离: {distanceToTarget:F2}，开始等待 {waitTimeAtPoint} 秒");
            return;
        }

        // 检查是否卡住
        CheckIfStuck();

        // 检查前方是否有墙体阻挡
        Transform checkPoint = direction > 0 ? wallCheckRight : wallCheckLeft;
        if (checkPoint != null)
        {
            Vector2 checkDir = direction > 0 ? Vector2.right : Vector2.left;
            RaycastHit2D wallHit = Physics2D.Raycast(checkPoint.position, checkDir, wallCheckDistance, wallLayer);

            if (wallHit.collider != null)
            {
                // 检测到墙体，转向并切换到下一个点
                Debug.Log($"🧱 AB点巡逻检测到墙体，转向并切换目标点");
                SwitchPatrolTarget();
                return;
            }
        }

        // 朝向目标点移动
        bool shouldFaceRight = direction > 0;
        if (shouldFaceRight != facingRight && Time.time >= lastFlipTime + flipCooldown)
        {
            Flip(shouldFaceRight);
            lastFlipTime = Time.time;
        }

        // 移动 - 修复：接近目标点时平滑减速
        float currentSpeed = CalculatePatrolSpeed(distanceToTarget, direction);

        rb.velocity = new Vector2(direction * currentSpeed, rb.velocity.y);

        // 调试信息 - 每60帧输出一次
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"🔄 AB点巡逻 - 目标: {currentPatrolTarget.name}, 距离: {distanceToTarget:F2}, 方向: {direction}, 速度: {currentSpeed:F2}");
        }
    }

    // ===== 新增：计算巡逻速度（包含平滑减速） =====
    private float CalculatePatrolSpeed(float distanceToTarget, float direction)
    {
        // 如果距离小于减速距离，开始减速
        if (distanceToTarget <= slowingDistance)
        {
            // 使用平滑的减速曲线
            float speedFactor = distanceToTarget / slowingDistance;
            // 使用二次曲线使减速更平滑
            speedFactor = Mathf.Pow(speedFactor, 1.5f);
            // 确保最小速度，避免完全停止前抽搐
            float minSpeed = 0.3f;
            return Mathf.Max(moveSpeed * speedFactor, minSpeed);
        }

        return moveSpeed;
    }

    // ===== 新增：检查是否卡住 =====
    private void CheckIfStuck()
    {
        // 计算当前位置和上一帧位置的距离
        float moveDistance = Vector2.Distance(transform.position, lastPosition);

        // 如果移动距离很小，说明可能卡住了
        if (moveDistance < 0.01f && !isWaitingAtPoint)
        {
            stuckTimer += Time.deltaTime;

            // 如果卡住时间超过阈值，强制切换目标点
            if (stuckTimer >= STUCK_TIME_THRESHOLD)
            {
                Debug.LogWarning($"⚠️ 敌人卡住 {stuckTimer:F1} 秒，强制切换巡逻点");
                SwitchPatrolTarget();
                stuckTimer = 0f;
            }
        }
        else
        {
            // 如果没有卡住，重置计时器
            stuckTimer = 0f;
        }

        // 更新上一帧位置
        lastPosition = transform.position;
    }

    // ===== 修复：改进的切换巡逻目标点方法 =====
    private void SwitchPatrolTarget()
    {
        if (currentPatrolTarget == patrolPointA)
        {
            currentPatrolTarget = patrolPointB;
        }
        else
        {
            currentPatrolTarget = patrolPointA;
        }

        // 重置相关状态
        isWaitingAtPoint = false;
        waitTimer = 0f;
        hasReachedPoint = false;
        stuckTimer = 0f;

        Debug.Log($"🔄 切换巡逻目标: {currentPatrolTarget.name}");
    }

    // ✅ 新增：敌人受伤事件处理
    private void OnEnemyTakeDamage(int damage, GameObject attacker)
    {
        Debug.Log($"🩸 敌人受到 {damage} 点伤害，来自: {attacker?.name ?? "未知"}");

        // 触发受击动画
        TriggerHurtAnimation();

        // 如果攻击者不为空，可以添加面向攻击者的逻辑
        if (attacker != null)
        {
            // 可选：让敌人面向攻击者
            // float xDiff = attacker.transform.position.x - transform.position.x;
            // bool attackerOnRight = xDiff > 0;
            // if (attackerOnRight != facingRight)
            // {
            //     Flip(attackerOnRight);
            // }
        }
    }

    // ✅ 修复：触发受击动画（支持自动检测参数名）
    void TriggerHurtAnimation()
    {
        if (isHurting || !enemyAttributes.IsAlive) return;

        if (anim == null)
        {
            Debug.LogWarning("⚠️ 无法触发受击动画: Animator为空");
            return;
        }

        // ✅ 修复：优先使用实际检测到的参数名，如果没有则使用配置的参数名
        string paramToUse = !string.IsNullOrEmpty(actualHurtParamName) ? actualHurtParamName : hurtParamName;

        // ✅ 修复：如果参数名不存在，尝试自动检测
        if (!HasParameter(paramToUse))
        {
            // 尝试自动检测受击参数
            DetectHurtParameter();
            paramToUse = !string.IsNullOrEmpty(actualHurtParamName) ? actualHurtParamName : hurtParamName;
        }

        if (HasParameter(paramToUse))
        {
            // ✅ 修复：确保重置触发器后再设置，避免触发器状态问题
            anim.ResetTrigger(paramToUse);
            anim.SetTrigger(paramToUse);
            isHurting = true;
            hurtTimer = 0f;
            currentAnimationState = "Hurt";
            Debug.Log($"🎬 触发受击动画，使用参数: {paramToUse}");
        }
        else
        {
            Debug.LogWarning($"⚠️ 无法触发受击动画: 参数 '{paramToUse}' 不存在");
            Debug.LogWarning($"   配置的参数名: {hurtParamName}");
            Debug.LogWarning($"   检测到的参数名: {actualHurtParamName ?? "未检测到"}");
        }
    }

    // ✅ 新增：安全的动画参数初始化
    void InitializeAnimationParameters()
    {
        // 只有在参数存在时才初始化哈希值
        if (HasParameter(walkParamName))
        {
            animIsWalk = Animator.StringToHash(walkParamName);
            Debug.Log($"✅ 初始化行走参数: {walkParamName} -> {animIsWalk}");
        }
        else
        {
            Debug.LogWarning($"⚠️ 行走参数 '{walkParamName}' 不存在，跳过初始化");
        }

        if (!string.IsNullOrEmpty(actualAttackParamName) && HasParameter(actualAttackParamName))
        {
            animIsAttack = Animator.StringToHash(actualAttackParamName);
            Debug.Log($"✅ 初始化攻击参数: {actualAttackParamName} -> {animIsAttack}");
        }
        else
        {
            Debug.LogWarning($"⚠️ 攻击参数 '{actualAttackParamName}' 不存在，跳过初始化");
        }

        // ✅ 修复：使用实际检测到的受击参数名
        string hurtParamToUse = !string.IsNullOrEmpty(actualHurtParamName) ? actualHurtParamName : hurtParamName;
        if (HasParameter(hurtParamToUse))
        {
            animIsHurt = Animator.StringToHash(hurtParamToUse);
            Debug.Log($"✅ 初始化受伤参数: {hurtParamToUse} -> {animIsHurt}");
        }
        else
        {
            Debug.LogWarning($"⚠️ 受伤参数 '{hurtParamToUse}' 不存在，跳过初始化");
        }

        if (HasParameter(deadParamName))
        {
            animIsDead = Animator.StringToHash(deadParamName);
            Debug.Log($"✅ 初始化死亡参数: {deadParamName} -> {animIsDead}");
        }
        else
        {
            Debug.LogWarning($"⚠️ 死亡参数 '{deadParamName}' 不存在，跳过初始化");
        }
    }

    // ✅ 新增：敌人死亡事件处理
    private void OnEnemyDeath()
    {
        Debug.Log("💀 EnemyAI: 接收到死亡事件，执行死亡流程");
        Die();
    }

    // ✅ 修复：检测实际的动画参数（包括受击参数）
    void DetectAnimationParameters()
    {
        if (anim != null && anim.runtimeAnimatorController != null)
        {
            Debug.Log("🎭 检测Animator参数:");

            // 查找攻击相关的参数
            List<string> possibleAttackParams = new List<string>();
            List<string> allParams = new List<string>();

            foreach (AnimatorControllerParameter param in anim.parameters)
            {
                Debug.Log($"   - {param.name} (类型: {param.type})");
                allParams.Add(param.name);

                // 收集可能的攻击参数（放宽条件）
                if (param.type == AnimatorControllerParameterType.Trigger)
                {
                    if (param.name.ToLower().Contains("attack") ||
                        param.name.ToLower().Contains("atk") ||
                        param.name == "Attack")
                    {
                        possibleAttackParams.Add(param.name);
                    }
                }
                else if (param.type == AnimatorControllerParameterType.Bool)
                {
                    if (param.name.ToLower().Contains("attack") ||
                        param.name.ToLower().Contains("atk"))
                    {
                        possibleAttackParams.Add(param.name);
                    }
                }
            }

            // ✅ 新增：检测受击参数
            DetectHurtParameter();

            // 确定使用的攻击参数
            if (possibleAttackParams.Count > 0)
            {
                actualAttackParamName = possibleAttackParams[0];
                Debug.Log($"✅ 使用攻击参数: {actualAttackParamName}");
            }
            else if (allParams.Count > 0)
            {
                // 如果没有找到攻击参数，尝试使用第一个触发器
                foreach (AnimatorControllerParameter param in anim.parameters)
                {
                    if (param.type == AnimatorControllerParameterType.Trigger)
                    {
                        actualAttackParamName = param.name;
                        Debug.Log($"⚠️ 未找到攻击参数，使用第一个触发器: {actualAttackParamName}");
                        break;
                    }
                }

                if (string.IsNullOrEmpty(actualAttackParamName))
                {
                    // 如果连触发器都没有，使用默认名称
                    actualAttackParamName = attackParamName;
                    Debug.LogWarning($"⚠️ 未找到任何触发器参数，使用默认: {actualAttackParamName}");
                }
            }
            else
            {
                // 如果没有任何参数，使用默认名称
                actualAttackParamName = attackParamName;
                Debug.LogWarning($"⚠️ Animator没有任何参数，使用默认: {actualAttackParamName}");
            }
        }
        else
        {
            actualAttackParamName = attackParamName;
            if (anim == null)
            {
                Debug.LogError("❌ Animator组件为空！");
            }
            else if (anim.runtimeAnimatorController == null)
            {
                Debug.LogError("❌ Animator没有分配Runtime Animator Controller！");
            }
        }
    }

    // ✅ 新增：检测受击参数
    void DetectHurtParameter()
    {
        if (anim == null || anim.runtimeAnimatorController == null) return;

        List<string> possibleHurtParams = new List<string>();

        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            // 查找受击相关的触发器参数
            if (param.type == AnimatorControllerParameterType.Trigger)
            {
                string lowerName = param.name.ToLower();
                if (lowerName.Contains("hurt") ||
                    lowerName.Contains("hit") ||
                    lowerName.Contains("damage") ||
                    param.name == "isHurt" ||
                    param.name == "Hurt" ||
                    param.name == "hurt")
                {
                    possibleHurtParams.Add(param.name);
                }
            }
            // 也检查 Bool 类型的受击参数（虽然通常用 Trigger）
            else if (param.type == AnimatorControllerParameterType.Bool)
            {
                string lowerName = param.name.ToLower();
                if (lowerName.Contains("hurt") ||
                    lowerName.Contains("hit") ||
                    lowerName == "ishurt")
                {
                    possibleHurtParams.Add(param.name);
                }
            }
        }

        // 确定使用的受击参数
        if (possibleHurtParams.Count > 0)
        {
            // 优先使用包含 "isHurt" 的参数
            string preferred = possibleHurtParams.Find(p => p.ToLower() == "ishurt");
            actualHurtParamName = preferred ?? possibleHurtParams[0];
            Debug.Log($"✅ 检测到受击参数: {actualHurtParamName} (从 {possibleHurtParams.Count} 个候选中选择)");
        }
        else
        {
            // 如果没有找到，使用配置的默认值
            actualHurtParamName = hurtParamName;
            Debug.LogWarning($"⚠️ 未找到受击参数，使用配置值: {hurtParamName}");
        }
    }

    // 检查参数是否存在
    private bool HasParameter(string paramName)
    {
        if (anim == null) return false;

        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            if (param.name == paramName)
                return true;
        }
        return false;
    }

    private void Update()
    {
        // ✅ 新增：检查敌人是否已死亡
        if (isDead)
        {
            return;
        }

        // ✅ 修复：确保 player 引用不为空
        if (player == null)
        {
            FindAndSetupPlayer();
        }

        // ✅ 新增：更新受击状态计时器
        if (isHurting)
        {
            hurtTimer += Time.deltaTime;
            if (hurtTimer >= hurtAnimationDuration)
            {
                isHurting = false;
                hurtTimer = 0f;

                // ✅ 修复：受击结束后强制更新动画状态
                ForceUpdateAnimationState();

                // ✅ 新增：强制播放空闲动画作为回退
                if (anim != null && !string.IsNullOrEmpty(attackAnimationName))
                {
                    // 尝试播放空闲状态
                    anim.Play("Idle", 0, 0f);
                    currentAnimationState = "Idle";
                }
            }
        }

        if (isAttacking || isKnockedBack || isHurting) return;

        // ✅ 修复：如果 player 仍然为空，跳过检测
        if (player == null)
        {
            isChasing = false;
            return;
        }

        // ✅ 修复：使用改进的玩家检测（包含视线检测）
        bool playerDetected = CanDetectPlayer();

        if (playerDetected && !isChasing)
        {
            isChasing = true;
        }
        else if (!playerDetected && isChasing)
        {
            isChasing = false;
        }

        if (playerDetected && player != null)
        {
            // ✅ 修复：使用PlayerColliderCenter获取玩家碰撞框中心位置
            float xDiff = PlayerColliderCenter.x - transform.position.x;
            bool playerOnRight = xDiff > 0;
            // ✅ 修复：统一转向逻辑，与ChasePlayer保持一致
            if (Mathf.Abs(xDiff) > flipThreshold && playerOnRight != facingRight && Time.time >= lastFlipTime + flipCooldown)
            {
                Flip(playerOnRight);
                lastFlipTime = Time.time;
            }
        }

        // 确保攻击条件正确判断
        // ✅ 修复：攻击条件也添加视线检测
        if (player != null && IsPlayerInAttackRange() && HasLineOfSightToPlayer() && !isAttacking && !attackAnimationPlaying && !isHurting)
        {
            StartCoroutine(AttackPlayer());
        }

        // 监控攻击动画状态
        if (attackAnimationPlaying)
        {
            attackAnimationTime += Time.deltaTime;
            // 如果动画播放时间超过攻击延迟的1.5倍，强制重置状态（防止动画卡住）
            if (attackAnimationTime > attackDelay * 1.5f)
            {
                attackAnimationPlaying = false;
                isAttacking = false;
            }
        }

        // 每120帧监控一次动画状态（避免日志过多）
        if (Time.frameCount % 120 == 0)
        {
            MonitorAnimationState();
        }
    }

    private bool isFrozen = false;

    public void SetFrozen(bool frozen)
    {
        isFrozen = frozen;
        if (frozen)
        {
            // 立即停止移动
            if (rb != null) rb.velocity = Vector2.zero;
        }
    }

    private void FixedUpdate()
    {
        // 新增冻结检查
        if (isFrozen)
        {
            if (rb != null) rb.velocity = Vector2.zero;
            return;
        }

        // ✅ 新增：检查敌人是否已死亡
        if (enemyAttributes != null && !enemyAttributes.IsAlive)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        // ✅ 修改：添加受击状态检查
        if (isAttacking || attackAnimationPlaying || isHurting)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        if (isKnockedBack)
            return;

        // 只在巡逻状态下使用基础墙体检测
        if (!isChasing)
        {
            CheckWallForPatrol();
        }

        if (isChasing && player != null)
        {
            ChasePlayer();
        }
        else
        {
            // ===== 修改：根据AB点巡逻设置选择巡逻方式 =====
            if (useABPatrol)
            {
                ABPatrol(); // 使用AB点巡逻
            }
            else
            {
                Patrol(); // 使用原有巡逻逻辑
            }
        }

        // 更新动画状态
        UpdateAnimationState();

        // 防止嵌入墙体的安全检测
        if (!isKnockedBack && !isAttacking && !isHurting)
        {
            CheckAndFixWallEmbedding();
        }
    }

    void Patrol()
    {
        float moveDir = facingRight ? 1f : -1f;
        rb.velocity = new Vector2(moveDir * moveSpeed, rb.velocity.y);
    }

    void ChasePlayer()
    {
        if (player == null) return;

        // ✅ 修复：在追击前检查视线，如果视线被阻挡则停止追击
        if (!HasLineOfSightToPlayer())
        {
            // 视线被阻挡，停止移动
            rb.velocity = new Vector2(0, rb.velocity.y);
            isChasing = false; // 可选：立即停止追击
            return;
        }

        // ... 原有的追击逻辑保持不变
        float xDiff = PlayerColliderCenter.x - transform.position.x;

        // ✅ 修复：先检查是否需要转向，然后再进行其他判断
        bool justFlipped = false;
        if (Mathf.Abs(xDiff) > flipThreshold && Time.time >= lastFlipTime + flipCooldown)
        {
            bool shouldFaceRight = xDiff > 0;
            if (shouldFaceRight != facingRight)
            {
                Flip(shouldFaceRight);
                lastFlipTime = Time.time;
                justFlipped = true;
            }
        }

        // ✅ 修复：如果距离玩家很近，停止移动准备攻击
        // 但是，如果刚刚转向，给一点缓冲时间，避免立即停止
        if (Mathf.Abs(xDiff) < attackRange * 1.2f && !justFlipped)
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
            return;
        }

        // 改进：检查前方是否有墙体阻挡
        Transform checkPoint = facingRight ? wallCheckRight : wallCheckLeft;
        if (checkPoint == null)
        {
            // 如果检测点为空，直接移动（避免空引用错误）
            float moveDir = Mathf.Sign(xDiff);
            rb.velocity = new Vector2(moveDir * chaseSpeed, rb.velocity.y);
            return;
        }

        Vector2 dir = facingRight ? Vector2.right : Vector2.left;

        RaycastHit2D wallHit = Physics2D.Raycast(checkPoint.position, dir, wallCheckDistance, wallLayer);
        bool blocked = wallHit.collider != null;

        if (blocked)
        {
            // ✅ 修复：墙体阻挡处理
            // 先检查玩家方向
            float playerDir = Mathf.Sign(xDiff);
            float facingDir = facingRight ? 1f : -1f;

            // 如果玩家方向和敌人朝向一致，说明玩家在前方但有墙体阻挡
            if (Mathf.Sign(playerDir) == Mathf.Sign(facingDir))
            {
                // 玩家确实在前方，但检测到墙体，尝试绕过
                HandleChaseWallCollision();
            }
            else
            {
                // 玩家在后方，转向继续追击
                Flip(!facingRight);
                lastFlipTime = Time.time;
                // ✅ 修复：转向后立即设置移动方向，避免静止
                float moveDir = Mathf.Sign(xDiff);
                rb.velocity = new Vector2(moveDir * chaseSpeed, rb.velocity.y);
            }
        }
        else
        {
            // ✅ 修复：没有墙体阻挡，直接移动
            float moveDir = Mathf.Sign(xDiff);
            rb.velocity = new Vector2(moveDir * chaseSpeed, rb.velocity.y);
        }
    }

    // 专门用于巡逻的墙体检测
    void CheckWallForPatrol()
    {
        // ===== 修改：AB点巡逻有自己的墙体检测，原有巡逻逻辑只用于非AB点巡逻 =====
        if (useABPatrol) return;

        Transform checkPoint = facingRight ? wallCheckRight : wallCheckLeft;
        if (checkPoint == null) return;

        Vector2 dir = facingRight ? Vector2.right : Vector2.left;
        RaycastHit2D hit = Physics2D.Raycast(checkPoint.position, dir, wallCheckDistance, wallLayer);

        if (hit.collider != null && Time.time >= lastFlipTime + flipCooldown)
        {
            Flip(!facingRight);
            lastFlipTime = Time.time;
        }
    }

    // 专门用于追击的墙体检测
    void CheckWallForChasing()
    {
        Transform checkPoint = facingRight ? wallCheckRight : wallCheckLeft;
        if (checkPoint == null) return;

        Vector2 dir = facingRight ? Vector2.right : Vector2.left;
        RaycastHit2D hit = Physics2D.Raycast(checkPoint.position, dir, wallCheckDistance, wallLayer);

        if (hit.collider != null)
        {
            hitWall = true;
            HandleChaseWallCollision();
        }
        else
        {
            hitWall = false;
        }
    }

    // 追击状态下的墙体碰撞处理
    void HandleChaseWallCollision()
    {
        if (isAttacking || isKnockedBack || isHurting) return;
        if (Time.time < lastFlipTime + flipCooldown) return;

        // 检查另一个方向是否可行
        bool alternativeDirection = !facingRight;
        Transform altCheckPoint = alternativeDirection ? wallCheckRight : wallCheckLeft;
        Vector2 altDir = alternativeDirection ? Vector2.right : Vector2.left;

        RaycastHit2D altHit = Physics2D.Raycast(altCheckPoint.position, altDir, wallCheckDistance * 2f, wallLayer);

        if (altHit.collider == null)
        {
            // 另一个方向没有墙体，转向
            Flip(alternativeDirection);
            lastFlipTime = Time.time;
        }
        else
        {
            // 两个方向都有墙体，可能是死胡同，停止移动
            rb.velocity = new Vector2(0, rb.velocity.y);

            // 尝试跳转或其他逃脱逻辑
            TryEscapeFromDeadEnd();
        }

        hitWall = false;
    }

    // 尝试从死胡同逃脱
    void TryEscapeFromDeadEnd()
    {
        StartCoroutine(EscapeCoroutine());
    }

    private IEnumerator EscapeCoroutine()
    {
        yield return new WaitForSeconds(1f);

        // 强制转向并尝试移动
        Flip(!facingRight);
        lastFlipTime = Time.time;
    }

    // 使用子对象的transform.localScale进行翻转
    void Flip(bool faceRight)
    {
        facingRight = faceRight;
        ApplyFacingDirection();
    }

    // 统一应用朝向的方法，针对子对象
    void ApplyFacingDirection()
    {
        Transform targetTransform = sprite != null ? sprite.transform : transform;

        if (facingRight)
        {
            targetTransform.localScale = new Vector3(
                Mathf.Abs(originalScale.x),
                originalScale.y,
                originalScale.z
            );
        }
        else
        {
            targetTransform.localScale = new Vector3(
                -Mathf.Abs(originalScale.x),
                originalScale.y,
                originalScale.z
            );
        }
    }

    // 更新动画状态
    void UpdateAnimationState()
    {
        if (anim == null) return;

        // 设置行走状态 - 排除受击状态
        bool isMoving = Mathf.Abs(rb.velocity.x) > 0.1f && !isAttacking && !attackAnimationPlaying && !isKnockedBack && !isHurting;

        // 修复：只有在参数存在时才设置
        if (HasParameter(walkParamName))
        {
            anim.SetBool(animIsWalk, isMoving);

            // ✅ 新增：更新当前动画状态
            if (isMoving)
            {
                currentAnimationState = "Walk";
            }
            else if (!isAttacking && !attackAnimationPlaying && !isHurting && !isKnockedBack)
            {
                currentAnimationState = "Idle";
            }
        }

        // 如果正在攻击或受击，确保行走状态为false
        if ((isAttacking || attackAnimationPlaying || isHurting) && HasParameter(walkParamName))
        {
            anim.SetBool(animIsWalk, false);
        }
    }

    // ✅ 新增：强制更新动画状态的方法
    void ForceUpdateAnimationState()
    {
        if (anim == null) return;

        // 重置所有可能卡住的动画状态
        if (HasParameter(walkParamName))
        {
            bool shouldWalk = Mathf.Abs(rb.velocity.x) > 0.1f && !isAttacking && !attackAnimationPlaying && !isKnockedBack && !isHurting;
            anim.SetBool(walkParamName, shouldWalk);

            // ✅ 新增：更新当前状态
            if (shouldWalk)
            {
                currentAnimationState = "Walk";
            }
            else
            {
                currentAnimationState = "Idle";
            }
        }

        // ✅ 修复：如果使用触发器，确保重置相关状态（使用实际参数名）
        string hurtParamToUse = !string.IsNullOrEmpty(actualHurtParamName) ? actualHurtParamName : hurtParamName;
        if (HasParameter(hurtParamToUse))
        {
            // 确保受击触发器被重置
            anim.ResetTrigger(hurtParamToUse);
        }
    }

    // ✅ 新增：动画状态监控方法
    private void MonitorAnimationState()
    {
        if (anim == null) return;

        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        string stateName = GetAnimationStateName(stateInfo);

        // ✅ 新增：检查是否卡在受击状态
        if (stateName == "Hurt" && stateInfo.normalizedTime >= 1.0f && !isHurting)
        {
            ForceExitHurtState();
        }
    }

    // ✅ 新增：获取动画状态名称
    private string GetAnimationStateName(AnimatorStateInfo stateInfo)
    {
        if (stateInfo.IsName("Walk")) return "Walk";
        else if (stateInfo.IsName("Idle")) return "Idle";
        else if (stateInfo.IsName("Hurt")) return "Hurt";
        else if (stateInfo.IsName("Attack")) return "Attack";
        else if (stateInfo.IsName("Death")) return "Death";
        else return stateInfo.ToString();
    }

    // ✅ 新增：强制退出受击状态
    private void ForceExitHurtState()
    {
        if (anim == null) return;

        // 强制播放空闲动画
        anim.Play("Idle", 0, 0f);
        currentAnimationState = "Idle";
        isHurting = false;
        hurtTimer = 0f;
    }

    // 改进：多种攻击动画触发方式
    IEnumerator AttackPlayer()
    {
        isAttacking = true;
        attackAnimationPlaying = true;
        attackAnimationTime = 0f;
        rb.velocity = Vector2.zero;

        // 详细的Animator状态检查
        if (anim == null)
        {
            Debug.LogError("❌ Animator组件为空！");
            yield break;
        }

        if (!anim.enabled)
        {
            Debug.LogError("❌ Animator组件未启用！");
            yield break;
        }

        if (!anim.isInitialized)
        {
            Debug.LogError("❌ Animator未初始化！");
            yield break;
        }

        // ✅ 修复：使用安全的动画触发方式
        bool animationTriggered = SafeTriggerAttackAnimation();

        if (!animationTriggered)
        {
            // 方法2: 直接播放动画状态
            yield return StartCoroutine(PlayAttackAnimationDirectly());
        }
        else
        {
            // 方法1成功，等待动画播放
            yield return new WaitForSeconds(attackDelay / 2f);

            // ✅ 修复：在攻击延迟一半时造成伤害
            if (IsPlayerInAttackRange())
            {
                DamageAtAttack(); // 使用新的伤害检测（按攻击模式）
            }

            yield return new WaitForSeconds(attackDelay / 2f);
        }

        // 重置状态
        isAttacking = false;
        attackAnimationPlaying = false;

        // ✅ 新增：攻击结束后强制更新动画状态
        ForceUpdateAnimationState();
    }

    // ✅ 修复：安全的动画触发方法
    bool SafeTriggerAttackAnimation()
    {
        if (anim == null || !anim.enabled || !anim.isInitialized)
        {
            Debug.LogError("❌ Animator不可用");
            return false;
        }

        try
        {
            // ✅ 修复：检查参数是否存在，如果不存在则使用直接播放方式
            if (string.IsNullOrEmpty(actualAttackParamName) || !HasParameter(actualAttackParamName))
            {
                return false;
            }

            // ✅ 修复：使用字符串名称而不是哈希值，避免哈希值错误
            anim.ResetTrigger(actualAttackParamName);
            anim.SetTrigger(actualAttackParamName);
            currentAnimationState = "Attack";

            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ 触发器方式失败: {e.Message}");
            return false;
        }
    }

    // 方法2 - 直接播放动画状态
    IEnumerator PlayAttackAnimationDirectly()
    {
        // 尝试直接播放攻击动画状态
        anim.Play(attackAnimationName, 0, 0f);
        currentAnimationState = "Attack";

        // 等待一帧检查状态
        yield return null;

        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);

        if (stateInfo.IsName(attackAnimationName) || stateInfo.length > 0)
        {
            // 等待动画播放一段时间后造成伤害
            yield return new WaitForSeconds(attackDelay / 2f);

            if (IsPlayerInAttackRange())
            {
                DamageAtAttack();
            }

            // 等待动画剩余时间
            float remainingTime = Mathf.Max(0.1f, attackDelay / 2f);
            yield return new WaitForSeconds(remainingTime);
        }
        else
        {
            yield return new WaitForSeconds(attackDelay / 2f);

            if (IsPlayerInAttackRange())
            {
                DamageAtAttack();
            }

            yield return new WaitForSeconds(attackDelay / 2f);
        }
    }

    // ===== 新增：根据当前攻击模式在攻击命中时造成伤害 =====
    void DamageAtAttack()
    {
        // 优先使用 attackPoint，如果未设置使用 transform
        Vector2 origin = (attackPoint != null) ? (Vector2)attackPoint.position : (Vector2)transform.position;

        if (attackMode == AttackMode.Circle)
        {
            Vector2 circleCenter = origin + circleOffset;
            Collider2D[] hits = Physics2D.OverlapCircleAll(circleCenter, attackRange, playerLayer);
            if (hits != null && hits.Length > 0)
            {
                foreach (Collider2D c in hits)
                {
                    if (c == null) continue;
                    Attribute attr = c.GetComponent<Attribute>() ?? c.GetComponentInParent<Attribute>() ?? c.GetComponentInChildren<Attribute>(true);
                    if (attr != null)
                    {
                        attr.TakeDamage(attackDamage, gameObject);
                    }
                }
            }
        }
        else // Box 模式
        {
            // ✅ 修复：使用当前Box偏移和角度（考虑翻转）
            Vector2 boxCenter = (attackPoint != null) ? (Vector2)attackPoint.position + CurrentBoxOffset : (Vector2)transform.position + CurrentBoxOffset;
            Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, boxSize, CurrentBoxAngle, playerLayer);
            if (hits != null && hits.Length > 0)
            {
                foreach (Collider2D c in hits)
                {
                    if (c == null) continue;
                    Attribute attr = c.GetComponent<Attribute>() ?? c.GetComponentInParent<Attribute>() ?? c.GetComponentInChildren<Attribute>(true);
                    if (attr != null)
                    {
                        attr.TakeDamage(attackDamage, gameObject);
                    }
                }
            }
        }
    }

    // ✅ 修复：安全的伤害处理方法 - 使用玩家根对象
    void SafeDealDamage()
    {
        // 兼容旧逻辑：如果你之前希望直接打 playerRoot 的 Attribute（单玩家），保留这段
        // 但在我们新的 DamageAtAttack 中会按攻击模式对所有命中对象造成伤害。
        // 这里我们优先调用 DamageAtAttack()（按新系统处理），并保留老方法作后备。
        DamageAtAttack();

        // 如果没有找到任何 Attribute，也尝试原有单一玩家伤害逻辑（以防只想伤害单个 playerRoot）
        if (playerAttributes == null)
        {
            FindPlayerRootAndAttributes();
        }

        if (playerRoot == null)
        {
            // 已经在 DamageAtAttack 中处理过，提前返回
            return;
        }

        if (playerAttributes != null)
        {
            // 如果你仍然希望在没有 overlap 检测时伤害 playerRoot（兼容老逻辑）
            playerAttributes.TakeDamage(attackDamage, gameObject);
        }
    }

    // 统一的伤害处理（保持原有方法，但内部调用安全版本）
    void DealDamage()
    {
        SafeDealDamage();
    }

    // 动画事件方法
    public void OnAttackAnimationStart()
    {
        attackAnimationPlaying = true;
        currentAnimationState = "Attack";
    }

    public void OnAttackAnimationEnd()
    {
        attackAnimationPlaying = false;
        isAttacking = false;

        // ✅ 新增：攻击结束后强制更新动画状态
        ForceUpdateAnimationState();
    }

    // ✅ 新增：受击动画事件方法
    public void OnHurtAnimationStart()
    {
        isHurting = true;
        currentAnimationState = "Hurt";
    }

    public void OnHurtAnimationEnd()
    {
        isHurting = false;
        hurtTimer = 0f;

        // ✅ 修复：受击动画结束后立即更新动画状态
        ForceUpdateAnimationState();

        // ✅ 新增：如果使用 Animator，确保状态正确重置
        if (anim != null)
        {
            // 强制 Animator 重新评估状态
            anim.Update(0f);
        }
    }

    // 攻击伤害触发点
    public void OnAttackHit()
    {
        DamageAtAttack(); // 使用统一的新函数
    }

    bool IsPlayerInDetectionRange()
    {
        if (player == null) return false;

        // ✅ 修复：使用敌人的根对象位置和玩家的碰撞框中心位置进行检测
        Vector2 offset = PlayerColliderCenter - (Vector2)transform.position;
        float ellipseValue =
            (offset.x * offset.x) / (detectionWidth * detectionWidth / 4f) +
            (offset.y * offset.y) / (detectionHeight * detectionHeight / 4f);

        return ellipseValue <= 1f;
    }

    bool IsPlayerInAttackRange()
    {
        if (player == null) return false;

        // 根据当前攻击模式判断是否有玩家处于攻击范围（用于决定何时开始攻击）
        Vector2 origin = (attackPoint != null) ? (Vector2)attackPoint.position : (Vector2)transform.position;

        if (attackMode == AttackMode.Circle)
        {
            Vector2 circleCenter = origin + circleOffset;
            Collider2D[] hits = Physics2D.OverlapCircleAll(circleCenter, attackRange, playerLayer);
            return hits != null && hits.Length > 0;
        }
        else // Box 模式
        {
            // ✅ 修复：使用当前Box偏移和角度（考虑翻转）
            Vector2 boxCenter = origin + CurrentBoxOffset;
            Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, boxSize, CurrentBoxAngle, playerLayer);
            return hits != null && hits.Length > 0;
        }
    }

    // ✅ 新增：视线检测方法
    private bool HasLineOfSightToPlayer()
    {
        if (player == null) return false;

        // 使用玩家的碰撞框中心作为目标点
        Vector2 targetPos = PlayerColliderCenter;

        // 确定视线检测的起点
        Vector2 startPos = sightCheckPoint != null ? (Vector2)sightCheckPoint.position : (Vector2)transform.position;

        // 计算到玩家的方向
        Vector2 direction = (targetPos - startPos).normalized;
        float distance = Vector2.Distance(startPos, targetPos);

        // 进行射线检测
        RaycastHit2D hit = Physics2D.Raycast(startPos, direction, distance, sightBlockingLayers);

        // 调试绘制射线（只在需要时绘制）
        if (drawSightGizmos)
        {
            Debug.DrawRay(startPos, direction * distance, hit.collider == null ? Color.green : Color.red, 0.1f);
        }

        // 如果没有击中任何东西，说明视线畅通
        if (hit.collider == null)
        {
            return true;
        }

        // 如果击中了玩家，说明视线畅通（射线可能先击中玩家）
        if (hit.collider.CompareTag("Player") || ((1 << hit.collider.gameObject.layer) & playerLayer) != 0)
        {
            return true;
        }

        // 击中了墙体或其他障碍物
        return false;
    }

    // ✅ 新增：改进的玩家检测方法（包含视线检测）
    private bool CanDetectPlayer()
    {
        if (player == null) return false;

        // 首先检查玩家是否在检测范围内
        if (!IsPlayerInDetectionRange())
            return false;

        // 然后检查是否有视线
        // 根据设置选择使用基础或高级视线检测
        return useAdvancedSightCheck ? HasLineOfSightToPlayerAdvanced() : HasLineOfSightToPlayer();
    }

    // ✅ 新增：多角度视线检测（更精确）
    private bool HasLineOfSightToPlayerAdvanced()
    {
        if (player == null) return false;

        Vector2 targetPos = PlayerColliderCenter;
        Vector2 startPos = sightCheckPoint != null ? (Vector2)sightCheckPoint.position : (Vector2)transform.position;

        // 使用多个检测点提高准确性
        Vector2[] checkPoints = GetSightCheckPoints(startPos);
        int validHits = 0;

        foreach (Vector2 checkPoint in checkPoints)
        {
            Vector2 direction = (targetPos - checkPoint).normalized;
            float distance = Vector2.Distance(checkPoint, targetPos);

            RaycastHit2D hit = Physics2D.Raycast(checkPoint, direction, distance, sightBlockingLayers);

            // 调试绘制（只在需要时绘制）
            if (drawSightGizmos)
            {
                Debug.DrawRay(checkPoint, direction * distance, hit.collider == null ? Color.green : Color.red, 0.1f);
            }

            if (hit.collider == null ||
                hit.collider.CompareTag("Player") ||
                ((1 << hit.collider.gameObject.layer) & playerLayer) != 0)
            {
                validHits++;
            }
        }

        // 如果超过一半的检测点有视线，则认为有视线
        return validHits >= checkPoints.Length / 2;
    }

    // ✅ 新增：获取多个视线检测点
    private Vector2[] GetSightCheckPoints(Vector2 basePoint)
    {
        return new Vector2[]
        {
            basePoint,                                   // 中心点
            basePoint + Vector2.up * 0.5f,              // 上方点
            basePoint + Vector2.down * 0.3f,            // 下方点
            basePoint + Vector2.up * 0.25f,             // 中上点
            basePoint + Vector2.down * 0.15f            // 中下点
        };
    }

    // 原有的方法（保持布尔参数）
    public void ApplyWindKnockback(float force, bool fromRight)
    {
        if (isKnockedBack) return;

        // ✅ 修复：将布尔方向转换为数值方向
        // fromRight=true 表示力来自右边，敌人应该被击退到左边（负方向）
        // fromRight=false 表示力来自左边，敌人应该被击退到右边（正方向）
        float direction = fromRight ? -1f : 1f;
        StartCoroutine(KnockbackCoroutine(force, direction));
    }

    // 新的方法（直接使用数值方向）
    public void ApplyWindKnockbackWithDirection(float force, float direction)
    {
        if (isKnockedBack) return;
        StartCoroutine(KnockbackCoroutine(force, direction));
    }

    // 统一的协程方法
    private IEnumerator KnockbackCoroutine(float force, float direction)
    {
        isKnockedBack = true;
        isAttacking = false;
        attackAnimationPlaying = false;
        isChasing = false;

        // ✅ 修复：触发受伤动画（使用实际检测到的参数名）
        if (anim != null)
        {
            string hurtParamToUse = !string.IsNullOrEmpty(actualHurtParamName) ? actualHurtParamName : hurtParamName;
            if (HasParameter(hurtParamToUse))
            {
                anim.ResetTrigger(hurtParamToUse);
                anim.SetTrigger(hurtParamToUse);
                currentAnimationState = "Hurt";
            }
        }

        float dir = Mathf.Clamp(direction, -1f, 1f); // 确保在 -1 到 1 之间
        float elapsed = 0f;

        float knockbackSpeed = force / windKnockbackDuration;

        Vector3 startPosition = transform.position;

        while (elapsed < windKnockbackDuration)
        {
            elapsed += Time.deltaTime;

            float moveStep = knockbackSpeed * Time.deltaTime;

            // 检查是否会撞墙
            bool willHitWall = CheckWallInKnockbackDirection(dir, moveStep);

            if (willHitWall)
            {
                break;
            }

            // 执行移动
            transform.position += new Vector3(dir * moveStep, 0, 0);

            yield return null;
        }

        // 确保最终停止
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }

        isKnockedBack = false;

        // ✅ 修复：击退结束后强制更新动画状态
        ForceUpdateAnimationState();
    }

    // ✅ 改进：死亡方法，与Attribute系统集成
    public void Die()
    {
        // ✅ 修复：防止重复调用Die()
        if (isDead)
        {
            return;
        }

        isDead = true;

        // 停止所有行为
        isAttacking = false;
        attackAnimationPlaying = false;
        isChasing = false;
        isKnockedBack = false;
        isHurting = false;

        // 停止移动
        rb.velocity = Vector2.zero;

        // 设置死亡状态
        if (anim != null && HasParameter(deadParamName))
        {
            // ✅ 修复：使用字符串名称而不是哈希值
            anim.SetBool(deadParamName, true);
            currentAnimationState = "Death";
        }
        else if (anim != null)
        {
            // 如果没有死亡参数，尝试播放死亡动画
            anim.Play("Death", 0, 0f);
            currentAnimationState = "Death";
        }

        // 禁用碰撞器
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
            collider.enabled = false;

        // 禁用脚本
        enabled = false;

        // 可选：在动画播放后销毁对象
        StartCoroutine(DestroyAfterDeath());
    }

    // 可选：死亡动画播放后销毁对象
    private IEnumerator DestroyAfterDeath()
    {
        // 等待死亡动画播放完毕
        yield return new WaitForSeconds(2f);
        Destroy(gameObject);
    }

    // 改进的墙体检测方法（多检测点）
    private bool CheckWallInKnockbackDirection(float direction, float distance)
    {
        if (wallCheckLeft == null || wallCheckRight == null) return false;

        // 使用多个检测点提高检测精度
        Vector2[] checkPoints = GetKnockbackCheckPoints(direction);
        Vector2 rayDir = new Vector2(direction, 0f);

        foreach (Vector2 point in checkPoints)
        {
            RaycastHit2D hit = Physics2D.Raycast(point, rayDir, distance + 0.1f, wallLayer);
            if (hit.collider != null)
            {
                return true;
            }
        }

        return false;
    }

    // 获取击退检测点数组
    private Vector2[] GetKnockbackCheckPoints(float direction)
    {
        Transform primaryCheck = direction > 0 ? wallCheckRight : wallCheckLeft;
        Vector2 basePoint = primaryCheck.position;

        // 在垂直方向上创建多个检测点
        return new Vector2[]
        {
            basePoint,
            basePoint + Vector2.up * 0.5f,    // 上方检测点
            basePoint + Vector2.down * 0.5f,  // 下方检测点
            basePoint + Vector2.up * 0.25f,   // 中上检测点
            basePoint + Vector2.down * 0.25f  // 中下检测点
        };
    }

    // 找到安全的击退距离
    private float FindSafeKnockbackDistance(float direction, float maxDistance)
    {
        if (wallCheckLeft == null || wallCheckRight == null) return 0f;

        Transform primaryCheck = direction > 0 ? wallCheckRight : wallCheckLeft;
        Vector2 rayDir = new Vector2(direction, 0f);

        // 找到最近的墙体距离
        float minDistance = maxDistance;
        Vector2[] checkPoints = GetKnockbackCheckPoints(direction);

        foreach (Vector2 point in checkPoints)
        {
            RaycastHit2D hit = Physics2D.Raycast(point, rayDir, maxDistance + 0.1f, wallLayer);
            if (hit.collider != null && hit.distance < minDistance)
            {
                minDistance = hit.distance;
            }
        }

        // 返回安全距离（留出0.05f的缓冲）
        return Mathf.Max(0, minDistance - 0.05f);
    }

    // 检测并修复嵌入墙体的情况
    private void CheckAndFixWallEmbedding()
    {
        Collider2D[] overlappingWalls = Physics2D.OverlapCircleAll(transform.position, 0.3f, wallLayer);
        if (overlappingWalls.Length > 0)
        {
            // 尝试向相反方向移动来脱离墙体
            Vector2 escapeDirection = facingRight ? Vector2.left : Vector2.right;
            RaycastHit2D hit = Physics2D.Raycast(transform.position, escapeDirection, 2f, ~wallLayer);

            if (hit.collider != null && ((1 << hit.collider.gameObject.layer) & wallLayer) == 0)
            {
                // 找到安全位置，移动过去
                transform.position = hit.point - (Vector2)escapeDirection * 0.1f;
            }
        }
    }

    [ContextMenu("测试攻击")]
    private void TestAttack()
    {
        if (!isAttacking && !attackAnimationPlaying)
        {
            StartCoroutine(AttackPlayer());
        }
    }

    [ContextMenu("测试受击")]
    private void TestHurt()
    {
        TriggerHurtAnimation();
    }

    [ContextMenu("测试向左击退")]
    private void TestKnockbackLeft()
    {
        // 使用新的方法，传递 float 方向值
        ApplyWindKnockbackWithDirection(5f, -1f);
    }

    [ContextMenu("测试向右击退")]
    private void TestKnockbackRight()
    {
        // 使用新的方法，传递 float 方向值
        ApplyWindKnockbackWithDirection(5f, 1f);
    }

    [ContextMenu("测试死亡")]
    private void TestDeath()
    {
        Die();
    }

    [ContextMenu("测试转向右边")]
    private void TestFlipRight()
    {
        Flip(true);
    }

    [ContextMenu("测试转向左边")]
    private void TestFlipLeft()
    {
        Flip(false);
    }

    [ContextMenu("调试Animator参数")]
    private void DebugAnimatorParameters()
    {
        DetectAnimationParameters();
    }

    [ContextMenu("重新查找玩家")]
    private void ReFindPlayer()
    {
        FindAndSetupPlayer();
    }

    // ===== 新增：AB点巡逻测试方法 =====
    [ContextMenu("切换AB点巡逻")]
    private void ToggleABPatrol()
    {
        useABPatrol = !useABPatrol;
        if (useABPatrol)
        {
            InitializeABPatrol();
        }
        else
        {
            // 禁用AB点巡逻时重置状态
            isWaitingAtPoint = false;
            waitTimer = 0f;
            hasReachedPoint = false;
            stuckTimer = 0f;
        }
        Debug.Log($"🔄 AB点巡逻已{(useABPatrol ? "启用" : "禁用")}");
    }

    // ===== 新增：强制切换到下一个巡逻点 =====
    [ContextMenu("强制切换巡逻点")]
    private void ForceSwitchPatrolPoint()
    {
        if (useABPatrol)
        {
            SwitchPatrolTarget();
        }
        else
        {
            Debug.Log("⚠️ AB点巡逻未启用");
        }
    }

    [ContextMenu("显示敌人状态")]
    private void ShowEnemyStatus()
    {
        if (enemyAttributes != null)
        {
            Debug.Log($"=== 敌人状态 ===");
            Debug.Log($"生命值: {enemyAttributes.CurrentHealth}/{enemyAttributes.MaxHealth}");
            Debug.Log($"存活状态: {(enemyAttributes.IsAlive ? "存活" : "死亡")}");
            Debug.Log($"攻击力: {attackDamage}");
            Debug.Log($"移动状态: {(isChasing ? "追击" : "巡逻")}");
            Debug.Log($"攻击状态: {(isAttacking ? "攻击中" : "待机")}");
            Debug.Log($"受击状态: {(isHurting ? "受击中" : "正常")}");
            Debug.Log($"当前动画: {currentAnimationState}");
            Debug.Log($"攻击模式: {attackMode}");
            Debug.Log($"AB点巡逻: {(useABPatrol ? "启用" : "禁用")}");

            if (useABPatrol)
            {
                Vector2 toTarget = currentPatrolTarget.position - transform.position;
                float distanceToTarget = Mathf.Abs(toTarget.x);

                Debug.Log($"当前巡逻目标: {(currentPatrolTarget != null ? currentPatrolTarget.name : "无")}");
                Debug.Log($"到目标点距离: {distanceToTarget:F2}");
                Debug.Log($"等待状态: {(isWaitingAtPoint ? $"等待中 ({waitTimer:F1}/{waitTimeAtPoint}s)" : "移动中")}");
                Debug.Log($"已到达标记: {hasReachedPoint}");
                Debug.Log($"卡住计时器: {stuckTimer:F1}/{STUCK_TIME_THRESHOLD}s");
            }

            if (attackMode == AttackMode.Box)
            {
                Debug.Log($"Box 偏移: {boxOffset}, 大小: {boxSize}, 角度: {boxAngle}");
                Debug.Log($"当前Box偏移: {CurrentBoxOffset}, 当前Box角度: {CurrentBoxAngle}");
                Debug.Log($"Box翻转启用: {flipBoxWithEnemy}");
            }
            else
            {
                Debug.Log($"Circle 偏移: {circleOffset}, 半径: {attackRange}");
            }
        }
    }

    // ✅ 新增：改进的玩家查找和设置方法
    void FindAndSetupPlayer()
    {
        // 如果player字段为空，尝试查找玩家
        if (player == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
            else
            {
                Debug.LogError("⚠ 找不到标记为 'Player' 的对象！");
                return;
            }
        }

        // ✅ 修复：查找玩家的根对象和Attribute组件
        FindPlayerRootAndAttributes();
    }

    // ✅ 修复：查找玩家根对象和Attribute组件（支持多种对象结构）
    void FindPlayerRootAndAttributes()
    {
        if (player == null) return;

        // ✅ 改进：尝试多种方式查找根对象
        playerRoot = null;

        // 方法1: 如果当前对象名包含"Visual"，向上查找根对象
        if (player.name.Contains("Visual") || player.name.Contains("visual"))
        {
            playerRoot = player.root;
            if (playerRoot == player || playerRoot == null)
            {
                playerRoot = player.parent;
            }
            if (playerRoot != null)
            {
            }
        }

        // 方法2: 尝试查找有 PlayerController 组件的对象（通常是根对象）
        if (playerRoot == null)
        {
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc == null)
            {
                pc = player.GetComponentInParent<PlayerController>();
            }
            if (pc != null)
            {
                playerRoot = pc.transform;
            }
        }

        // 方法3: 尝试查找有 Attribute 组件的父对象或根对象
        if (playerRoot == null)
        {
            Attribute attr = player.GetComponentInParent<Attribute>();
            if (attr != null)
            {
                playerRoot = attr.transform;
            }
        }

        // 方法4: 向上查找直到找到根对象
        if (playerRoot == null)
        {
            Transform current = player;
            while (current.parent != null)
            {
                current = current.parent;
            }
            playerRoot = current;
        }

        // 如果还是没找到，使用当前对象
        if (playerRoot == null)
        {
            playerRoot = player;
        }

        // ✅ 新增：查找玩家的BoxCollider2D组件（用于精确的碰撞框检测）
        playerCollider = playerRoot.GetComponent<BoxCollider2D>();
        if (playerCollider == null)
        {
            // 如果在根对象没找到，尝试在整个玩家层次结构中查找
            playerCollider = player.GetComponentInParent<BoxCollider2D>();
            if (playerCollider == null)
            {
                playerCollider = player.GetComponentInChildren<BoxCollider2D>();
            }
        }

        // ✅ 改进：多层级查找 Attribute 组件
        playerAttributes = null;

        // 首先在根对象上查找
        playerAttributes = playerRoot.GetComponent<Attribute>();
        if (playerAttributes != null)
        {
            return;
        }

        // 在根对象的子对象中查找（包括所有子对象）
        playerAttributes = playerRoot.GetComponentInChildren<Attribute>(true);
        if (playerAttributes != null)
        {
            return;
        }

        // 尝试在整个玩家层次结构中查找（包括父对象）
        playerAttributes = player.GetComponentInParent<Attribute>();
        if (playerAttributes != null)
        {
            return;
        }

        // ✅ 新增：尝试在整个场景中查找玩家的 Attribute
        Attribute[] allAttributes = FindObjectsOfType<Attribute>();
        foreach (Attribute attr in allAttributes)
        {
            if (attr.CompareTag("Player") || attr.transform.CompareTag("Player"))
            {
                playerAttributes = attr;
                playerRoot = attr.transform;
                return;
            }
        }

        // 如果还没找到，尝试查找所有带 PlayerController 的对象
        PlayerController[] allPlayers = FindObjectsOfType<PlayerController>();
        foreach (PlayerController pc in allPlayers)
        {
            Attribute attr = pc.GetComponent<Attribute>();
            if (attr == null)
            {
                attr = pc.GetComponentInChildren<Attribute>(true);
            }
            if (attr != null)
            {
                playerAttributes = attr;
                playerRoot = pc.transform;
                return;
            }
        }
    }

    // ✅ 新增：获取对象的完整路径
    string GetFullPath(Transform t)
    {
        if (t == null) return "null";
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }

    // ✅ 新增：重置动画状态的方法
    [ContextMenu("重置动画状态")]
    private void ResetAnimationState()
    {
        if (anim == null) return;

        // 重置所有布尔参数
        if (HasParameter(walkParamName))
            anim.SetBool(walkParamName, false);

        // ✅ 修复：重置所有触发器（使用实际参数名）
        string hurtParamToUse = !string.IsNullOrEmpty(actualHurtParamName) ? actualHurtParamName : hurtParamName;
        if (HasParameter(hurtParamToUse))
            anim.ResetTrigger(hurtParamToUse);

        if (HasParameter(actualAttackParamName))
            anim.ResetTrigger(actualAttackParamName);

        // 强制回到默认状态
        anim.Play("Idle", 0, 0f);
        currentAnimationState = "Idle";
    }

    // ✅ 新增：测试受击后动画恢复
    [ContextMenu("测试受击后动画恢复")]
    private void TestHurtRecovery()
    {
        StartCoroutine(TestHurtRecoveryCoroutine());
    }

    private IEnumerator TestHurtRecoveryCoroutine()
    {
        // 触发受击
        TriggerHurtAnimation();

        // 等待受击动画结束
        yield return new WaitForSeconds(hurtAnimationDuration + 0.1f);

        // 尝试移动
        rb.velocity = new Vector2(1f, 0f);

        // 检查动画状态
        yield return new WaitForSeconds(0.5f);
    }

    // ✅ 新增：强制修复动画状态
    [ContextMenu("强制修复动画状态")]
    private void ForceFixAnimationState()
    {
        if (anim == null) return;

        // 检查当前状态
        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        string stateName = GetAnimationStateName(stateInfo);

        // 如果卡在受击状态，强制退出
        if (stateName == "Hurt" && stateInfo.normalizedTime >= 1.0f)
        {
            ForceExitHurtState();
        }
        else
        {
            // 否则强制切换到空闲状态
            anim.Play("Idle", 0, 0f);
            currentAnimationState = "Idle";
        }

        // 重置所有状态变量
        isHurting = false;
        hurtTimer = 0f;
        isAttacking = false;
        attackAnimationPlaying = false;

        // 强制更新动画状态
        ForceUpdateAnimationState();
    }

    [ContextMenu("刷新攻击范围显示")]
    private void RefreshAttackRangeDisplay()
    {
#if UNITY_EDITOR
        UnityEditor.SceneView.RepaintAll();
        
        if (attackMode == AttackMode.Circle)
        {
            Debug.Log($"  圆形 - 偏移: {circleOffset}, 半径: {attackRange}");
        }
        else
        {
            Debug.Log($"  Box - 偏移: {boxOffset}, 大小: {boxSize}, 角度: {boxAngle}");
            Debug.Log($"  当前Box偏移: {CurrentBoxOffset}, 当前Box角度: {CurrentBoxAngle}");
            Debug.Log($"  Box翻转启用: {flipBoxWithEnemy}");
        }
#endif
    }

    [ContextMenu("切换Box翻转设置")]
    private void ToggleBoxFlip()
    {
        flipBoxWithEnemy = !flipBoxWithEnemy;
        RefreshAttackRangeDisplay();
    }

    // ✅ 新增：编辑器菜单项用于测试视线检测
    [ContextMenu("测试视线检测")]
    private void TestLineOfSight()
    {
        if (player == null)
        {
            Debug.LogError("❌ 玩家引用为空，无法测试视线检测");
            return;
        }

        bool hasSight = CanDetectPlayer();

        // 在场景中高亮显示检测结果
        StartCoroutine(HighlightSightTest());
    }

    private IEnumerator HighlightSightTest()
    {
        Vector2 startPos = sightCheckPoint != null ? (Vector2)sightCheckPoint.position : (Vector2)transform.position;
        Vector2 targetPos = PlayerColliderCenter;

        // 绘制3秒的调试线
        float timer = 0f;
        while (timer < 3f)
        {
            bool hasSight = HasLineOfSightToPlayer();
            Debug.DrawRay(startPos, (targetPos - startPos), hasSight ? Color.green : Color.red, 0.1f);
            timer += Time.deltaTime;
            yield return null;
        }
    }

    [ContextMenu("诊断玩家位置检测")]
    private void DiagnosePlayerDetection()
    {
        Debug.Log("=== 玩家位置检测诊断 ===");

        if (player == null)
        {
            Debug.LogError("❌ player引用为空！");
            return;
        }

        Debug.Log($"player引用对象: {player.name}");
        Debug.Log($"player.position: {player.position}");

        if (playerRoot != null)
        {
            Debug.Log($"playerRoot: {playerRoot.name}");
            Debug.Log($"playerRoot.position: {playerRoot.position}");
        }
        else
        {
            Debug.LogWarning("⚠️ playerRoot为空！");
        }

        if (playerCollider != null)
        {
            Debug.Log($"玩家碰撞体对象: {playerCollider.name}");
            Debug.Log($"碰撞体Size: {playerCollider.size}");
            Debug.Log($"碰撞体Offset: {playerCollider.offset}");
            Debug.Log($"碰撞体bounds.center: {playerCollider.bounds.center}");
            Debug.Log($"碰撞体bounds.size: {playerCollider.bounds.size}");
        }
        else
        {
            Debug.LogWarning("⚠️ playerCollider为空！");
        }

        Debug.Log($"当前使用的位置PlayerColliderCenter: {PlayerColliderCenter}");

        // ✅ 新增：敌人位置信息
        Debug.Log($"敌人根对象位置: {transform.position}");
        Debug.Log($"敌人视觉位置: {EnemyVisualPosition}");
        if (sprite != null)
        {
            Debug.Log($"敌人视觉对象: {sprite.name}, 位置: {sprite.transform.position}");
        }

        // 计算距离
        float xDiff = PlayerColliderCenter.x - transform.position.x;
        Debug.Log($"到玩家的水平距离（基于根对象）: {xDiff}");
        Debug.Log($"攻击范围: {attackRange}");
        Debug.Log($"停止移动范围: {attackRange * 1.2f}");
        Debug.Log($"是否在停止范围内: {Mathf.Abs(xDiff) < attackRange * 1.2f}");
        Debug.Log($"当前朝向: {(facingRight ? "右" : "左")}");
        Debug.Log($"当前速度: {rb.velocity}");

        Debug.Log($"isChasing: {isChasing}");
        Debug.Log($"isAttacking: {isAttacking}");
        Debug.Log($"attackAnimationPlaying: {attackAnimationPlaying}");
        Debug.Log($"isHurting: {isHurting}");
        Debug.Log($"isKnockedBack: {isKnockedBack}");

        // ✅ 修复：检测范围信息（使用根对象位置）
        bool inDetectionRange = IsPlayerInDetectionRange();
        Debug.Log($"检测中心位置: {transform.position}");
        Debug.Log($"在检测范围内: {inDetectionRange}");

        // ✅ 修复：攻击范围检测信息（使用根对象位置）
        bool inAttackRange = IsPlayerInAttackRange();
        Debug.Log($"攻击检测中心位置: {transform.position}");
        Debug.Log($"在攻击范围内: {inAttackRange}");

        // 视线检测信息
        bool hasSight = HasLineOfSightToPlayer();
        Debug.Log($"视线检测结果: {hasSight}");
        Debug.Log($"视线阻挡层: {sightBlockingLayers}");

        // 墙体检测
        if (wallCheckLeft != null && wallCheckRight != null)
        {
            RaycastHit2D leftHit = Physics2D.Raycast(wallCheckLeft.position, Vector2.left, wallCheckDistance, wallLayer);
            RaycastHit2D rightHit = Physics2D.Raycast(wallCheckRight.position, Vector2.right, wallCheckDistance, wallLayer);
            Debug.Log($"左侧墙体检测: {leftHit.collider != null}");
            Debug.Log($"右侧墙体检测: {rightHit.collider != null}");
        }

        Debug.Log("=== 诊断结束 ===");
    }

    private void OnDrawGizmos()
    {
        if (!drawSightGizmos) return;

        // ✅ 修复：在编辑模式下也绘制攻击范围
        Vector2 origin = (attackPoint != null) ? (Vector2)attackPoint.position : (Vector2)transform.position;

        // 绘制检测范围
        Gizmos.color = new Color(0f, 0f, 1f, 0.3f);
        DrawEllipseGizmo(transform.position, detectionWidth, detectionHeight, 64);

        // 根据攻击模式绘制不同的攻击范围
        if (attackMode == AttackMode.Circle)
        {
            // 圆形攻击范围
            Vector2 circleCenter = origin + circleOffset;
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.4f);
            Gizmos.DrawWireSphere(circleCenter, attackRange);

            // 在圆形中心添加小标记
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(circleCenter, 0.1f);
        }
        else
        {
            // Box攻击范围 - ✅ 修复：使用当前Box偏移和角度（考虑翻转）
            Vector2 boxCenter = origin + CurrentBoxOffset;
            Gizmos.color = new Color(1f, 0.6f, 0.0f, 0.4f);

#if UNITY_EDITOR
        // 使用 Handles 绘制带旋转的 Box（更精确）
        UnityEditor.Handles.color = Gizmos.color;
        
        // ✅ 修复：使用 Matrix4x4 来应用旋转，因为 Handles.DrawWireCube 不支持直接旋转
        Matrix4x4 originalMatrix = UnityEditor.Handles.matrix;
        Matrix4x4 rotationMatrix = Matrix4x4.TRS(boxCenter, Quaternion.Euler(0, 0, CurrentBoxAngle), Vector3.one);
        UnityEditor.Handles.matrix = rotationMatrix;
        UnityEditor.Handles.DrawWireCube(Vector3.zero, boxSize);
        UnityEditor.Handles.matrix = originalMatrix;
        
        // 在 Box 中心添加标记
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(boxCenter, 0.1f);
        
        // 绘制Box方向指示线
        Vector2 direction = Quaternion.Euler(0, 0, CurrentBoxAngle) * Vector2.right;
        Gizmos.color = Color.white;
        Gizmos.DrawLine(boxCenter, boxCenter + direction * 0.5f);
#else
            // 运行时回退到 Gizmos（不带旋转）
            Gizmos.DrawWireCube(boxCenter, boxSize);
#endif
        }

        // ===== 新增：绘制AB点巡逻路径 =====
        if (useABPatrol && patrolPointA != null && patrolPointB != null)
        {
            // 绘制巡逻路径线
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(patrolPointA.position, patrolPointB.position);

            // 绘制点A
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(patrolPointA.position, 0.3f);
            Gizmos.DrawWireSphere(patrolPointA.position, reachThreshold);

            // 绘制点B
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(patrolPointB.position, 0.3f);
            Gizmos.DrawWireSphere(patrolPointB.position, reachThreshold);

            // 绘制减速距离
            if (currentPatrolTarget != null)
            {
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
                Gizmos.DrawWireSphere(currentPatrolTarget.position, slowingDistance);
            }

            // 绘制当前目标点
            if (currentPatrolTarget != null && Application.isPlaying)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(currentPatrolTarget.position, 0.2f);

                // 绘制从敌人到目标点的线
                Gizmos.DrawLine(transform.position, currentPatrolTarget.position);

                // 绘制距离文本
#if UNITY_EDITOR
                Vector2 toTarget = currentPatrolTarget.position - transform.position;
                float distanceToTarget = Mathf.Abs(toTarget.x);
                GUIStyle labelStyle = new GUIStyle();
                labelStyle.normal.textColor = Color.yellow;
                labelStyle.fontSize = 10;
                UnityEditor.Handles.Label(transform.position + Vector3.up * 1.2f, 
                    $"目标: {currentPatrolTarget.name}\n距离: {distanceToTarget:F2}\n等待: {isWaitingAtPoint}", labelStyle);
#endif
            }
        }

        // 绘制攻击模式标签
#if UNITY_EDITOR
    GUIStyle style = new GUIStyle();
    style.normal.textColor = attackMode == AttackMode.Circle ? Color.red : Color.yellow;
    style.fontSize = 11;
    style.fontStyle = FontStyle.Bold;
    
    Vector3 labelPos = transform.position + Vector3.up * 1f;
    string modeText = $"攻击模式: {attackMode}";
    if (attackMode == AttackMode.Box)
    {
        modeText += $"\nBox翻转: {(flipBoxWithEnemy ? "启用" : "禁用")}";
    }
    if (useABPatrol)
    {
        modeText += $"\nAB点巡逻: 启用";
    }
    UnityEditor.Handles.Label(labelPos, modeText, style);
#endif

        // 原有的其他Gizmos绘制代码保持不变...
        if (wallCheckLeft != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(
                wallCheckLeft.position,
                wallCheckLeft.position + Vector3.left * wallCheckDistance
            );
        }

        if (wallCheckRight != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(
                wallCheckRight.position,
                wallCheckRight.position + Vector3.right * wallCheckDistance
            );
        }

        // 绘制朝向指示器
        Gizmos.color = facingRight ? Color.green : Color.red;
        Vector3 directionIndicator = transform.position + (facingRight ? Vector3.right : Vector3.left) * 0.8f;
        Gizmos.DrawWireSphere(directionIndicator, 0.2f);

        // 原有的其他Gizmos代码...
        if (!Application.isPlaying || player != null)
        {
            Gizmos.color = new Color(0f, 0f, 1f, 0.3f);
            DrawEllipseGizmo(transform.position, detectionWidth, detectionHeight, 64);
        }

        // 绘制当前攻击形状（调试用）
        if (Application.isPlaying)
        {
            if (attackMode == AttackMode.Circle)
            {
                Vector2 circleCenter = origin + circleOffset;
                Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.4f);
                Gizmos.DrawWireSphere(circleCenter, attackRange);
            }
            else
            {
                Vector2 boxCenter = origin + CurrentBoxOffset;
                Gizmos.color = new Color(1f, 0.6f, 0.0f, 0.4f);
#if UNITY_EDITOR
            // 使用 Handles 绘制带旋转的 Box
            UnityEditor.Handles.color = Gizmos.color;
            
            // ✅ 修复：使用 Matrix4x4 来应用旋转
            Matrix4x4 originalMatrix = UnityEditor.Handles.matrix;
            Matrix4x4 rotationMatrix = Matrix4x4.TRS(boxCenter, Quaternion.Euler(0, 0, CurrentBoxAngle), Vector3.one);
            UnityEditor.Handles.matrix = rotationMatrix;
            UnityEditor.Handles.DrawWireCube(Vector3.zero, boxSize);
            UnityEditor.Handles.matrix = originalMatrix;
#else
                // 运行时回退到 Gizmos
                Gizmos.DrawWireCube(boxCenter, boxSize);
#endif
            }
        }

        // 新增：绘制击退检测点
        if (Application.isPlaying && isKnockedBack)
        {
            Gizmos.color = Color.magenta;
            Vector2[] checkPoints = GetKnockbackCheckPoints(facingRight ? 1f : -1f);
            foreach (Vector2 point in checkPoints)
            {
                Gizmos.DrawWireSphere(point, 0.1f);
            }
        }

        // 新增：绘制当前朝向指示器
        Gizmos.color = facingRight ? Color.green : Color.red;
        Vector3 directionIndicator2 = transform.position + (facingRight ? Vector3.right : Vector3.left) * 0.8f;
        Gizmos.DrawWireSphere(directionIndicator2, 0.2f);

        // 新增：绘制攻击状态指示器
        if (isAttacking || attackAnimationPlaying)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 1.5f, 0.3f);
        }

        // ✅ 新增：绘制受击状态指示器
        if (isHurting)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 1.8f, 0.3f);
        }

        // ✅ 新增：绘制生命状态指示器
        if (enemyAttributes != null)
        {
            if (!enemyAttributes.IsAlive)
            {
                Gizmos.color = Color.black;
                Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.4f);
            }
            else
            {
                float healthPercent = enemyAttributes.GetHealthPercentage();
                Gizmos.color = Color.Lerp(Color.red, Color.green, healthPercent);
                Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.4f);
            }
        }

        // ✅ 新增：绘制当前动画状态指示器
        if (anim != null && Application.isPlaying)
        {
            AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
            string stateName = GetAnimationStateName(stateInfo);

            GUIStyle style2 = new GUIStyle();
            style2.normal.textColor = Color.white;
            style2.fontSize = 12;

#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2.5f, 
            $"动画状态: {stateName}\n受击: {isHurting}\n移动: {Mathf.Abs(rb.velocity.x) > 0.1f}\n当前状态: {currentAnimationState}", style2);
#endif
        }

        // ✅ 新增：绘制玩家位置和距离
        if (Application.isPlaying && player != null)
        {
            Vector2 playerPos = PlayerColliderCenter;
            Vector2 enemyVisualPos = EnemyVisualPosition;
            float xDiff = playerPos.x - transform.position.x; // 使用根对象位置计算距离

            // ✅ 修复：从敌人视觉位置绘制到玩家位置的连线
            Gizmos.color = isChasing ? Color.red : Color.yellow;
            Gizmos.DrawLine(enemyVisualPos, playerPos);

            // 绘制玩家碰撞框中心
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(playerPos, 0.5f);

            // ✅ 修复：在敌人视觉位置绘制标记
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(enemyVisualPos, 0.3f);

            // 绘制距离文本（在视觉位置上方）
#if UNITY_EDITOR
        GUIStyle labelStyle = new GUIStyle();
        labelStyle.normal.textColor = Color.white;
        labelStyle.fontSize = 11;
        UnityEditor.Handles.Label((Vector3)enemyVisualPos + Vector3.up * 3.5f, 
            $"到玩家距离: {Mathf.Abs(xDiff):F1}\n攻击范围: {attackRange}\n停止范围: {attackRange * 1.2f}", labelStyle);
#endif
        }

        // ✅ 修改：简化视线检测绘制 - 只绘制一条主线
        if (Application.isPlaying && player != null)
        {
            Vector2 startPos = sightCheckPoint != null ? (Vector2)sightCheckPoint.position : (Vector2)transform.position;
            Vector2 targetPos = PlayerColliderCenter;

            // 只绘制一条主要的视线线
            bool hasSight = CanDetectPlayer();
            Gizmos.color = hasSight ? Color.green : Color.red;
            Gizmos.DrawLine(startPos, targetPos);
        }
    }

    void DrawEllipseGizmo(Vector3 center, float width, float height, int segments)
    {
        float a = width / 2f;
        float b = height / 2f;

        Vector3 prev = center + new Vector3(a, 0, 0);
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            float x = Mathf.Cos(angle) * a;
            float y = Mathf.Sin(angle) * b;
            Vector3 next = center + new Vector3(x, y, 0);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
}