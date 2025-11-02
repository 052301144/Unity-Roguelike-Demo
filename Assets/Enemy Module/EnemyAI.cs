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

    [Header("检测点设置")]
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

        // ✅ 新增：注册死亡事件监听
        if (enemyAttributes != null)
        {
            enemyAttributes.OnDeath += OnEnemyDeath;
            // ✅ 新增：注册受伤事件监听
            enemyAttributes.OnTakeDamage += OnEnemyTakeDamage;
        }

        // 确保初始朝向正确
        ApplyFacingDirection();

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
        if (enemyAttributes != null && !enemyAttributes.IsAlive)
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
                    Debug.Log("🔄 受击结束，强制切换到空闲状态");
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

        bool playerDetected = IsPlayerInDetectionRange();

        if (playerDetected && !isChasing)
        {
            isChasing = true;
            Debug.Log("🎯 玩家进入检测范围，开始追击！");
        }
        else if (!playerDetected && isChasing)
        {
            isChasing = false;
            Debug.Log("🚶 玩家离开检测范围，恢复巡逻");
        }

        if (playerDetected && player != null)
        {
            // ✅ 修复：使用PlayerColliderCenter获取玩家碰撞框中心位置
            float xDiff = PlayerColliderCenter.x - transform.position.x;
            bool playerOnRight = xDiff > 0;
            if (playerOnRight != facingRight && Time.time >= lastFlipTime + flipCooldown)
            {
                Flip(playerOnRight);
                lastFlipTime = Time.time;
            }
        }

        // 在追击状态下也持续检测墙体
        if (isChasing)
        {
            CheckWallForChasing();
        }

        // 确保攻击条件正确判断
        if (player != null && IsPlayerInAttackRange() && !isAttacking && !attackAnimationPlaying && !isHurting)
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
                Debug.LogWarning("⚠️ 攻击动画超时，强制重置状态");
            }
        }

        // 每120帧监控一次动画状态（避免日志过多）
        if (Time.frameCount % 120 == 0)
        {
            MonitorAnimationState();
        }
    }

    private void FixedUpdate()
    {
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
            Patrol();
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

        // ✅ 修复：使用PlayerColliderCenter获取玩家碰撞框中心位置
        float xDiff = PlayerColliderCenter.x - transform.position.x;

        if (Mathf.Abs(xDiff) > flipThreshold && Time.time >= lastFlipTime + flipCooldown)
        {
            bool shouldFaceRight = xDiff > 0;
            if (shouldFaceRight != facingRight)
            {
                Flip(shouldFaceRight);
                lastFlipTime = Time.time;
            }
        }

        // 改进：如果距离玩家很近，停止移动准备攻击
        if (Mathf.Abs(xDiff) < attackRange * 1.2f)
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
            return;
        }

        // 改进：检查前方是否有墙体阻挡
        Transform checkPoint = facingRight ? wallCheckRight : wallCheckLeft;
        Vector2 dir = facingRight ? Vector2.right : Vector2.left;

        RaycastHit2D wallHit = Physics2D.Raycast(checkPoint.position, dir, wallCheckDistance, wallLayer);
        bool blocked = wallHit.collider != null;

        if (blocked)
        {
            // 追击时遇到墙体，寻找替代路径
            HandleChaseWallCollision();
        }
        else
        {
            float moveDir = Mathf.Sign(xDiff);
            rb.velocity = new Vector2(moveDir * chaseSpeed, rb.velocity.y);
        }
    }

    // 专门用于巡逻的墙体检测
    void CheckWallForPatrol()
    {
        Transform checkPoint = facingRight ? wallCheckRight : wallCheckLeft;
        if (checkPoint == null) return;

        Vector2 dir = facingRight ? Vector2.right : Vector2.left;
        RaycastHit2D hit = Physics2D.Raycast(checkPoint.position, dir, wallCheckDistance, wallLayer);

        if (hit.collider != null && Time.time >= lastFlipTime + flipCooldown)
        {
            Debug.Log("🧱 巡逻时检测到墙体，转向");
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

        Debug.Log("🧱 追击时检测到墙体，寻找替代路径");

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
            Debug.Log("🔄 转向到可行方向继续追击");
        }
        else
        {
            // 两个方向都有墙体，可能是死胡同，停止移动
            rb.velocity = new Vector2(0, rb.velocity.y);
            Debug.Log("❌ 陷入死胡同，停止移动");

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
        Debug.Log("🔄 强制转向尝试逃脱死胡同");
    }

    // 使用子对象的transform.localScale进行翻转
    void Flip(bool faceRight)
    {
        facingRight = faceRight;
        ApplyFacingDirection();
        Debug.Log($"🔄 敌人转向: {(faceRight ? "右" : "左")}");
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

        // ✅ 新增：调试日志，帮助诊断动画状态
        if (Time.frameCount % 60 == 0) // 每60帧记录一次，避免日志过多
        {
            Debug.Log($"🎭 动画状态 - 移动: {isMoving}, 速度X: {rb.velocity.x:F2}, 受击: {isHurting}, 攻击: {isAttacking}, 当前状态: {currentAnimationState}");
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

        Debug.Log("🔄 强制更新动画状态完成");
    }

    // ✅ 新增：动画状态监控方法
    private void MonitorAnimationState()
    {
        if (anim == null) return;

        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        string stateName = GetAnimationStateName(stateInfo);
        Debug.Log($"🎭 当前动画状态: {stateName}, 进度: {stateInfo.normalizedTime:F2}, 循环: {stateInfo.loop}, 长度: {stateInfo.length:F2}");

        // ✅ 新增：检查是否卡在受击状态
        if (stateName == "Hurt" && stateInfo.normalizedTime >= 1.0f && !isHurting)
        {
            Debug.LogWarning("⚠️ 检测到卡在受击状态，强制修复");
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

        Debug.Log("🔧 强制退出受击状态，切换到空闲");
    }

    // 改进：多种攻击动画触发方式
    IEnumerator AttackPlayer()
    {
        isAttacking = true;
        attackAnimationPlaying = true;
        attackAnimationTime = 0f;
        rb.velocity = Vector2.zero;

        Debug.Log("⚔ 敌人发动攻击！");

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
                SafeDealDamage();
            }

            yield return new WaitForSeconds(attackDelay / 2f);
        }

        // 重置状态
        isAttacking = false;
        attackAnimationPlaying = false;

        // ✅ 新增：攻击结束后强制更新动画状态
        ForceUpdateAnimationState();

        Debug.Log("攻击结束");
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
                Debug.LogWarning($"⚠️ 攻击参数 '{actualAttackParamName}' 不存在，使用直接播放方式");
                return false;
            }

            // ✅ 修复：使用字符串名称而不是哈希值，避免哈希值错误
            anim.ResetTrigger(actualAttackParamName);
            anim.SetTrigger(actualAttackParamName);
            currentAnimationState = "Attack";
            Debug.Log($"✅ 使用触发器方式触发攻击动画，参数: {actualAttackParamName}");

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
        Debug.Log("🔄 尝试直接播放攻击动画");

        // 尝试直接播放攻击动画状态
        anim.Play(attackAnimationName, 0, 0f);
        currentAnimationState = "Attack";

        // 等待一帧检查状态
        yield return null;

        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        Debug.Log($"直接播放后状态: {stateInfo.ToString()}");

        if (stateInfo.IsName(attackAnimationName) || stateInfo.length > 0)
        {
            Debug.Log("✅ 直接播放攻击动画成功");

            // 等待动画播放一段时间后造成伤害
            yield return new WaitForSeconds(attackDelay / 2f);

            if (IsPlayerInAttackRange())
            {
                SafeDealDamage();
            }

            // 等待动画剩余时间
            float remainingTime = Mathf.Max(0.1f, attackDelay / 2f);
            yield return new WaitForSeconds(remainingTime);
        }
        else
        {
            Debug.LogError("❌ 直接播放攻击动画失败，使用默认延迟");
            yield return new WaitForSeconds(attackDelay / 2f);

            if (IsPlayerInAttackRange())
            {
                SafeDealDamage();
            }

            yield return new WaitForSeconds(attackDelay / 2f);
        }
    }

    // ✅ 修复：安全的伤害处理方法 - 使用玩家根对象
    void SafeDealDamage()
    {
        // ✅ 改进：如果 playerAttributes 为空，重新查找
        if (playerAttributes == null)
        {
            FindPlayerRootAndAttributes();
        }

        if (playerRoot == null)
        {
            Debug.LogWarning("⚠️ 玩家根对象为空，无法造成伤害");
            return;
        }

        // ✅ 修复：使用玩家根对象的Attribute组件
        if (playerAttributes != null)
        {
            int damageToDeal = attackDamage;
            playerAttributes.TakeDamage(damageToDeal, gameObject);
            Debug.Log($"💥 攻击命中玩家 '{playerRoot.name}'，造成 {damageToDeal} 伤害！");
        }
        else
        {
            // ✅ 修复：最后一次尝试重新查找
            playerAttributes = playerRoot.GetComponent<Attribute>();
            if (playerAttributes == null)
            {
                playerAttributes = playerRoot.GetComponentInChildren<Attribute>(true);
            }
            if (playerAttributes == null && player != null)
            {
                playerAttributes = player.GetComponentInParent<Attribute>();
            }

            if (playerAttributes != null)
            {
                int damageToDeal = attackDamage;
                playerAttributes.TakeDamage(damageToDeal, gameObject);
                Debug.Log($"💥 攻击命中玩家 '{playerRoot.name}'，造成 {damageToDeal} 伤害！");
            }
            else
            {
                Debug.LogError($"❌ 在玩家根对象 '{playerRoot.name}' 及其所有相关对象中都没有找到Attribute组件，无法造成伤害");
                Debug.LogError($"   玩家对象: {player?.name ?? "null"}");
                Debug.LogError($"   玩家根对象: {playerRoot.name}");
                Debug.LogError($"   建议：请确保玩家对象（或其父/子对象）上有 Attribute 组件");
            }
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
        Debug.Log("🎬 攻击动画开始");
        attackAnimationPlaying = true;
        currentAnimationState = "Attack";
    }

    public void OnAttackAnimationEnd()
    {
        Debug.Log("🎬 攻击动画结束");
        attackAnimationPlaying = false;
        isAttacking = false;

        // ✅ 新增：攻击结束后强制更新动画状态
        ForceUpdateAnimationState();
    }

    // ✅ 新增：受击动画事件方法
    public void OnHurtAnimationStart()
    {
        Debug.Log("🎬 受击动画开始");
        isHurting = true;
        currentAnimationState = "Hurt";
    }

    public void OnHurtAnimationEnd()
    {
        Debug.Log("🎬 受击动画结束");
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
        Debug.Log("🎯 攻击命中帧");
        SafeDealDamage();
    }

    bool IsPlayerInDetectionRange()
    {
        if (player == null || detectionPoint == null) return false;

        // ✅ 修复：使用PlayerColliderCenter获取玩家碰撞框中心位置
        Vector2 offset = PlayerColliderCenter - (Vector2)detectionPoint.position;
        float ellipseValue =
            (offset.x * offset.x) / (detectionWidth * detectionWidth / 4f) +
            (offset.y * offset.y) / (detectionHeight * detectionHeight / 4f);

        return ellipseValue <= 1f;
    }

    bool IsPlayerInAttackRange()
    {
        if (player == null || attackPoint == null) return false;

        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, playerLayer);
        return hits.Length > 0;
    }

    // 原有的方法（保持布尔参数）
    public void ApplyWindKnockback(float force, bool fromRight)
    {
        if (isKnockedBack) return;

        // 将布尔方向转换为数值方向
        float direction = fromRight ? -1f : 1f; // fromRight=true 表示力来自右边，所以向左击退
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

        Debug.Log($"🌀 击退开始 - 力量: {force}, 方向: {dir}, 速度: {knockbackSpeed}");

        Vector3 startPosition = transform.position;

        while (elapsed < windKnockbackDuration)
        {
            elapsed += Time.deltaTime;

            float moveStep = knockbackSpeed * Time.deltaTime;

            // 检查是否会撞墙
            bool willHitWall = CheckWallInKnockbackDirection(dir, moveStep);

            if (willHitWall)
            {
                Debug.Log("🧱 击退中检测到墙体，停止击退");
                break;
            }

            // 执行移动
            transform.position += new Vector3(dir * moveStep, 0, 0);

            // 实时位置监控
            if (elapsed < 0.2f) // 只在前0.2秒打印
            {
                Debug.Log($"击退中 - 方向: {dir}, 当前位置X: {transform.position.x:F2}");
            }

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

        Debug.Log($"✅ 击退结束 - 最终位置X: {transform.position.x:F2}, 总移动: {transform.position.x - startPosition.x:F2}");
    }

    // ✅ 改进：死亡方法，与Attribute系统集成
    public void Die()
    {
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

        Debug.Log("💀 敌人死亡 - EnemyAI已禁用");

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
            Debug.LogWarning($"⚠️ 检测到 {name} 嵌入墙体，尝试修复");

            // 尝试向相反方向移动来脱离墙体
            Vector2 escapeDirection = facingRight ? Vector2.left : Vector2.right;
            RaycastHit2D hit = Physics2D.Raycast(transform.position, escapeDirection, 2f, ~wallLayer);

            if (hit.collider != null && ((1 << hit.collider.gameObject.layer) & wallLayer) == 0)
            {
                // 找到安全位置，移动过去
                transform.position = hit.point - (Vector2)escapeDirection * 0.1f;
                Debug.Log($"✅ 已修复 {name} 的墙体嵌入问题");
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
                Debug.Log($"✅ 通过标签找到玩家: {player.name}");
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
                Debug.Log($"🔍 方法1 - 从Visual子对象找到根对象: {playerRoot.name} (从 {player.name})");
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
                Debug.Log($"🔍 方法2 - 通过PlayerController找到根对象: {playerRoot.name}");
            }
        }

        // 方法3: 尝试查找有 Attribute 组件的父对象或根对象
        if (playerRoot == null)
        {
            Attribute attr = player.GetComponentInParent<Attribute>();
            if (attr != null)
            {
                playerRoot = attr.transform;
                Debug.Log($"🔍 方法3 - 通过Attribute父组件找到根对象: {playerRoot.name}");
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
            Debug.Log($"🔍 方法4 - 向上查找根对象: {playerRoot.name}");
        }

        // 如果还是没找到，使用当前对象
        if (playerRoot == null)
        {
            playerRoot = player;
            Debug.LogWarning($"⚠️ 无法找到玩家根对象，使用当前对象: {player.name}");
        }
        
        // ✅ 新增：查找玩家的BoxCollider2D组件（用于精确的碰撞框检测）
        playerCollider = playerRoot.GetComponent<BoxCollider2D>();
        if (playerCollider != null)
        {
            Debug.Log($"✅ 找到玩家BoxCollider2D - Size: {playerCollider.size}, Offset: {playerCollider.offset}");
        }
        else
        {
            // 如果在根对象没找到，尝试在整个玩家层次结构中查找
            playerCollider = player.GetComponentInParent<BoxCollider2D>();
            if (playerCollider == null)
            {
                playerCollider = player.GetComponentInChildren<BoxCollider2D>();
            }
            if (playerCollider != null)
            {
                Debug.Log($"✅ 在玩家层次结构中找到BoxCollider2D - Size: {playerCollider.size}, Offset: {playerCollider.offset}");
            }
        }

        // ✅ 改进：多层级查找 Attribute 组件
        playerAttributes = null;

        // 首先在根对象上查找
        playerAttributes = playerRoot.GetComponent<Attribute>();
        if (playerAttributes != null)
        {
            Debug.Log($"✅ 在玩家根对象 '{playerRoot.name}' 上找到Attribute组件");
            return;
        }

        // 在根对象的子对象中查找（包括所有子对象）
        playerAttributes = playerRoot.GetComponentInChildren<Attribute>(true);
        if (playerAttributes != null)
        {
            Debug.Log($"✅ 在玩家子对象 '{playerAttributes.gameObject.name}' 上找到Attribute组件");
            return;
        }

        // 尝试在整个玩家层次结构中查找（包括父对象）
        playerAttributes = player.GetComponentInParent<Attribute>();
        if (playerAttributes != null)
        {
            Debug.Log($"✅ 在玩家父对象 '{playerAttributes.gameObject.name}' 上找到Attribute组件");
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
                Debug.Log($"✅ 通过场景扫描在 '{attr.gameObject.name}' 上找到Attribute组件");
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
                Debug.Log($"✅ 通过PlayerController扫描在 '{pc.gameObject.name}' 上找到Attribute组件");
                return;
            }
        }

        // 如果还是找不到，输出详细的调试信息
        Debug.LogError($"❌ 在玩家对象 '{playerRoot.name}' 及其所有相关对象中都没有找到Attribute组件！");
        Debug.LogError($"   玩家对象路径: {GetFullPath(playerRoot)}");
        Debug.LogError($"   玩家对象标签: {playerRoot.tag}");
        Debug.LogError($"   建议：请在玩家对象（或根对象）上添加 Attribute 组件");
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

        Debug.Log("🔄 动画状态已重置");
    }

    // ✅ 新增：测试受击后动画恢复
    [ContextMenu("测试受击后动画恢复")]
    private void TestHurtRecovery()
    {
        StartCoroutine(TestHurtRecoveryCoroutine());
    }

    private IEnumerator TestHurtRecoveryCoroutine()
    {
        Debug.Log("🧪 开始测试受击后动画恢复");

        // 触发受击
        TriggerHurtAnimation();

        // 等待受击动画结束
        yield return new WaitForSeconds(hurtAnimationDuration + 0.1f);

        // 尝试移动
        rb.velocity = new Vector2(1f, 0f);

        // 检查动画状态
        yield return new WaitForSeconds(0.5f);

        if (anim != null && HasParameter(walkParamName))
        {
            bool isWalking = anim.GetBool(walkParamName);
            Debug.Log($"🧪 测试结果 - 应该行走: {Mathf.Abs(rb.velocity.x) > 0.1f}, 实际行走状态: {isWalking}, 当前状态: {currentAnimationState}");
        }
    }

    // ✅ 新增：强制修复动画状态
    [ContextMenu("强制修复动画状态")]
    private void ForceFixAnimationState()
    {
        if (anim == null) return;

        // 检查当前状态
        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        string stateName = GetAnimationStateName(stateInfo);

        Debug.Log($"🔧 强制修复前状态: {stateName}, 进度: {stateInfo.normalizedTime:F2}");

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
            Debug.Log("🔧 强制切换到空闲状态");
        }

        // 重置所有状态变量
        isHurting = false;
        hurtTimer = 0f;
        isAttacking = false;
        attackAnimationPlaying = false;

        // 强制更新动画状态
        ForceUpdateAnimationState();
    }

    private void OnDrawGizmos()
    {
        if (detectionPoint != null)
        {
            Gizmos.color = new Color(0f, 0f, 1f, 0.3f);
            DrawEllipseGizmo(detectionPoint.position, detectionWidth, detectionHeight, 64);
        }

        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }

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
        Vector3 directionIndicator = transform.position + (facingRight ? Vector3.right : Vector3.left) * 0.8f;
        Gizmos.DrawWireSphere(directionIndicator, 0.2f);

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

            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.white;
            style.fontSize = 12;

#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2.5f, 
                $"动画状态: {stateName}\n受击: {isHurting}\n移动: {Mathf.Abs(rb.velocity.x) > 0.1f}\n当前状态: {currentAnimationState}", style);
#endif
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