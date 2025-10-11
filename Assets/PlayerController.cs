using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{

    [Header("Components")]
    public Rigidbody2D rb;
    public Collider2D bodyCollider;

    // �ƶ����ܵĲ���
    [Header("Movement")]
    public float maxSpeed = 6f;           // ����ٶ�
    public float accel = 40f;             // ���ٶȣ����°���ʱ�����ٶ�������
    public float decel = 80f;             // ���ٶȣ��ɿ�����ʱ����ֹͣ��
    public float groundMoveSpeed = 6f;    // �̶��ƶ��ٶȣ����ڿ������ã�
    [Tooltip("����ռλ: ������� will use accel/decel to reach maxSpeed")]
    public bool useAcceleration = true;

    // ��Ծ���ܵĲ���
    [Header("Jump")]
    public float jumpForce = 20f;         // ��Ծ���ٶ�
    public bool allowDoubleJump = false;  // Ԥ��˫����
    public float highJumpMultiplier = 1.5f; // Ԥ����������

    // ���书�ܵĲ��������ã�����������
    [Header("Fall")]
    public float fallSpeed = -10f;        // �̶������ٶȣ���ֵ����������ʱû��gravity
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.1f;
    public Vector2 groundCheckBoxSize = new Vector2(0.5f, 0.05f);

    // ����ɫǰ���Ƿ���ǽ��
    [Header("Wall Detection")]
    public float wallCheckDistance = 0.1f; // ǰ��ǽ������߳���
    public Vector2 wallCheckOffset = new Vector2(0.5f, 0f); // ����ڽ�ɫ���ĵ�ƫ�ƣ�ˮƽ��

    // ���ܹ���Ԥ������
    [Header("Skills (placeholders)")]
    public KeyCode dashKey = KeyCode.L;   // λ�Ƽ���ռλ
    public KeyCode skill1Key = KeyCode.U;
    public KeyCode skill2Key = KeyCode.I;
    public KeyCode skill3Key = KeyCode.O;

    // �ڲ�״̬
    private float inputX;
    private bool wantJump;
    private bool isGrounded;
    private bool doubleJumpUsed;
    private int facing = 1; // 1 ��, -1 �����ڱ�ʾ��ɫ�ĳ���

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

        // ʹ�� bodyCollider.bounds ֱ�Ӽ���ױ�λ�ã�world space��
        float bottomY = bodyCollider.bounds.min.y; // world y ��Сֵ = ��ײ��ײ�
        Vector2 center = new Vector2(bodyCollider.bounds.center.x, bottomY - (overlapHeight * 0.5f) + skin);

        Collider2D hit = Physics2D.OverlapBox(center, size, 0f, groundLayer);
        bool verticalOk = rb == null ? true : rb.velocity.y <= 0.1f;
        isGrounded = (hit != null) && verticalOk;
        if (isGrounded) doubleJumpUsed = false;

        // ���ӻ���ʹ�� Debug.DrawLine����ʱ���� OnDrawGizmosSelected �е� Gizmos.DrawWireCube
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
        // ���ǰ����ǽ��������ˮƽ�߶��룬�����ƶ���ǽ����
        bool wallAhead = IsWallAhead();

        float targetVelX = 0f;
        if (!wallAhead)
        {
            // ���� inputX ����Ŀ���ٶ�
            targetVelX = inputX * groundMoveSpeed;
        }
        else
        {
            // ����ͼ��ǽ�����ƶ����ٶ�Ϊ 0����������Ȼ�����ƶ�
            if (inputX * facing > 0) targetVelX = 0f;
            else targetVelX = inputX * groundMoveSpeed;
        }

        float currentVX = rb.velocity.x;
        float newVX = currentVX;

        if (!useAcceleration)
        {
            // ֱ���趨�ٶȣ��̶��ƶ���
            newVX = targetVelX;
        }
        else
        {
            // ʹ�ü��ٶ�/���ٶ�ƽ��������Ŀ���ٶ�
            if (Mathf.Abs(targetVelX) > 0.01f)
            {
                // ���ڰ��ƶ���������
                float sign = Mathf.Sign(targetVelX - currentVX);
                newVX = currentVX + Mathf.Clamp(sign * accel * Time.fixedDeltaTime, -Mathf.Abs(targetVelX - currentVX), Mathf.Abs(targetVelX - currentVX));
            }
            else
            {
                // �ɿ����������ٵ� 0
                float sign = Mathf.Sign(0 - currentVX);
                newVX = currentVX + Mathf.Clamp(sign * decel * Time.fixedDeltaTime, -Mathf.Abs(currentVX), Mathf.Abs(currentVX));
            }

            // ��������ٶ�
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
        // Ӧ�ô�ֱ�ٶȣ�Ԥ���������������ʵ�ָ�����������ϣ�
        float appliedJump = jumpForce * highJumpMultiplier;
        Debug.Log($"DoJump appliedJump={appliedJump}");
        Vector2 v = rb.velocity;
        v.y = appliedJump;
        rb.velocity = v;
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

    // ����ռλ������Ŀǰֻ��¼����������չ
    void OnDashSkill()
    {
        // λ�Ƽ���ռλ��ֱ���� facing �������λ�ƣ����ӣ��̾���˲�ƣ�
        float dashDistance = 2f;
        Vector3 target = transform.position + Vector3.right * facing * dashDistance;

        // �򵥵�λ��ʵ�֣�����ǽ��⣩��ʵ����ĿӦ�����ײ/����/��ȴ
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
        // ȷ�������������
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (bodyCollider == null) bodyCollider = GetComponent<Collider2D>();

        if (rb == null)
        {
            Debug.LogError("[PlayerController] Rigidbody2D δ�ҵ����������", this);
            enabled = false;
            return;
        }
        if (bodyCollider == null)
        {
            Debug.LogError("[PlayerController] Collider2D δ�ҵ����������", this);
            enabled = false;
            return;
        }

        // У����������ֹ�����쳣��
        maxSpeed = Mathf.Max(0.01f, maxSpeed);
        accel = Mathf.Max(0f, accel);
        decel = Mathf.Max(0f, decel);
        groundMoveSpeed = Mathf.Clamp(groundMoveSpeed, 0f, maxSpeed);

        // ���泣��ֵ����ѡ����������¼�����ƫ��
        // cachedExtentY = bodyCollider.bounds.extents.y; // ����� CheckGround ��ʹ�ã�����ǰ����

        // ��������ʱ�߼�ռλ
    }

    // Update is called once per frame
    void Update()
    {
        // ��ȡ���루�� Update �
        inputX = 0f;
        if (Input.GetKey(KeyCode.A)) inputX -= 1f;
        if (Input.GetKey(KeyCode.D)) inputX += 1f;

        if (inputX > 0) facing = 1;
        else if (inputX < 0) facing = -1;

        if (Input.GetKeyDown(KeyCode.K))
        {
            wantJump = true;
        }

        // ���ܰ�����⣨ֻռλ��
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

    // ���ӻ���ⷶΧ�����ԣ�
    void OnDrawGizmosSelected()
    {
        if (bodyCollider == null) return;

        // �� CheckGround ʹ����ͬ�Ĳ����ͼ��㷽ʽ����֤���ӻ�����һ��
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
