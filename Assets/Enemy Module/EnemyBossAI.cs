using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyBossAI : MonoBehaviour
{
    [Header("移动参数")]
    public float moveSpeed = 3f;
    public float chaseSpeed = 4.5f;
    public float wallCheckDistance = 0.2f;
    public LayerMask wallLayer;

    [Header("检测范围")]
    public float detectionWidth = 10f;
    public float detectionHeight = 5f;
    public LayerMask playerLayer;

    [Header("攻击参数")]
    public float attack1Range = 2f;
    public float attack2Range = 3f;
    public float attack3Range = 4f;
    public float attack4Range = 5f;
    // 每段攻击的延迟（可在 Inspector 单独设置）
    public float attack1Delay = 0.5f;
    public float attack2Delay = 0.5f;
    public float attack3Delay = 0.5f;
    public float attack4Delay = 0.5f;
    public int attackDamage = 10;
    public int consecutiveHitsToTriggerAttack4 = 3;
    public float attackOffset = 1.2f; // 基于 transform.position 的左右偏移（可在 Inspector 调整）

    [Header("攻击检测点")]
    public Transform attackPoint;
    public Transform wallCheckLeft;
    public Transform wallCheckRight;
    public Transform detectionPoint;

    [Header("玩家设置")]
    public Transform player;

    [Header("动画参数名称")]
    public string walkParamName = "isWalk";
    public string attack1ParamName = "isAttack1";
    public string attack2ParamName = "isAttack2";
    public string attack3ParamName = "isAttack3";
    public string attack4ParamName = "isAttack4";
    public string hurtParamName = "isHurt";
    public string deadParamName = "isDead";

    [Header("（可选）按状态名强制播放动画")]
    [Tooltip("如果 Animator 参数/过渡配置有问题，可以在 Inspector 填写状态机中的状态名并启用 forcePlayStates 来直接播放对应动画作为后备。")]
    public bool forcePlayStates = false;
    public string walkStateName = "Enemy BOSS1 Walk";
    public string idleStateName = "Enemy BOSS1 Idle";
    public string attack1StateName = "Enemy BOSS1 Attack1";
    public string attack2StateName = "Enemy BOSS1 Attack2";
    public string attack3StateName = "Enemy BOSS1 Attack3";
    public string attack4StateName = "Enemy BOSS1 Attack4";
    public string hurtStateName = "Enemy BOSS1 Hurt";
    public string deathStateName = "Enemy BOSS1 Death";

    [Header("动画对象引用")]
    public Transform animationChild;

    [Header("受击设置")]
    public float hurtAnimationDuration = 0.3f;

    [Header("连段/动画设置（可调）")]
    [Tooltip("如果动画没有事件触发下一段，是否自动在攻击结束后开启下一段（作为后备）。建议留 true 便于调试，正式需求可设 false 强制动画事件。")]
    public bool allowFallbackEnableCombo = true;
    [Tooltip("自动后备开启的延迟（若启用）")]
    public float fallbackEnableDelay = 0.05f;

    private Rigidbody2D rb;
    private bool isAttacking = false;
    private bool isChasing = false;
    private bool facingRight = true;
    private bool isKnockedBack = false;
    private bool isHurting = false;
    private bool isDead = false;

    private float hurtTimer = 0f;
    private float lastFlipTime = 0f;
    private float flipCooldown = 0.25f; // 更短的冷却，但仍防抖
    private float flipThreshold = 0.5f;

    private SpriteRenderer sprite;
    private Animator anim;
    private Attribute enemyAttributes;

    private int consecutiveHits = 0;

    private Vector3 originalScale;
    private bool attackAnimationPlaying = false;
    private float attackAnimationTime = 0f;
    // 当前正在播放的攻击的延迟，用于 Update 中的超时判断
    private float currentAttackDelay = 0.5f;

    private string currentAnimationState = "Idle";

    private Attribute playerAttributes;
    private Transform playerRoot;
    private BoxCollider2D playerCollider;

    // 连段状态（新增）
    // 0 = 未开始，1 = Attack1 已触发，2 = Attack2 已触发，3 = Attack3 已触发（完成）
    private int comboStage = 0;
    // 动画事件控制的开关（由动画事件调用 AE_EnableAttack2/3 开启）
    private bool isAttack2Enabled = false;
    private bool isAttack3Enabled = false;
    // 标记是否正在执行强制 Attack4（此时不应被普通取消逻辑打断）
    private bool attack4ForcedActive = false;
    // 动画事件短期屏蔽：用于在取消攻击或强制攻击切换时，阻止动画事件立即启动新连段
    private float blockAEUntil = 0f;

    // 用于防止激活时瞬间抖动：记录首次检测到玩家的时间，短暂延迟允许平滑朝向
    private float firstDetectedTime = -999f;
    private float detectFaceDelay = 0.08f; // 小延迟以避免瞬间翻转抖动

    private Vector2 PlayerColliderCenter
    {
        get
        {
            if (playerCollider != null)
            {
                return playerCollider.bounds.center;
            }
            if (playerRoot != null && playerRoot != player)
            {
                return playerRoot.position;
            }
            return player != null ? player.position : Vector2.zero;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        attack1Range = Mathf.Max(0.1f, attack1Range);
        attack2Range = Mathf.Max(0.1f, attack2Range);
        attack3Range = Mathf.Max(0.1f, attack3Range);
        attack4Range = Mathf.Max(0.1f, attack4Range);
        moveSpeed = Mathf.Max(0.1f, moveSpeed);
        chaseSpeed = Mathf.Max(0.1f, chaseSpeed);
        wallCheckDistance = Mathf.Max(0.01f, wallCheckDistance);
        attack1Delay = Mathf.Max(0.01f, attack1Delay);
        attack2Delay = Mathf.Max(0.01f, attack2Delay);
        attack3Delay = Mathf.Max(0.01f, attack3Delay);
        attack4Delay = Mathf.Max(0.01f, attack4Delay);
    }
#endif

    private void Awake()
    {
        // ensure rb exists
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();

        if (animationChild != null)
        {
            sprite = animationChild.GetComponent<SpriteRenderer>();
            anim = animationChild.GetComponent<Animator>();
        }
        else
        {
            sprite = GetComponentInChildren<SpriteRenderer>();
            anim = GetComponentInChildren<Animator>();
        }

        enemyAttributes = GetComponent<Attribute>();

        if (sprite != null)
        {
            originalScale = sprite.transform.localScale;
        }
        else
        {
            originalScale = transform.localScale;
        }

        // safe Rigidbody settings
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.freezeRotation = true;
            rb.velocity = Vector2.zero; // 初始静止
        }

        FindAndSetupPlayer();

        if (enemyAttributes != null)
        {
            enemyAttributes.OnDeath += OnEnemyDeath;
            enemyAttributes.OnTakeDamage += OnEnemyTakeDamage;
        }

        ApplyFacingDirection();

        currentAnimationState = "Idle"; // 初始待机

        Debug.Log($"🎯 EnemyBossAI初始化完成 - 连续受击次数触发攻击4: {consecutiveHitsToTriggerAttack4}");
    }

    private void OnDestroy()
    {
        if (enemyAttributes != null)
        {
            enemyAttributes.OnDeath -= OnEnemyDeath;
            enemyAttributes.OnTakeDamage -= OnEnemyTakeDamage;
        }
    }

    // 重要：当 Boss 受到伤害时会被调用。
    // 为了实现模式1（攻击4优先），当 consecutiveHits 达到阈值时立即中断并强制触发 Attack4。
    private void OnEnemyTakeDamage(int damage, GameObject attacker)
    {
        // 增加连击计数，触发 Hurt 动画
        Debug.Log($"🩸 Boss受到 {damage} 点伤害，来自: {(attacker!=null?attacker.name:"null")}，连续受击: {consecutiveHits + 1}/{consecutiveHitsToTriggerAttack4}");

        consecutiveHits++;
        Debug.Log($"🩸 consecutiveHits incremented -> {consecutiveHits}");
        // 如果达到阈值，立即强制触发 Attack4（模式1）并跳过 Hurt 流程——Attack4 优先级高于受击
        if (consecutiveHits >= consecutiveHitsToTriggerAttack4)
        {
            Debug.Log($"💥 连续受击{consecutiveHits}次，强制触发 Attack4（最高优先级）！");
            // 重置计数，避免重复触发
            consecutiveHits = 0;

            // 标记强制 Attack4 即将运行，防止取消逻辑中断它
            attack4ForcedActive = true;

            // 强制中断并触发 Attack4（立即）
            // StopAllCoroutines 会停止本脚本上的所有协程 —— 包括可能的 Attack1/2/3 协程
            Debug.Log("💥 OnEnemyTakeDamage: StopAllCoroutines -> 强制中断当前协程");
            StopAllCoroutines();

            // 屏蔽动画事件短时间，避免动画事件在协程被停止后仍触发新的连段
            blockAEUntil = Time.time + 0.25f;

            // 清理连段状态并动画变量，确保 Attack4 是独立流程
            comboStage = 0;
            isAttack2Enabled = false;
            isAttack3Enabled = false;

            // 清理 animator 中的攻击标志（避免残留）
            if (anim != null)
            {
                if (HasParameter(attack1ParamName)) anim.ResetTrigger(attack1ParamName);
                if (HasParameter(attack2ParamName)) SetAnimatorParameterFalse(attack2ParamName);
                if (HasParameter(attack3ParamName)) SetAnimatorParameterFalse(attack3ParamName);
                if (HasParameter(attack4ParamName)) SetAnimatorParameterFalse(attack4ParamName);
                // 同时清理 Hurt Trigger（避免残留导致状态冲突）
                if (HasParameter(hurtParamName)) SetAnimatorParameterFalse(hurtParamName);
            }

            // 确保状态标记
            isAttacking = false;
            attackAnimationPlaying = false;
            // 取消受击标志以允许 Attack4 处理伤害逻辑（Attack4 优先）
            isHurting = false;

            // 启动强制 Attack4（不再受当前攻击/受击状态限制）
            StartCoroutine(Attack4_Forced());
            return;
        }

        // 若未达到触发 Attack4 的阈值，则按正常流程触发 Hurt
        TriggerHurtAnimation();
    }

    void TriggerHurtAnimation()
    {
        if (isHurting || enemyAttributes == null || !enemyAttributes.IsAlive) return;

        if (anim == null) return;

        if (HasParameter(hurtParamName))
        {
            // 标记受击并清理与攻击相关的状态，避免在受击期间仍然造成伤害
            anim.SetTrigger(hurtParamName);
            isHurting = true;
            hurtTimer = 0f;
            currentAnimationState = "Hurt";

            // 中断并清理所有攻击标志，防止协程尚未结束时继续造成伤害或触发连段
            isAttacking = false;
            attackAnimationPlaying = false;
            comboStage = 0;
            isAttack2Enabled = false;
            isAttack3Enabled = false;

            // 重置 Animator 中可能的攻击参数（Trigger/Bool/Int/Float）
            if (anim != null)
            {
                if (HasParameter(attack1ParamName)) SetAnimatorParameterFalse(attack1ParamName);
                if (HasParameter(attack2ParamName)) SetAnimatorParameterFalse(attack2ParamName);
                if (HasParameter(attack3ParamName)) SetAnimatorParameterFalse(attack3ParamName);
                if (HasParameter(attack4ParamName)) SetAnimatorParameterFalse(attack4ParamName);
            }

            if (forcePlayStates && !string.IsNullOrEmpty(hurtStateName))
            {
                anim.Play(hurtStateName);
            }
        }
    }

    private void Update()
    {
        if (isDead) return;

        if (player == null)
        {
            FindAndSetupPlayer();
        }

        if (isHurting)
        {
            hurtTimer += Time.deltaTime;
            if (hurtTimer >= hurtAnimationDuration)
            {
                isHurting = false;
                hurtTimer = 0f;
                ForceUpdateAnimationState();
            }
        }

        // 状态锁：攻击、击退或受击期间不做探测/翻转/追击逻辑
        if (isAttacking || isKnockedBack || isHurting) return;

        if (player == null)
        {
            isChasing = false;
            return;
        }

        bool playerDetected = IsPlayerInDetectionRange();

        // 进入检测区时记录首次发现时间，用于避免瞬间抖动翻转
        if (playerDetected && !isChasing)
        {
            firstDetectedTime = Time.time;
            isChasing = true;
            Debug.Log("🎯 玩家进入检测范围，开始追击！");
        }
        else if (!playerDetected && isChasing)
        {
            // 离开检测范围立即停止追击（你要求不巡逻，所以停止时静止）
            isChasing = false;
            Debug.Log("🚫 玩家离开检测范围，停止追击");
            // 当玩家离开检测范围时，取消当前的普通攻击流程（但保留强制 Attack4）
            CancelAttacksDueToPlayerLoss();
        }

        if (playerDetected && player != null)
        {
            float xDiff = PlayerColliderCenter.x - transform.position.x;
            bool playerOnRight = xDiff > 0;

            // 改进翻转逻辑：当刚检测到玩家时稍作延迟再翻转，避免瞬间抖动
            if (Mathf.Abs(xDiff) > flipThreshold && playerOnRight != facingRight && Time.time >= lastFlipTime + flipCooldown)
            {
                // only flip immediately if already chasing for a short time (reduce activation jitter)
                if (Time.time >= firstDetectedTime + detectFaceDelay)
                {
                    Flip(playerOnRight);
                    lastFlipTime = Time.time;
                }
            }
        }

        // 优化：仅当满足状态时检查连段（按顺序）
        if (player != null && !isAttacking && !attackAnimationPlaying && !isHurting)
        {
            CheckAndStartAttack_Sequential();
        }

        if (attackAnimationPlaying)
        {
            attackAnimationTime += Time.deltaTime;
            if (attackAnimationTime > currentAttackDelay * 1.5f)
            {
                attackAnimationPlaying = false;
                isAttacking = false;
            }
        }
    }

    private void FixedUpdate()
    {
        if (isDead) return;

        if (enemyAttributes != null && !enemyAttributes.IsAlive)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        // 如果攻击、播放攻击动画或受击时，不移动
        if (isAttacking || attackAnimationPlaying || isHurting)
        {
            if (rb != null) rb.velocity = Vector2.zero;
            return;
        }

        if (isKnockedBack)
            return;

        // 你要求永远不巡逻：保持 Patrol() 为空（但保留函数以不破坏结构）
        // 当 isChasing 为真时追击玩家
        if (isChasing && player != null)
        {
            ChasePlayer();
        }
        else
        {
            // 不做 Patrol(), 但确保速度为 0（保持待机）
            if (rb != null) rb.velocity = Vector2.zero;
        }

        UpdateAnimationState();
    }

    // 保留方法签名但不执行巡逻（满足“永远不巡逻”的要求）
    void Patrol()
    {
        // 不执行任何巡逻逻辑，Boss 静止直到检测到玩家
        if (rb != null) rb.velocity = Vector2.zero;
    }

    void ChasePlayer()
    {
        if (player == null) return;

        float xDiff = PlayerColliderCenter.x - transform.position.x;

        bool justFlipped = false;
        if (Mathf.Abs(xDiff) > flipThreshold && Time.time >= lastFlipTime + flipCooldown)
        {
            bool shouldFaceRight = xDiff > 0;
            if (shouldFaceRight != facingRight)
            {
                // 翻转时记录，防止连续多次翻转
                Flip(shouldFaceRight);
                lastFlipTime = Time.time;
                justFlipped = true;
            }
        }

        // 当玩家足够接近攻击1时停止移动以保证稳定攻击距离
        // 使用攻击原点到玩家中心的精确距离判断，避免 transform.x vs attackOffset 造成的偏差
        Vector2 attackOrigin = GetAttackOrigin();
        float distToPlayer = Vector2.Distance(attackOrigin, PlayerColliderCenter);

        if (distToPlayer <= attack1Range && !justFlipped)
        {
            if (rb != null) rb.velocity = new Vector2(0, rb.velocity.y);
            return;
        }

        Transform checkPoint = facingRight ? wallCheckRight : wallCheckLeft;
        if (checkPoint == null)
        {
            float moveDir = Mathf.Sign(xDiff);
            if (rb != null) rb.velocity = new Vector2(moveDir * chaseSpeed, rb.velocity.y);
            return;
        }

        Vector2 dir = facingRight ? Vector2.right : Vector2.left;
        RaycastHit2D wallHit = Physics2D.Raycast(checkPoint.position, dir, wallCheckDistance, wallLayer);
        bool blocked = wallHit.collider != null;

        if (blocked)
        {
            HandleChaseWallCollision();
        }
        else
        {
            float moveDir = Mathf.Sign(xDiff);
            if (rb != null) rb.velocity = new Vector2(moveDir * chaseSpeed, rb.velocity.y);
        }
    }

    void CheckWallForPatrol()
    {
        // 虽然我们不巡逻，但保留墙体检测方法以兼容未来逻辑
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

    void HandleChaseWallCollision()
    {
        if (isAttacking || isKnockedBack || isHurting) return;
        if (Time.time < lastFlipTime + flipCooldown) return;

        bool alternativeDirection = !facingRight;
        Transform altCheckPoint = alternativeDirection ? wallCheckRight : wallCheckLeft;
        if (altCheckPoint == null) return;
        Vector2 altDir = alternativeDirection ? Vector2.right : Vector2.left;

        RaycastHit2D altHit = Physics2D.Raycast(altCheckPoint.position, altDir, wallCheckDistance * 2f, wallLayer);

        if (altHit.collider == null)
        {
            Flip(alternativeDirection);
            lastFlipTime = Time.time;
        }
        else
        {
            if (rb != null) rb.velocity = new Vector2(0, rb.velocity.y);
            StartCoroutine(EscapeCoroutine());
        }
    }

    private IEnumerator EscapeCoroutine()
    {
        yield return new WaitForSeconds(1f);
        Flip(!facingRight);
        lastFlipTime = Time.time;
    }

    void Flip(bool faceRight)
    {
        facingRight = faceRight;
        ApplyFacingDirection();
    }

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

    void UpdateAnimationState()
    {
        if (anim == null) return;

        bool isMoving = Mathf.Abs(rb != null ? rb.velocity.x : 0f) > 0.1f && !isAttacking && !attackAnimationPlaying && !isKnockedBack && !isHurting;
        if (forcePlayStates)
        {
            // 使用按状态名强制播放（只在需要时作为后备）
            string desired = isMoving ? "Walk" : "Idle";
            string desiredStateName = isMoving ? walkStateName : idleStateName;
            if (!string.IsNullOrEmpty(desiredStateName) && currentAnimationState != desired)
            {
                anim.Play(desiredStateName);
                currentAnimationState = desired;
            }
        }
        else
        {
            if (HasParameter(walkParamName))
            {
                anim.SetBool(walkParamName, isMoving);

                if (isMoving)
                {
                    currentAnimationState = "Walk";
                }
                else if (!isAttacking && !attackAnimationPlaying && !isHurting && !isKnockedBack)
                {
                    currentAnimationState = "Idle";
                }
            }

            if ((isAttacking || attackAnimationPlaying || isHurting) && HasParameter(walkParamName))
            {
                anim.SetBool(walkParamName, false);
            }
        }
    }

    void ForceUpdateAnimationState()
    {
        if (anim == null) return;

        if (HasParameter(walkParamName))
        {
            bool shouldWalk = Mathf.Abs(rb != null ? rb.velocity.x : 0f) > 0.1f && !isAttacking && !attackAnimationPlaying && !isKnockedBack && !isHurting;
            anim.SetBool(walkParamName, shouldWalk);

            if (shouldWalk)
            {
                currentAnimationState = "Walk";
            }
            else
            {
                currentAnimationState = "Idle";
            }
        }

        if (HasParameter(hurtParamName))
        {
            anim.ResetTrigger(hurtParamName);
        }
    }

    // 严格按顺序检测并启动攻击（满足你要求）
    void CheckAndStartAttack_Sequential()
    {
        if (isDead || isHurting || isKnockedBack) return;

        // 如果当前没有正在连段（comboStage == 0），优先检测 attack1Range
        if (comboStage == 0)
        {
            // 使用更可靠的距离检查以避免 LayerMask/Collider 层级导致的误判
            // 仅当玩家仍在检测区域内且位于攻击1范围时才触发 Attack1
            if (IsPlayerInDetectionRange() && IsPlayerWithinRangeExact(attack1Range))
            {
                // 启动 Attack1（Trigger 类型）
                StartCoroutine(Attack1());
                comboStage = 1; // 标记 Attack1 已触发（等待动画事件开启下一段）
                // 注意：真正允许 Attack2 仍由 AE_EnableAttack2()（动画事件）开启 isAttack2Enabled
                return;
            }
            return;
        }

        // comboStage == 1：Attack1 已触发，等待动画事件开启 isAttack2Enabled
        if (comboStage == 1 && isAttack2Enabled)
        {
            // 更改为精确距离判断，避免因 collider 层级/重叠导致无法识别
            if (IsPlayerWithinRangeExact(attack2Range))
            {
                StartCoroutine(Attack2());
                comboStage = 2; // Attack2 已触发，等待 AE_EnableAttack3()
                // 不直接清理 isAttack2Enabled——由结束处或重置逻辑清理
                return;
            }
            return;
        }

        // comboStage == 2：Attack2 已触发，等待动画事件开启 isAttack3Enabled
        if (comboStage == 2 && isAttack3Enabled)
        {
            if (IsPlayerWithinRangeExact(attack3Range))
            {
                StartCoroutine(Attack3());
                comboStage = 3; // 完成连段
                return;
            }
            return;
        }

        // 如果连段完成或超时，且当前没有攻击动画播放，清理连段状态（复位）
        if (!isAttacking && !attackAnimationPlaying)
        {
            if (comboStage >= 3)
            {
                comboStage = 0;
                isAttack2Enabled = false;
                isAttack3Enabled = false;
            }
            // 如果希望不强制在一段时间内连段，可以在这里添加超时重置逻辑
        }
    }

    IEnumerator Attack1()
    {
        if (isAttacking) yield break;

        isAttacking = true;
            attackAnimationPlaying = true;
            attackAnimationTime = 0f;
            if (rb != null) rb.velocity = Vector2.zero;
            Debug.Log("⚔ Boss发动攻击1！");

        // 先关闭行走标志，避免 Walk 状态覆盖攻击过渡
        if (anim != null && HasParameter(walkParamName))
        {
            SetAnimatorParameterFalse(walkParamName);
        }

        // 在启动本段前清理所有攻击参数，防止残留导致状态冲突
        ResetAllAttackParameters();
        // 根据 Animator 中该参数的类型正确触发攻击参数（Trigger/Bool）
        if (anim != null && HasParameter(attack1ParamName))
        {
            SetAnimatorParameterTrue(attack1ParamName);
            currentAnimationState = "Attack1";
            if (forcePlayStates && !string.IsNullOrEmpty(attack1StateName))
            {
                anim.Play(attack1StateName);
            }
        }

        // 记录当前攻击延迟，用于 Update 中的超时保护
        currentAttackDelay = attack1Delay;

        // 打印参数与状态，便于现场排查（在设置参数后）
        LogAnimatorDebugInfo("Attack1 start");

        // 攻击判定（时间点可由动画事件替换）
        yield return new WaitForSeconds(currentAttackDelay / 2f);

        Debug.Log("Attack1: resumed after first wait");

        if (IsPlayerInAttackRange(attack1Range))
        {
            // 在命中判定前再次确认未进入受击状态
            if (!isHurting && !isDead)
            {
                DealDamage(attackDamage, attack1Range);
            }
            else
            {
                Debug.Log("⚠ Attack1 命中点被取消：Boss 当前处于受击或死亡状态");
            }
        }

        yield return new WaitForSeconds(currentAttackDelay / 2f);

        Debug.Log("Attack1: resumed after second wait");

        // 在结束时仅在没有下一段接手时清理 isAttacking/attackAnimationPlaying，
        // 如果 comboStage 已经被推进（>=2），说明 AE 已经请求并启动了下一段连段，
        // 此时让下一段自己负责清理状态，避免在段间出现回到 Idle 的帧。
        if (comboStage < 2)
        {
            // 本段结束且无下一段接手：清理本段参数并回到待机
            SetAnimatorParameterFalse(attack1ParamName);
            isAttacking = false;
            attackAnimationPlaying = false;
            ForceUpdateAnimationState();

            // Fallback: 如果动画事件没有触发 isAttack2Enabled，并且允许后备开启（用于调试/兼容）
            if (!isAttack2Enabled && allowFallbackEnableCombo)
            {
                // small delay to ensure animation has finished transitions
                yield return new WaitForSeconds(fallbackEnableDelay);
                isAttack2Enabled = true;
            }
        }
        else
        {
            // 有下一段接手时，不在这里把 isAttacking/attackAnimationPlaying 设为 false，
            // 等待下一段开始后由其结束逻辑来清理。
        }
        
        // 不在这里复位 comboStage，让动画事件或后续逻辑控制连段的推进与复位。
        
    }

    IEnumerator Attack2()
    {
        // 允许在上一段（Attack1）仍在运行时直接开始下一段，
        // 因为连段由动画事件驱动，这避免中间回到 Idle 的一帧。

        // 标记连段阶段为 2（防止前一段在结束时清理状态）
        comboStage = Mathf.Max(comboStage, 2);

        isAttacking = true;
            attackAnimationPlaying = true;
            attackAnimationTime = 0f;
            if (rb != null) rb.velocity = Vector2.zero;

            Debug.Log("⚔ Boss发动攻击2！");

        // 关闭行走标志
        if (anim != null && HasParameter(walkParamName))
        {
            SetAnimatorParameterFalse(walkParamName);
        }

        // 重置上一段的攻击参数，确保 Animator 能直接从 Attack1 过渡到 Attack2
        if (anim != null && HasParameter(attack1ParamName))
        {
            SetAnimatorParameterFalse(attack1ParamName);
        }

        // 在启动前清理所有攻击参数，避免残留
        ResetAllAttackParameters();
        if (anim != null && HasParameter(attack2ParamName))
        {
            SetAnimatorParameterTrue(attack2ParamName);
            currentAnimationState = "Attack2";
            if (forcePlayStates && !string.IsNullOrEmpty(attack2StateName))
            {
                anim.Play(attack2StateName);
            }
        }

        // 记录当前攻击延迟
        currentAttackDelay = attack2Delay;

        LogAnimatorDebugInfo("Attack2 start");

        yield return new WaitForSeconds(currentAttackDelay / 2f);

        Debug.Log("Attack2: resumed after first wait");

        if (IsPlayerInAttackRange(attack2Range))
        {
            if (!isHurting && !isDead)
            {
                DealDamage(attackDamage, attack2Range);
            }
            else
            {
                Debug.Log("⚠ Attack2 命中点被取消：Boss 当前处于受击或死亡状态");
            }
        }

        yield return new WaitForSeconds(currentAttackDelay / 2f);

        Debug.Log("Attack2: resumed after second wait");

        if (anim != null && HasParameter(attack2ParamName))
        {
            SetAnimatorParameterFalse(attack2ParamName);
        }

        // 只有当没有下一段（Attack3）接手时才清理攻击状态
        if (comboStage < 3)
        {
            isAttacking = false;
            attackAnimationPlaying = false;
            ForceUpdateAnimationState();
        }
        else
        {
            // 下一段会接手并负责清理
        }

        // Fallback: 自动开启 Attack3（仅在允许且还未由动画事件开启时）
        if (!isAttack3Enabled && allowFallbackEnableCombo && comboStage < 3)
        {
            yield return new WaitForSeconds(fallbackEnableDelay);
            isAttack3Enabled = true;
        }
        
    }

    IEnumerator Attack3()
    {
        // 标记连段阶段为 3（防止前一段在结束时清理状态）
        comboStage = Mathf.Max(comboStage, 3);

        isAttacking = true;
            attackAnimationPlaying = true;
            attackAnimationTime = 0f;
            if (rb != null) rb.velocity = Vector2.zero;

            Debug.Log("⚔ Boss发动攻击3！");

        if (anim != null && HasParameter(walkParamName))
        {
            SetAnimatorParameterFalse(walkParamName);
        }

        // 重置上一段的参数（Attack2），确保 Animator 能直接过渡
        if (anim != null && HasParameter(attack2ParamName))
        {
            SetAnimatorParameterFalse(attack2ParamName);
        }

        // 在启动前清理所有攻击参数，避免残留
        ResetAllAttackParameters();
        if (anim != null && HasParameter(attack3ParamName))
        {
            SetAnimatorParameterTrue(attack3ParamName);
            currentAnimationState = "Attack3";
            if (forcePlayStates && !string.IsNullOrEmpty(attack3StateName))
            {
                anim.Play(attack3StateName);
            }
        }

        // 记录当前攻击延迟
        currentAttackDelay = attack3Delay;

        LogAnimatorDebugInfo("Attack3 start");

        yield return new WaitForSeconds(currentAttackDelay / 2f);

        Debug.Log("Attack3: resumed after first wait");

        if (IsPlayerInAttackRange(attack3Range))
        {
            if (!isHurting && !isDead)
            {
                DealDamage(attackDamage, attack3Range);
            }
            else
            {
                Debug.Log("⚠ Attack3 命中点被取消：Boss 当前处于受击或死亡状态");
            }
        }

        yield return new WaitForSeconds(currentAttackDelay / 2f);

        Debug.Log("Attack3: resumed after second wait");

        if (anim != null && HasParameter(attack3ParamName))
        {
            SetAnimatorParameterFalse(attack3ParamName);
        }

        isAttacking = false;
        attackAnimationPlaying = false;
        ForceUpdateAnimationState();

        // 连段最终完成后复位（可立即复位或等待外部控制）
        comboStage = 0;
        isAttack2Enabled = false;
        isAttack3Enabled = false;
        
    }

    // 强制触发的 Attack4（用于“模式1：最高优先级”）
    IEnumerator Attack4_Forced()
    {
        // 直接进入强制攻击流程（不检查 isAttacking）
        isAttacking = true;
        attackAnimationPlaying = true;
        attackAnimationTime = 0f;
        if (rb != null) rb.velocity = Vector2.zero;

        Debug.Log("💥 Boss 强制发动特殊攻击4！（Forced）");

        // 在启动前清理所有攻击参数
        ResetAllAttackParameters();
    // 标记强制 Attack4 正在运行，外部取消逻辑应跳过
    attack4ForcedActive = true;
        if (anim != null && HasParameter(attack4ParamName))
        {
            SetAnimatorParameterTrue(attack4ParamName);
            currentAnimationState = "Attack4";
            if (forcePlayStates && !string.IsNullOrEmpty(attack4StateName))
            {
                anim.Play(attack4StateName);
            }
            currentAttackDelay = attack4Delay;
        }

            // 等待命中特效时间点
            yield return new WaitForSeconds(currentAttackDelay / 2f);

            if (IsPlayerInAttackRange(attack4Range))
            {
                if (!isHurting && !isDead)
                {
                    DealDamage(attackDamage * 2, attack4Range);
                }
                else
                {
                    Debug.Log("⚠ Attack4 命中点被取消：Boss 当前处于受击或死亡状态");
                }
            }

            yield return new WaitForSeconds(currentAttackDelay / 2f);

            if (anim != null && HasParameter(attack4ParamName))
            {
                SetAnimatorParameterFalse(attack4ParamName);
            }

            // 强制结束，清理状态
            isAttacking = false;
            attackAnimationPlaying = false;
            ForceUpdateAnimationState();

            // 确保连段重置（Attack4 不在连段中）
            comboStage = 0;
            isAttack2Enabled = false;
            isAttack3Enabled = false;
        // 解除强制标记
        attack4ForcedActive = false;
    }

    // 常规 Attack4（保留以兼容测试与直接调用）
    IEnumerator Attack4()
    {
        if (isAttacking) yield break;

        isAttacking = true;
        attackAnimationPlaying = true;
        attackAnimationTime = 0f;
        if (rb != null) rb.velocity = Vector2.zero;

        Debug.Log("💥 Boss发动特殊攻击4！");

        if (anim != null && HasParameter(attack4ParamName))
        {
            SetAnimatorParameterTrue(attack4ParamName);
            currentAnimationState = "Attack4";
            currentAttackDelay = attack4Delay;
        }

        yield return new WaitForSeconds(currentAttackDelay / 2f);

        if (IsPlayerInAttackRange(attack4Range))
        {
            DealDamage(attackDamage * 2, attack4Range);
        }

    yield return new WaitForSeconds(currentAttackDelay / 2f);

        if (anim != null && HasParameter(attack4ParamName))
        {
            SetAnimatorParameterFalse(attack4ParamName);
        }

        isAttacking = false;
        attackAnimationPlaying = false;
        ForceUpdateAnimationState();
    }

    // DealDamage 现在接受 range 参数，确保命中判定和可视化一致
    void DealDamage(int damage, float range)
    {
        if (player == null) return;

        // 如果处于受击或已经死亡，取消任何伤害判定
        if (isHurting || isDead || !enemyAttributes.IsAlive)
        {
            Debug.Log("⚠ DealDamage 被取消：Boss 正在受击或已死亡");
            return;
        }

        Vector2 origin = GetAttackOrigin();
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, range, playerLayer);

        if (hits != null && hits.Length > 0)
        {
            foreach (Collider2D c in hits)
            {
                if (c == null) continue;
                Attribute attr = c.GetComponent<Attribute>() ?? c.GetComponentInParent<Attribute>() ?? c.GetComponentInChildren<Attribute>(true);
                if (attr != null)
                {
                    attr.TakeDamage(damage, gameObject);
                    Debug.Log($"💥 攻击命中 {c.name}，造成 {damage} 伤害");
                }
            }
        }
    }

    // 根据 attackPoint 或 偏移中心 返回实际检测 origin
    Vector2 GetAttackOrigin()
    {
        if (attackPoint != null) return attackPoint.position;
        // 使用基于 transform 的偏移中心（与 Gizmos 绘制一致）
        Vector3 offsetCenter = transform.position + (facingRight ? Vector3.right : Vector3.left) * attackOffset;
        return offsetCenter;
    }

    bool IsPlayerInDetectionRange()
    {
        if (player == null) return false;

        Vector2 center = detectionPoint != null ? (Vector2)detectionPoint.position : (Vector2)transform.position;
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, new Vector2(detectionWidth, detectionHeight), 0f, playerLayer);

        foreach (var hit in hits)
        {
            if (hit == null) continue;
            // compare tag rather than component type - compatible with different player setups
            if (hit.CompareTag("Player"))
            {
                return true;
            }
            // sometimes the collider may be child, check up parent
            var root = hit.transform.root;
            if (root != null && root.CompareTag("Player")) return true;
        }

        return false;
    }

    // IsPlayerInAttackRange 使用与 Gizmos 相同的 origin（attackPoint 或 偏移中心）
    bool IsPlayerInAttackRange(float range)
    {
        if (player == null) return false;

        Vector2 origin = GetAttackOrigin();
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, range, playerLayer);
        if (hits == null || hits.Length == 0) return false;

        foreach (var hit in hits)
        {
            if (hit == null) continue;
            if (hit.CompareTag("Player")) return true;
            if (hit.transform.root != null && hit.transform.root.CompareTag("Player")) return true;
        }
        return false;
    }

    // 精确距离判断：直接比较玩家碰撞体中心（或 root）与攻击原点的距离
    // 这个判定用于连段启动决策，避免 LayerMask/OverlapCircle 在复杂层级下漏判
    bool IsPlayerWithinRangeExact(float range)
    {
        if (player == null) return false;

        Vector2 origin = GetAttackOrigin();
        Vector2 playerPos = PlayerColliderCenter;

        float dist = Vector2.Distance(origin, playerPos);
        return dist <= range;
    }

    private void OnEnemyDeath()
    {
        Debug.Log("💀 EnemyBossAI: 接收到死亡事件");
        Die();
    }

    public void Die()
    {
        if (isDead) return;

        isDead = true;

        isAttacking = false;
        attackAnimationPlaying = false;
        isChasing = false;
        isKnockedBack = false;
        isHurting = false;

        if (rb != null) rb.velocity = Vector2.zero;

        if (anim != null && HasParameter(deadParamName))
        {
            anim.SetBool(deadParamName, true);
            currentAnimationState = "Death";
            if (forcePlayStates && !string.IsNullOrEmpty(deathStateName))
            {
                anim.Play(deathStateName);
            }
        }

        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
            collider.enabled = false;

        enabled = false;

        Debug.Log("💀 Boss死亡 - EnemyBossAI已禁用");

        StartCoroutine(DestroyAfterDeath());
    }

    private IEnumerator DestroyAfterDeath()
    {
        yield return new WaitForSeconds(2f);
        Destroy(gameObject);
    }

    public void ApplyWindKnockback(float force, bool fromRight)
    {
        if (isKnockedBack) return;

        float direction = fromRight ? -1f : 1f;
        StartCoroutine(KnockbackCoroutine(force, direction));
    }

    private IEnumerator KnockbackCoroutine(float force, float direction)
    {
        isKnockedBack = true;
        isAttacking = false;
        attackAnimationPlaying = false;
        isChasing = false;

        if (anim != null && HasParameter(hurtParamName))
        {
            anim.SetTrigger(hurtParamName);
            currentAnimationState = "Hurt";
        }

        float dir = Mathf.Clamp(direction, -1f, 1f);
        float elapsed = 0f;
        float knockbackSpeed = force / 0.3f;

        while (elapsed < 0.3f)
        {
            elapsed += Time.deltaTime;
            float moveStep = knockbackSpeed * Time.deltaTime;
            transform.position += new Vector3(dir * moveStep, 0, 0);
            yield return null;
        }

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }

        isKnockedBack = false;
        ForceUpdateAnimationState();
    }

    void FindAndSetupPlayer()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
            else
            {
                return;
            }
        }

        // 继续查找 root、碰撞体、属性组件
        FindPlayerRootAndAttributes();
    }

    // 当玩家离开检测范围时取消普通攻击并复位（不会中断强制 Attack4）
    void CancelAttacksDueToPlayerLoss()
    {
        // 如果当前没有攻击或动画播放，则无需处理
        if (!isAttacking && !attackAnimationPlaying) return;

        // 如果当前正在执行强制 Attack4，则不取消（Attack4_Forced 自行完成）
        if (attack4ForcedActive)
        {
            Debug.Log("🔒 正在执行强制 Attack4，跳过取消攻击逻辑");
            return;
        }

        Debug.Log("⛔ 玩家离开检测区：取消当前攻击并复位 Animator 参数");

        // 停止所有协程以中断普通攻击协程
    StopAllCoroutines();

    // 屏蔽动画事件短时间，避免动画事件在协程被停止后仍触发新的连段
    blockAEUntil = Time.time + 0.15f;

        // 清理连段状态
        comboStage = 0;
        isAttack2Enabled = false;
        isAttack3Enabled = false;

        // 复位标志
        isAttacking = false;
        attackAnimationPlaying = false;

        // 重置 Animator 中的攻击相关参数，避免残留继续触发动画
        ResetAllAttackParameters();

        // 作为最后防线：如果 Animator 存在，尝试切回 Idle 状态（减少因过渡/Clip设置导致的残留）
        if (anim != null && !string.IsNullOrEmpty(idleStateName))
        {
            try
            {
                anim.Play(idleStateName);
            }
            catch (System.Exception)
            {
                // ignore if state name invalid
            }
        }

        // 更新 Animator 到合适的空闲状态
        ForceUpdateAnimationState();
    }

    void FindPlayerRootAndAttributes()
    {
        if (player == null) return;

        playerRoot = null;

        if (player.name.Contains("Visual") || player.name.Contains("visual"))
        {
            // 尝试取 root/parent
            playerRoot = player.root;
            if (playerRoot == player || playerRoot == null)
            {
                playerRoot = player.parent;
            }
        }

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

        if (playerRoot == null)
        {
            Attribute attr = player.GetComponentInParent<Attribute>();
            if (attr != null)
            {
                playerRoot = attr.transform;
            }
        }

        if (playerRoot == null)
        {
            Transform current = player;
            while (current.parent != null)
            {
                current = current.parent;
            }
            playerRoot = current;
        }

        if (playerRoot == null)
        {
            playerRoot = player;
        }

        // 获取 BoxCollider2D（显式 cast 问题已修复）
        playerCollider = playerRoot.GetComponent<BoxCollider2D>();
        if (playerCollider == null)
        {
            playerCollider = player.GetComponentInParent<BoxCollider2D>();
            if (playerCollider == null)
            {
                playerCollider = player.GetComponentInChildren<BoxCollider2D>();
            }
        }

        playerAttributes = playerRoot.GetComponent<Attribute>();
        if (playerAttributes == null)
        {
            playerAttributes = playerRoot.GetComponentInChildren<Attribute>(true);
        }
        if (playerAttributes == null)
        {
            playerAttributes = player.GetComponentInParent<Attribute>();
        }
    }

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

    // Debug helper: 打印当前 Animator layer0 的状态和参数（仅用于调试）
    void LogAnimatorDebugInfo(string context)
    {
        if (anim == null) 
        {
            Debug.Log($"[AnimatorDebug] {context}: anim is null");
            return;
        }

        try
        {
            AnimatorStateInfo st = anim.GetCurrentAnimatorStateInfo(0);
            Debug.Log($"[AnimatorDebug] {context}: stateHash={st.shortNameHash}, normalizedTime={st.normalizedTime:F2}");

            foreach (var p in anim.parameters)
            {
                string val = "n/a";
                switch (p.type)
                {
                    case AnimatorControllerParameterType.Bool:
                        val = anim.GetBool(p.name).ToString();
                        break;
                    case AnimatorControllerParameterType.Float:
                        val = anim.GetFloat(p.name).ToString("F2");
                        break;
                    case AnimatorControllerParameterType.Int:
                        val = anim.GetInteger(p.name).ToString();
                        break;
                    case AnimatorControllerParameterType.Trigger:
                        // Trigger 没有读取接口，输出存在性即可
                        val = "(trigger)";
                        break;
                }
                Debug.Log($"[AnimatorDebug] param: {p.name} ({p.type}) = {val}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.Log($"[AnimatorDebug] {context}: Exception while dumping animator info: {ex.Message}");
        }
    }

    // Helper: 根据 Animator 参数的类型设置为 true（Trigger -> SetTrigger, Bool -> SetBool(true), Int/Float -> SetInteger/SetFloat）
    void SetAnimatorParameterTrue(string paramName)
    {
        if (anim == null) return;
        foreach (var p in anim.parameters)
        {
            if (p.name != paramName) continue;
            switch (p.type)
            {
                case AnimatorControllerParameterType.Bool:
                    anim.SetBool(paramName, true);
                    return;
                case AnimatorControllerParameterType.Trigger:
                    anim.SetTrigger(paramName);
                    return;
                case AnimatorControllerParameterType.Int:
                    anim.SetInteger(paramName, 1);
                    return;
                case AnimatorControllerParameterType.Float:
                    anim.SetFloat(paramName, 1f);
                    return;
            }
        }
    }

    // Helper: 根据 Animator 参数的类型设置为 false / 重置（Trigger -> ResetTrigger, Bool -> SetBool(false)）
    void SetAnimatorParameterFalse(string paramName)
    {
        if (anim == null) return;
        foreach (var p in anim.parameters)
        {
            if (p.name != paramName) continue;
            switch (p.type)
            {
                case AnimatorControllerParameterType.Bool:
                    anim.SetBool(paramName, false);
                    return;
                case AnimatorControllerParameterType.Trigger:
                    anim.ResetTrigger(paramName);
                    return;
                case AnimatorControllerParameterType.Int:
                    anim.SetInteger(paramName, 0);
                    return;
                case AnimatorControllerParameterType.Float:
                    anim.SetFloat(paramName, 0f);
                    return;
            }
        }
    }

    // Helper: 重置所有攻击相关的 Animator 参数，确保没有残留触发
    void ResetAllAttackParameters()
    {
        if (anim == null) return;
        if (HasParameter(attack1ParamName)) SetAnimatorParameterFalse(attack1ParamName);
        if (HasParameter(attack2ParamName)) SetAnimatorParameterFalse(attack2ParamName);
        if (HasParameter(attack3ParamName)) SetAnimatorParameterFalse(attack3ParamName);
        if (HasParameter(attack4ParamName)) SetAnimatorParameterFalse(attack4ParamName);
    }

    // 动画事件：Attack1 动画结束处调用以允许检测 Attack2（严格使用动画事件触发）
    public void AE_EnableAttack2()
    {
        isAttack2Enabled = true;
        // 动画事件触发时尽量立即开启下一段连段，避免回到 Idle 一帧的间隙。
        // 如果当前处于 comboStage==1（Attack1 已触发），立即启动 Attack2 的协程并推进 comboStage，
        // 注意：Attack2 协程内部会重置 Attack1 的参数以确保 Animator 能直接过渡。
        // 额外防护：只有当玩家仍在检测区且没有强制 Attack4 时才允许动画事件启动下一段
        // 如果短期内被屏蔽（例如刚刚取消攻击或切换到强制 Attack4），阻止 AE 启动
        if (Time.time < blockAEUntil)
        {
            Debug.Log("AE_EnableAttack2 阻止：短时屏蔽中");
            return;
        }

        if (!IsPlayerInDetectionRange())
        {
            Debug.Log("AE_EnableAttack2 阻止：玩家已离开检测区");
            return;
        }

        if (attack4ForcedActive)
        {
            Debug.Log("AE_EnableAttack2 阻止：强制 Attack4 正在运行");
            return;
        }

        if (comboStage == 1)
        {
            comboStage = 2; // 预先推进连段阶段，表示下一段正在开始
            StartCoroutine(Attack2());
        }
    }

    // 动画事件：Attack2 动画结束处调用以允许检测 Attack3（严格使用动画事件触发）
    public void AE_EnableAttack3()
    {
        isAttack3Enabled = true;
        // 如果短期内被屏蔽（例如刚刚取消攻击或切换到强制 Attack4），阻止 AE 启动
        if (Time.time < blockAEUntil)
        {
            Debug.Log("AE_EnableAttack3 阻止：短时屏蔽中");
            return;
        }

        // 额外防护：只有当玩家仍在检测区且没有强制 Attack4 时才允许动画事件启动下一段
        if (!IsPlayerInDetectionRange())
        {
            Debug.Log("AE_EnableAttack3 阻止：玩家已离开检测区");
            return;
        }

        if (attack4ForcedActive)
        {
            Debug.Log("AE_EnableAttack3 阻止：强制 Attack4 正在运行");
            return;
        }

        if (comboStage == 2)
        {
            comboStage = 3;
            StartCoroutine(Attack3());
        }
    }

    [ContextMenu("测试攻击1")]
    private void TestAttack1()
    {
        if (!isAttacking && !attackAnimationPlaying)
        {
            StartCoroutine(Attack1());
        }
    }

    [ContextMenu("测试攻击2")]
    private void TestAttack2()
    {
        if (!isAttacking && !attackAnimationPlaying)
        {
            StartCoroutine(Attack2());
        }
    }

    [ContextMenu("测试攻击3")]
    private void TestAttack3()
    {
        if (!isAttacking && !attackAnimationPlaying)
        {
            StartCoroutine(Attack3());
        }
    }

    [ContextMenu("测试攻击4")]
    private void TestAttack4()
    {
        if (!isAttacking && !attackAnimationPlaying)
        {
            StartCoroutine(Attack4());
        }
    }

    [ContextMenu("测试受击")]
    private void TestHurt()
    {
        TriggerHurtAnimation();
    }

    [ContextMenu("测试死亡")]
    private void TestDeath()
    {
        Die();
    }

    [ContextMenu("显示Boss状态")]
    private void ShowBossStatus()
    {
        if (enemyAttributes != null)
        {
            Debug.Log($"=== Boss状态 ===");
            Debug.Log($"生命值: {enemyAttributes.CurrentHealth}/{enemyAttributes.MaxHealth}");
            Debug.Log($"连续受击: {consecutiveHits}/{consecutiveHitsToTriggerAttack4}");
            Debug.Log($"攻击1范围: {attack1Range}");
            Debug.Log($"攻击2范围: {attack2Range}");
            Debug.Log($"攻击3范围: {attack3Range}");
            Debug.Log($"攻击4范围: {attack4Range}");
            Debug.Log($"当前动画: {currentAnimationState}");
            Debug.Log($"comboStage: {comboStage}, isAttack2Enabled: {isAttack2Enabled}, isAttack3Enabled: {isAttack3Enabled}");
            Debug.Log($"isChasing: {isChasing}, facingRight: {facingRight}");
        }
    }

    private void OnDrawGizmos()
    {
        // 绘制 detection 区域（矩形），以及所有 attack range（以偏移中心为中心）
        Vector2 origin = (attackPoint != null) ? (Vector2)attackPoint.position : (Vector2)transform.position;

        // 绘制检测范围（椭圆/矩形混合 - 使用矩形可视化）
        Gizmos.color = new Color(0f, 0f, 1f, 0.15f);
        Vector3 detectCenter = detectionPoint != null ? detectionPoint.position : transform.position;
        Gizmos.DrawCube(detectCenter, new Vector3(detectionWidth, detectionHeight, 0.1f));
        Gizmos.color = new Color(0f, 0f, 1f, 0.35f);
        DrawEllipseGizmo(transform.position, detectionWidth, detectionHeight, 64);

        // 显示攻击点位置（原位置）
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.9f);
        Gizmos.DrawSphere(origin, 0.06f);

        // 计算基于 transform.position 的偏移中心（attackOffset）
        Vector3 offsetCenter = transform.position + (facingRight ? Vector3.right : Vector3.left) * attackOffset;

        // 绘制偏移后的攻击范围（Attack1 ~ Attack4）以 offsetCenter 为中心
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.35f); // attack1
        Gizmos.DrawWireSphere(offsetCenter, attack1Range);

        Gizmos.color = new Color(1f, 0.5f, 0.2f, 0.35f); // attack2
        Gizmos.DrawWireSphere(offsetCenter, attack2Range);

        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.35f); // attack3
        Gizmos.DrawWireSphere(offsetCenter, attack3Range);

        Gizmos.color = new Color(1f, 0.0f, 0.8f, 0.35f); // attack4
        Gizmos.DrawWireSphere(offsetCenter, attack4Range);

        // 在 offsetCenter 处画一个小球指出偏移中心
        Gizmos.color = new Color(0f, 1f, 0f, 0.9f);
        Gizmos.DrawSphere(offsetCenter, 0.04f);

        // 墙体检测点显示
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

        // 朝向指示
        Gizmos.color = facingRight ? Color.green : Color.red;
        Vector3 directionIndicator = transform.position + (facingRight ? Vector3.right : Vector3.left) * 0.8f;
        Gizmos.DrawWireSphere(directionIndicator, 0.2f);

        // 运行时显示玩家位置与连线
        if (Application.isPlaying && player != null)
        {
            Vector2 playerPos = PlayerColliderCenter;
            Gizmos.color = isChasing ? Color.red : Color.yellow;
            Gizmos.DrawLine(transform.position, playerPos);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(playerPos, 0.5f);
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
