using UnityEngine;
using System.Collections;

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

    private Rigidbody2D rb;
    private bool isAttacking = false;
    private bool isChasing = false;
    private bool facingRight = true;
    private bool hitWall = false;
    private bool isKnockedBack = false;
    private float flipThreshold = 0.5f;

    private SpriteRenderer sprite; // ✅ 专门用于控制翻转外观

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponentInChildren<SpriteRenderer>();

        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.freezeRotation = true;

        if (player == null)
        {
            player = GameObject.FindWithTag("Player")?.transform;
            if (player == null)
                Debug.LogError("⚠ 找不到标记为 'Player' 的对象！");
        }
    }

    private void Update()
    {
        if (isAttacking || isKnockedBack) return;

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

        if (playerDetected)
        {
            float xDiff = player.position.x - transform.position.x;
            bool playerOnRight = xDiff > 0;
            if (playerOnRight != facingRight)
                Flip(playerOnRight);
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

        if (isKnockedBack)
            return;

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

    void ChasePlayer()
    {
        if (player == null) return;

        float xDiff = player.position.x - transform.position.x;

        if (Mathf.Abs(xDiff) > flipThreshold)
        {
            bool shouldFaceRight = xDiff > 0;
            if (shouldFaceRight != facingRight)
                Flip(shouldFaceRight);
        }

        if (Mathf.Abs(xDiff) < 3f)
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
            return;
        }

        // ✅ 根据朝向选择检测点
        Transform checkPoint = facingRight ? wallCheckRight : wallCheckLeft;
        Vector2 dir = facingRight ? Vector2.right : Vector2.left;

        RaycastHit2D wallHit = Physics2D.Raycast(checkPoint.position, dir, wallCheckDistance, wallLayer);
        bool blocked = wallHit.collider != null;

        if (blocked)
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
            Debug.Log("🧱 追击时检测到墙体，停止前进");
        }
        else
        {
            float moveDir = Mathf.Sign(xDiff);
            rb.velocity = new Vector2(moveDir * chaseSpeed, rb.velocity.y);
        }
    }

    // ✅ 翻转视觉外观，而不是整体缩放
    void Flip(bool faceRight)
    {
        facingRight = faceRight;

        // ✅ 只翻转Sprite，不改变Transform坐标系
        if (sprite != null)
            sprite.flipX = !faceRight;
    }

    // ✅ 检查墙体（巡逻用）
    void CheckWall()
    {
        Transform checkPoint = facingRight ? wallCheckRight : wallCheckLeft;
        if (checkPoint == null) return;

        Vector2 dir = facingRight ? Vector2.right : Vector2.left;
        RaycastHit2D hit = Physics2D.Raycast(checkPoint.position, dir, wallCheckDistance, wallLayer);
        hitWall = hit.collider != null && !isChasing;
    }

    IEnumerator AttackPlayer()
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

    bool IsPlayerInDetectionRange()
    {
        if (player == null || detectionPoint == null) return false;

        Vector2 offset = player.position - detectionPoint.position;
        float ellipseValue =
            (offset.x * offset.x) / (detectionWidth * detectionWidth / 4f) +
            (offset.y * offset.y) / (detectionHeight * detectionHeight / 4f);

        return ellipseValue <= 1f;
    }

    bool IsPlayerInAttackRange()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, playerLayer);
        return hits.Length > 0;
    }

    public void ApplyWindKnockback(float force, bool fromRight)
    {
        if (isKnockedBack) return;
        StartCoroutine(KnockbackCoroutine(force, fromRight));
    }

    IEnumerator KnockbackCoroutine(float force, bool fromRight)
    {
        isKnockedBack = true;
        isAttacking = false;
        isChasing = false;

        float dir = fromRight ? 1f : -1f;
        float originalY = transform.position.y;
        float elapsed = 0f;

        float knockbackSpeed = force / windKnockbackDuration;

        Debug.Log($"🌀 击退开始：方向={(fromRight ? "右" : "左")}, 力量={force}");

        while (elapsed < windKnockbackDuration)
        {
            elapsed += Time.deltaTime;

            float moveStep = knockbackSpeed * Time.deltaTime;
            Vector2 moveDir = new Vector2(dir, 0f);

            // ✅ 使用 Rigidbody2D.Cast 进行预测性碰撞检测
            // 这个方法会检测整个刚体在移动过程中是否会碰撞，比单点射线更可靠
            RaycastHit2D[] hits = new RaycastHit2D[5];
            int hitCount = rb.Cast(moveDir, hits, moveStep);

            // 检查是否会撞到墙体
            bool willHitWall = false;
            float minDistance = moveStep;

            for (int i = 0; i < hitCount; i++)
            {
                // 检查碰撞对象是否在墙体层
                if (((1 << hits[i].collider.gameObject.layer) & wallLayer) != 0)
                {
                    willHitWall = true;
                    minDistance = Mathf.Min(minDistance, hits[i].distance);
                }
            }

            if (willHitWall)
            {
                // 🧱 如果会撞到墙体，只移动到墙体前的安全距离
                float safeDistance = Mathf.Max(0, minDistance - 0.05f); // 留出0.05的安全间距
                if (safeDistance > 0.001f)
                {
                    transform.position = new Vector3(
                        transform.position.x + dir * safeDistance,
                        originalY,
                        transform.position.z
                    );
                }
                Debug.Log("🧱 击退中检测到墙体，停止击退");
                break;
            }

            // ✅ 没有撞墙 → 正常移动
            transform.position = new Vector3(
                transform.position.x + dir * moveStep,
                originalY,
                transform.position.z
            );

            yield return null;
        }

        rb.velocity = Vector2.zero;
        isKnockedBack = false;
        Debug.Log("✅ 击退结束");
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
