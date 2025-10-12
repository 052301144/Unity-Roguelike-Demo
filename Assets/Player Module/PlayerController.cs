using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{

    [Header("Components")]
    public Rigidbody2D rb;
    public Collider2D bodyCollider;

    // 移动功能的参数
    [Header("Movement")]
    public float maxSpeed = 6f;           // 最大速度
    public float accel = 40f;             // 加速度（按下按键时用于速度上升）
    public float decel = 80f;             // 减速度（松开按键时快速停止）
    public float groundMoveSpeed = 6f;    // 固定移动速度（初期开发暂用）
    [Tooltip("允许占位: 如果启用 will use accel/decel to reach maxSpeed")]
    public bool useAcceleration = true;

    // 跳跃功能的参数
    [Header("Jump")]
    public float jumpForce = 20f;         // 跳跃初速度
    public bool allowDoubleJump = false;  // 预留双段跳
    public float highJumpMultiplier = 1.5f; // 预留高跳倍率

    // 下落功能的参数（暂用，后续更换）
    [Header("Fall")]
    public float fallSpeed = -10f;        // 固定下落速度（负值），人物暂时没有gravity
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.1f;
    public Vector2 groundCheckBoxSize = new Vector2(0.5f, 0.05f);

    // 检测角色前方是否有墙壁
    [Header("Wall Detection")]
    public float wallCheckDistance = 0.1f; // 前方墙检测射线长度
    public Vector2 wallCheckOffset = new Vector2(0.5f, 0f); // 相对于角色中心的偏移（水平）

    // 技能功能预留按键
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

        float skin = 0.02f;
        float overlapHeight = 0.12f;
        float width = Mathf.Max(0.1f, bodyCollider.bounds.size.x * 0.9f);
        Vector2 size = new Vector2(width, overlapHeight);

        // 使用 bodyCollider.bounds 直接计算底边位置（world space）
        float bottomY = bodyCollider.bounds.min.y; // world y 最小值 = 碰撞箱底部
        Vector2 center = new Vector2(bodyCollider.bounds.center.x, bottomY - (overlapHeight * 0.5f) + skin);

        Collider2D hit = Physics2D.OverlapBox(center, size, 0f, groundLayer);
        bool verticalOk = rb == null ? true : rb.velocity.y <= 0.1f;
        isGrounded = (hit != null) && verticalOk;
        if (isGrounded) doubleJumpUsed = false;

        // 可视化：使用 Debug.DrawLine（短时）或 OnDrawGizmosSelected 中的 Gizmos.DrawWireCube
        Color c = isGrounded ? Color.green : Color.red;
        Debug.DrawLine(center + Vector2.left * size.x * 0.5f, center + Vector2.right * size.x * 0.5f, c, 0.02f);
        Debug.DrawLine(center + Vector2.up * size.y * 0.5f, center + Vector2.down * size.y * 0.5f, c, 0.02f);
        if (hit != null) Debug.Log($"GroundHit: {hit.name}");
    }

    bool IsWallAhead()
    {
        Vector2 origin = (Vector2)transform.position + new Vector2(wallCheckOffset.x * facing, wallCheckOffset.y);
        RaycastHit2D hit = Physics2D.Raycast(origin, new Vector2(facing, 0f), wallCheckDistance, groundLayer);
        return hit.collider != null;
    }

    void HandleMovement()
    {
        // 如果前方有墙且与人物水平线对齐，则不能移动向墙方向
        bool wallAhead = IsWallAhead();

        float targetVelX = 0f;
        if (!wallAhead)
        {
            // 基于 inputX 计算目标速度
            targetVelX = inputX * groundMoveSpeed;
        }
        else
        {
            // 若试图朝墙方向移动，速度为 0；反方向仍然允许移动
            if (inputX * facing > 0) targetVelX = 0f;
            else targetVelX = inputX * groundMoveSpeed;
        }

        float currentVX = rb.velocity.x;
        float newVX = currentVX;

        if (!useAcceleration)
        {
            // 直接设定速度（固定移动）
            newVX = targetVelX;
        }
        else
        {
            // 使用加速度/减速度平滑过渡至目标速度
            if (Mathf.Abs(targetVelX) > 0.01f)
            {
                // 正在按移动键，加速
                float sign = Mathf.Sign(targetVelX - currentVX);
                newVX = currentVX + Mathf.Clamp(sign * accel * Time.fixedDeltaTime, -Mathf.Abs(targetVelX - currentVX), Mathf.Abs(targetVelX - currentVX));
            }
            else
            {
                // 松开按键，减速到 0
                float sign = Mathf.Sign(0 - currentVX);
                newVX = currentVX + Mathf.Clamp(sign * decel * Time.fixedDeltaTime, -Mathf.Abs(currentVX), Mathf.Abs(currentVX));
            }

            // 限制最大速度
            newVX = Mathf.Clamp(newVX, -maxSpeed, maxSpeed);
        }

        rb.velocity = new Vector2(newVX, rb.velocity.y);
    }
    
    void HandleJump()
    {
        if(!wantJump) return;

        if (isGrounded)
        {
            DoJump();
            wantJump = false;
        }else if(allowDoubleJump && !doubleJumpUsed)
        {
            doubleJumpUsed = true;
            DoJump();
            wantJump = false;
        }
        else { }
    }

    void DoJump()
    {
        // 应用垂直速度，预留高跳乘数（如果实现高跳，这里乘上）
        float appliedJump = jumpForce * highJumpMultiplier;
        Debug.Log($"DoJump appliedJump={appliedJump}");
        Vector2 v = rb.velocity;
        v.y = appliedJump;
        rb.velocity = v;
    }

    /*
    void HandleFallFix()
    {
        // 当空中且我们想要固定下落速度，强制下落速度（如果需要）
        if (!isGrounded)
        {
            if (rb.velocity.y < fallSpeed) // fallSpeed 是负值，更低表示更快下落
            {
                Vector2 v = rb.velocity;
                v.y = fallSpeed;
                rb.velocity = v;
            }
        }
    }
    */

    // 技能占位方法：目前只记录按键并可扩展
    void OnDashSkill()
    {
        // 位移技能占位：直接向 facing 方向快速位移（例子，短距离瞬移）
        float dashDistance = 2f;
        Vector3 target = transform.position + Vector3.right * facing * dashDistance;

        // 简单的位移实现（不穿墙检测），实际项目应检测碰撞/耗蓝/冷却
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
        // 确保核心组件存在
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (bodyCollider == null) bodyCollider = GetComponent<Collider2D>();

        if (rb == null)
        {
            Debug.LogError("[PlayerController] Rigidbody2D 未找到，请检查组件", this);
            enabled = false;
            return;
        }
        if (bodyCollider == null)
        {
            Debug.LogError("[PlayerController] Collider2D 未找到，请检查组件", this);
            enabled = false;
            return;
        }

        // 校正参数（防止配置异常）
        maxSpeed = Mathf.Max(0.01f, maxSpeed);
        accel = Mathf.Max(0f, accel);
        decel = Mathf.Max(0f, decel);
        groundMoveSpeed = Mathf.Clamp(groundMoveSpeed, 0f, maxSpeed);

        // 缓存常用值（可选），例如脚下检测起点偏移
        // cachedExtentY = bodyCollider.bounds.extents.y; // 如果在 CheckGround 中使用，可提前缓存

        // 其它启动时逻辑占位
    }

    // Update is called once per frame
    void Update()
    {
        // 读取输入（在 Update 里）
        inputX = 0f;
        if (Input.GetKey(KeyCode.A)) inputX -= 1f;
        if (Input.GetKey(KeyCode.D)) inputX += 1f;

        if (inputX > 0) facing = 1;
        else if (inputX < 0) facing = -1;

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
        HandleMovement();
        Debug.Log($"[Fixed] wantJump={wantJump} isGrounded={isGrounded} velY={rb.velocity.y}");
        HandleJump();
        // HandleFallFix();
        wantJump = false; // Reset after physics step
    }

    // 可视化检测范围（调试）
    void OnDrawGizmosSelected()
    {
        if (bodyCollider == null) return;

        // 与 CheckGround 使用相同的参数和计算方式，保证可视化与检测一致
        float skin = 0.02f;
        float overlapHeight = 0.12f;
        float width = Mathf.Max(0.1f, bodyCollider.bounds.size.x * 0.9f);
        Vector3 size = new Vector3(width, overlapHeight, 0f);

        float bottomY = bodyCollider.bounds.min.y;
        Vector3 center = new Vector3(bodyCollider.bounds.center.x, bottomY - (overlapHeight * 0.5f) + skin, transform.position.z);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(center, size);
    }
}
