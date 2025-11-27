using UnityEngine;
using System.Collections;

/// <summary>
/// 烈焰斩 - 向前方扇形区域释放火焰斩击，造成火焰伤害并附加燃烧效果
/// </summary>
public class Melee_FireSlash : SM_MeleeSkillBase
{
    [Header("烈焰斩属性")]
    public float damage = 35f;
    public float burnDPS = 8f;
    public float burnDuration = 3f;

    [Header("攻击范围")]
    public float attackRange = 3f;
    public float attackAngle = 120f; // 扇形角度
    public LayerMask targetLayer;

    private readonly System.Collections.Generic.List<SM_IDamageable> _hitTargets =
        new System.Collections.Generic.List<SM_IDamageable>();

    protected override void OnHitboxBegin()
    {
        PerformFireSlash();
    }

    private void PerformFireSlash()
    {
        _hitTargets.Clear();

        Vector2 origin = character != null ?
            (Vector2)character.AimOrigin.position : (Vector2)transform.position;
        Vector2 direction = character != null ? character.AimDirection.normalized : Vector2.right;

        var hits = Physics2D.OverlapCircleAll(origin, attackRange, targetLayer);

        foreach (var hit in hits)
        {
            if (hit == null) continue;

            Vector2 toTarget = (Vector2)hit.transform.position - origin;
            float angle = Vector2.Angle(direction, toTarget);

            // 检查是否在扇形范围内
            if (angle <= attackAngle * 0.5f && toTarget.magnitude <= attackRange)
            {
                var damageable = hit.GetComponentInParent<SM_IDamageable>();
                if (damageable != null && !_hitTargets.Contains(damageable))
                {
                    _hitTargets.Add(damageable);
                    ApplyFireDamage(damageable);
                }
            }
        }

        Debug.Log($"[烈焰斩] 击中 {_hitTargets.Count} 个目标");

        // 触发火焰特效
        StartCoroutine(CreateFireEffect(origin, direction));
    }

    private void ApplyFireDamage(SM_IDamageable target)
    {
        var info = new SM_DamageInfo
        {
            Amount = damage,
            Element = SM_Element.Fire,
            IgnoreDefense = false,
            CritChance = 0.15f,
            CritMultiplier = 1.8f
        };
        target.ApplyDamage(info);

        // 应用燃烧效果
        var burnable = target.GetTransform().GetComponentInParent<SM_IBurnable>();
        if (burnable != null)
        {
            burnable.ApplyBurn(burnDPS, burnDuration);
        }
    }

    private IEnumerator CreateFireEffect(Vector2 origin, Vector2 direction)
    {
        // 创建扇形火焰效果
        float effectDuration = 0.5f;
        float timer = 0f;

        while (timer < effectDuration)
        {
            // 绘制扇形Gizmo（仅用于可视化）
            Debug.DrawRay(origin, Quaternion.Euler(0, 0, -attackAngle * 0.5f) * direction * attackRange, Color.red, 0.1f);
            Debug.DrawRay(origin, Quaternion.Euler(0, 0, attackAngle * 0.5f) * direction * attackRange, Color.red, 0.1f);

            timer += Time.deltaTime;
            yield return null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (character == null) return;

        Vector2 origin = character.AimOrigin != null ?
            (Vector2)character.AimOrigin.position : (Vector2)transform.position;
        Vector2 direction = character.AimDirection.normalized;
        if (direction == Vector2.zero) direction = Vector2.right;

        // 绘制扇形范围
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.3f);
        Vector2 leftBound = Quaternion.Euler(0, 0, -attackAngle * 0.5f) * direction * attackRange;
        Vector2 rightBound = Quaternion.Euler(0, 0, attackAngle * 0.5f) * direction * attackRange;

        Gizmos.DrawLine(origin, origin + leftBound);
        Gizmos.DrawLine(origin, origin + rightBound);
        Gizmos.DrawLine(origin + leftBound, origin + rightBound);
    }
}