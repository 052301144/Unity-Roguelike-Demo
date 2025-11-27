using UnityEngine;
using System.Collections;

/// <summary>
/// 狂风突进 - 快速冲刺并对路径上敌人造成击退
/// </summary>
public class Movement_WindDash : SM_MovementSkillBase
{
    [Header("狂风突进属性")]
    public float pushForce = 8f;
    public float pushDuration = 0.5f;
    public float pathDamage = 15f;

    [Header("范围")]
    public float pushRadius = 1.5f;

    [Header("目标层级")]
    public LayerMask targetLayer;

    protected override bool DoCast()
    {
        // 确保 rbCache 引用的是玩家的 Rigidbody2D
        if (rbCache == null)
        {
            // 通过 character 的 AimOrigin 获取玩家的 Rigidbody2D
            if (character != null && character.AimOrigin != null)
            {
                rbCache = character.AimOrigin.GetComponent<Rigidbody2D>();
                // 如果 AimOrigin 上没有 Rigidbody2D，尝试从根对象获取
                if (rbCache == null)
                {
                    rbCache = character.AimOrigin.root.GetComponent<Rigidbody2D>();
                }
            }

            if (rbCache == null)
            {
                Debug.LogWarning($"[狂风突进] 无法找到玩家的 Rigidbody2D 组件");
                return false;
            }
        }

        // 冲刺前先检查路径上的敌人
        StartCoroutine(CheckPathDuringDash());
        return base.DoCast();
    }

    private IEnumerator CheckPathDuringDash()
    {
        Vector2 startPos = rbCache.position;
        Vector2 endPos = startPos + (character != null ? character.AimDirection.normalized : Vector2.right) * dashDistance;

        float elapsed = 0f;
        Vector2 lastPos = startPos;

        while (elapsed < dashDuration)
        {
            Vector2 currentPos = rbCache.position;

            // 检查两点之间的敌人
            CheckSegmentForEnemies(lastPos, currentPos);

            lastPos = currentPos;
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void CheckSegmentForEnemies(Vector2 from, Vector2 to)
    {
        Vector2 direction = (to - from).normalized;
        float distance = Vector2.Distance(from, to);

        // 使用胶囊体检测路径上的敌人
        var hits = Physics2D.CapsuleCastAll(from, new Vector2(pushRadius, pushRadius),
                                          CapsuleDirection2D.Horizontal, 0f, direction, distance, targetLayer);

        foreach (var hit in hits)
        {
            var damageable = hit.collider.GetComponentInParent<SM_IDamageable>();
            if (damageable != null)
            {
                // 造成伤害
                var info = new SM_DamageInfo
                {
                    Amount = pathDamage,
                    Element = SM_Element.Wind,
                    IgnoreDefense = false,
                    CritChance = 0.1f,
                    CritMultiplier = 1.5f
                };
                damageable.ApplyDamage(info);

                // 击退
                var knockbackable = hit.collider.GetComponentInParent<SM_IKnockbackable>();
                if (knockbackable != null)
                {
                    Vector2 pushDir = ((Vector2)hit.transform.position - from).normalized;
                    knockbackable.Knockback(pushDir, pushForce, pushDuration);
                }
            }
        }
    }

    protected override void OnDashApplied(Vector2 start, Vector2 end)
    {
        // 冲刺结束时的爆发效果
        CreateWindBurst(end);

        Debug.Log($"[狂风突进] 从 {start} 冲刺到 {end}");
    }

    private void CreateWindBurst(Vector2 position)
    {
        // 冲刺结束时的范围击退
        var hits = Physics2D.OverlapCircleAll(position, pushRadius * 1.5f, targetLayer);

        foreach (var hit in hits)
        {
            var knockbackable = hit.GetComponentInParent<SM_IKnockbackable>();
            if (knockbackable != null)
            {
                Vector2 pushDir = ((Vector2)hit.transform.position - position).normalized;
                knockbackable.Knockback(pushDir, pushForce * 0.5f, pushDuration * 0.7f);
            }
        }

        Debug.Log($"[狂风突进] 冲刺结束，击退 {hits.Length} 个敌人");
    }
}