using UnityEngine;
using System.Collections;

/// <summary>
/// 雷霆一击 - 对前方矩形区域释放强力电击，造成电元素伤害
/// </summary>
public class Melee_ThunderStrike : SM_MeleeSkillBase
{
    [Header("雷霆一击属性")]
    public float damage = 40f;
    public float stunChance = 0.3f;
    public float stunDuration = 1.5f;

    [Header("攻击范围")]
    public float attackRange = 4f;
    public float attackWidth = 2f;
    public LayerMask targetLayer;

    protected override void OnHitboxBegin()
    {
        PerformThunderStrike();
    }

    private void PerformThunderStrike()
    {
        Vector2 origin = character != null ?
            (Vector2)character.AimOrigin.position : (Vector2)transform.position;
        Vector2 direction = character != null ? character.AimDirection.normalized : Vector2.right;

        // 矩形检测区域
        Vector2 detectionSize = new Vector2(attackRange, attackWidth);
        Vector2 detectionCenter = origin + direction * (attackRange * 0.5f);
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        var hits = Physics2D.OverlapBoxAll(detectionCenter, detectionSize, angle, targetLayer);

        foreach (var hit in hits)
        {
            if (hit == null) continue;

            var damageable = hit.GetComponentInParent<SM_IDamageable>();
            if (damageable != null)
            {
                ApplyThunderDamage(damageable);
            }
        }

        Debug.Log($"[雷霆一击] 击中 {hits.Length} 个目标");

        // 雷电特效
        StartCoroutine(CreateThunderEffect(origin, direction));
    }

    private void ApplyThunderDamage(SM_IDamageable target)
    {
        var info = new SM_DamageInfo
        {
            Amount = damage,
            Element = SM_Element.Lightning,
            IgnoreDefense = false,
            CritChance = 0.25f,
            CritMultiplier = 2f
        };
        target.ApplyDamage(info);

    }

    private IEnumerator CreateThunderEffect(Vector2 origin, Vector2 direction)
    {
        // 创建雷电视觉效果
        float effectDuration = 0.5f;
        float timer = 0f;

        while (timer < effectDuration)
        {
            // 绘制矩形范围
            Vector2 detectionCenter = origin + direction * (attackRange * 0.5f);
            Vector2 detectionSize = new Vector2(attackRange, attackWidth);
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // 绘制调试框
            DrawDebugBox(detectionCenter, detectionSize, angle, Color.yellow);

            timer += Time.deltaTime;
            yield return null;
        }
    }

    private void DrawDebugBox(Vector2 center, Vector2 size, float angle, Color color)
    {
        // 计算矩形的四个角
        Vector2 halfSize = size * 0.5f;
        Matrix4x4 rotationMatrix = Matrix4x4.TRS(center, Quaternion.Euler(0, 0, angle), Vector3.one);

        Vector2[] corners = new Vector2[4]
        {
            new Vector2(-halfSize.x, -halfSize.y),
            new Vector2(halfSize.x, -halfSize.y),
            new Vector2(halfSize.x, halfSize.y),
            new Vector2(-halfSize.x, halfSize.y)
        };

        for (int i = 0; i < 4; i++)
        {
            Vector2 worldCorner = rotationMatrix.MultiplyPoint(corners[i]);
            Vector2 nextWorldCorner = rotationMatrix.MultiplyPoint(corners[(i + 1) % 4]);
            Debug.DrawLine(worldCorner, nextWorldCorner, color, 0.1f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (character == null) return;

        Vector2 origin = character.AimOrigin != null ?
            (Vector2)character.AimOrigin.position : (Vector2)transform.position;
        Vector2 direction = character.AimDirection.normalized;
        if (direction == Vector2.zero) direction = Vector2.right;

        // 绘制攻击范围
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Vector2 detectionCenter = origin + direction * (attackRange * 0.5f);
        Vector2 detectionSize = new Vector2(attackRange, attackWidth);

        // 绘制矩形
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Matrix4x4 rotationMatrix = Matrix4x4.TRS(detectionCenter, Quaternion.AngleAxis(angle, Vector3.forward), Vector3.one);
        Gizmos.matrix = rotationMatrix;
        Gizmos.DrawWireCube(Vector3.zero, detectionSize);
        Gizmos.matrix = Matrix4x4.identity;
    }
}