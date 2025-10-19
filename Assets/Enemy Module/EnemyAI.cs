using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("移动参数")]
    public float moveSpeed = 3f;
    public float chaseSpeed = 4.5f;
    public float wallCheckDistance = 0.2f;
    public LayerMask wallLayer;

    [Header("检测与攻击参数")]
    public float detectionRange = 10f;
    public float attackRange = 2f;
    public LayerMask attackLayer;
    public float attackDelay = 0.5f;
    public int attackDamage = 10;

    [Header("检测点设置")]
    public Transform detectionPoint;
    public Transform attackPoint;
    public Transform wallCheck;

    [Header("玩家设置")]
    public Transform player;

    private Rigidbody2D rb;
    private bool isAttacking = false;
    private bool isChasing = false;
    private bool facingRight = true;
    private bool hitWall = false;

    // 防止抽搐：敌人只有当玩家在一定距离外才翻转
    private float flipThreshold = 0.5f;

    private void Start()
    {
        if (player == null)
        {
            player = GameObject.FindWithTag("Player")?.transform;
            if (player == null)
                Debug.LogError("⚠ 找不到标记为 'Player' 的对象！");
        }

        rb = GetComponent<Rigidbody2D>();
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.freezeRotation = true;
    }

    private void Update()
    {
        if (isAttacking) return;

        bool playerDetected = IsPlayerInDetectionRange();

        if (playerDetected && !isChasing)
        {
            isChasing = true;
            Debug.Log("🎯 玩家进入检测范围，开始追击！");
        }
        else if (!playerDetected && isChasing)
        {
            isChasing = false;
            Debug.Log("🚶 玩家离开范围，恢复巡逻");
        }

        if (IsPlayerInAttackRange() && !isAttacking)
        {
            StartCoroutine(AttackPlayer());
        }
    }

    private void FixedUpdate()
    {
        if (isAttacking)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        CheckWall();

        if (isChasing && player != null)
        {
            ChasePlayer();
        }
        else
        {
            Patrol();
        }
    }

    // ✅ 巡逻逻辑
    void Patrol()
    {
        if (hitWall)
        {
            Flip(!facingRight);
            hitWall = false;
        }

        float moveDir = facingRight ? 1f : -1f;
        rb.velocity = new Vector2(moveDir * moveSpeed, rb.velocity.y);
    }

    // ✅ 改进追击逻辑
    void ChasePlayer()
    {
        if (player == null) return;

        float direction = player.position.x - transform.position.x;

        // 🟩 1️⃣ 防止抽搐：玩家移动距离超过一定阈值才翻转
        if (Mathf.Abs(direction) > flipThreshold)
        {
            bool shouldFaceRight = direction > 0;
            if (shouldFaceRight != facingRight)
                Flip(shouldFaceRight);
        }

        // 🟩 2️⃣ 检查前方是否有墙体，避免穿墙
        Vector2 dir = facingRight ? Vector2.right : Vector2.left;
        RaycastHit2D wallHit = Physics2D.Raycast(wallCheck.position, dir, wallCheckDistance, wallLayer);
        bool blocked = wallHit.collider != null;

        if (blocked)
        {
            rb.velocity = new Vector2(0, rb.velocity.y); // 停下防止穿墙
            Debug.Log("🧱 敌人检测到墙体，停止前进");
        }
        else
        {
            // 🟩 3️⃣ 敌人沿着 X 方向靠近玩家
            float moveDir = Mathf.Sign(direction);
            rb.velocity = new Vector2(moveDir * chaseSpeed, rb.velocity.y);
        }
    }

    // ✅ 翻转逻辑
    void Flip(bool faceRight)
    {
        facingRight = faceRight;
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (facingRight ? 1 : -1);
        transform.localScale = scale;
    }

    // ✅ 检测墙（仅巡逻时用）
    void CheckWall()
    {
        if (wallCheck == null) return;

        Vector2 dir = facingRight ? Vector2.right : Vector2.left;
        RaycastHit2D hit = Physics2D.Raycast(wallCheck.position, dir, wallCheckDistance, wallLayer);
        hitWall = hit.collider != null && !isChasing;
    }

    // ✅ 攻击逻辑
    System.Collections.IEnumerator AttackPlayer()
    {
        isAttacking = true;
        rb.velocity = Vector2.zero;
        Debug.Log("⚔ 敌人发动攻击！");

        yield return new WaitForSeconds(attackDelay / 2f);

        if (IsPlayerInAttackRange())
        {
            Attribute playerAttr = player.GetComponent<Attribute>();
            if (playerAttr != null)
                playerAttr.TakeDamage(attackDamage);
            Debug.Log($"💥 攻击命中，造成 {attackDamage} 伤害！");
        }

        yield return new WaitForSeconds(attackDelay / 2f);
        isAttacking = false;
    }

    // ✅ 检测玩家是否在范围内
    bool IsPlayerInDetectionRange()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(detectionPoint.position, detectionRange, LayerMask.GetMask("Player"));
        return hits.Length > 0;
    }

    // ✅ 检测玩家是否在攻击范围内
    bool IsPlayerInAttackRange()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, LayerMask.GetMask("Player"));
        return hits.Length > 0;
    }

    private void OnDrawGizmos()
    {
        if (detectionPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(detectionPoint.position, detectionRange);
        }
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
        if (wallCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(wallCheck.position,
                wallCheck.position + (facingRight ? Vector3.right : Vector3.left) * wallCheckDistance);
        }
    }
}
