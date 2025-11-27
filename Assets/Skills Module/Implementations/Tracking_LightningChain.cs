using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 闪电链 - 在多个敌人之间跳跃造成闪电伤害
/// </summary>
public class Tracking_LightningChain : SM_BaseSkill
{
    [Header("闪电链属性")]
    public float initialDamage = 25f;
    public float damageReduction = 0.7f;
    public int maxJumps = 4;
    public float jumpRange = 3f;
    public float seekRadius = 8f;

    [Header("目标层级")]
    public LayerMask targetLayer;

    [Header("视觉效果")]
    public GameObject chainEffect;

    private bool _isCasting = false;

    protected override bool DoCast()
    {
        if (_isCasting)
        {
            Debug.LogWarning("[闪电链] 技能正在施放中，无法重复施放");
            return false;
        }

        var target = FindClosestTarget();
        if (target == null)
        {
            Debug.LogWarning($"[闪电链] 没有找到目标，技能施放失败");
            return false;
        }

        _isCasting = true;
        StartCoroutine(ChainLightning(target));
        return true;
    }

    private Transform FindClosestTarget()
    {
        if (character == null)
        {
            Debug.LogWarning("[闪电链] 角色引用为空");
            return null;
        }

        Vector2 origin = character.AimOrigin != null ?
            (Vector2)character.AimOrigin.position : (Vector2)transform.position;

        var hits = Physics2D.OverlapCircleAll(origin, seekRadius, targetLayer);
        float closestDistance = float.MaxValue;
        Transform closestTarget = null;

        foreach (var hit in hits)
        {
            if (hit == null) continue;

            var damageable = hit.GetComponentInParent<SM_IDamageable>();
            if (damageable == null) continue;

            float distance = Vector2.Distance(hit.transform.position, origin);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = hit.transform;
            }
        }

        if (closestTarget != null)
        {
            Debug.Log($"[闪电链] 找到目标: {closestTarget.name}, 距离: {closestDistance}");
        }
        else
        {
            Debug.Log($"[闪电链] 在半径 {seekRadius} 内没有找到目标");
        }

        return closestTarget;
    }

    private IEnumerator ChainLightning(Transform firstTarget)
    {
        var hitTargets = new List<Transform>();
        Transform currentTarget = firstTarget;

        var firstDamageable = firstTarget.GetComponentInParent<SM_IDamageable>();
        if (firstDamageable == null)
        {
            _isCasting = false;
            yield break;
        }

        hitTargets.Add(firstTarget);

        float currentDamage = initialDamage;

        ApplyChainDamage(firstDamageable, currentDamage);

        if (character != null && character.AimOrigin != null)
        {
            CreateChainEffect(character.AimOrigin.position, firstTarget.position);
        }

        for (int jumpIndex = 1; jumpIndex < maxJumps; jumpIndex++)
        {
            Transform nextTarget = FindNextTarget(currentTarget.position, hitTargets);
            if (nextTarget == null)
            {
                Debug.Log($"[闪电链] 第 {jumpIndex} 跳后没有找到下一个目标");
                break;
            }

            var nextDamageable = nextTarget.GetComponentInParent<SM_IDamageable>();
            if (nextDamageable == null) break;

            currentDamage *= damageReduction;
            if (currentDamage < 1f)
            {
                Debug.Log($"[闪电链] 伤害过低，停止跳跃");
                break;
            }

            ApplyChainDamage(nextDamageable, currentDamage);
            CreateChainEffect(currentTarget.position, nextTarget.position);

            hitTargets.Add(nextTarget);
            currentTarget = nextTarget;

            Debug.Log($"[闪电链] 第 {jumpIndex} 跳完成，伤害: {currentDamage}");

            yield return new WaitForSeconds(0.2f);
        }

        Debug.Log($"[闪电链] 完成 {hitTargets.Count} 次跳跃，总共击中 {hitTargets.Count} 个目标");

        // 重要：重置施放状态，但不销毁技能对象
        _isCasting = false;

        // 确保没有销毁技能对象的代码！
        // 不要有 Destroy(gameObject); 或类似的代码
    }

    private Transform FindNextTarget(Vector3 currentPos, List<Transform> excludedTargets)
    {
        var hits = Physics2D.OverlapCircleAll(currentPos, jumpRange, targetLayer);
        float closestDistance = float.MaxValue;
        Transform closestTarget = null;

        foreach (var hit in hits)
        {
            if (hit == null) continue;

            var damageable = hit.GetComponentInParent<SM_IDamageable>();
            if (damageable == null) continue;

            Transform targetTransform = damageable.GetTransform();

            if (excludedTargets.Contains(targetTransform)) continue;

            float distance = Vector3.Distance(currentPos, targetTransform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTarget = targetTransform;
            }
        }

        return closestTarget;
    }

    private void ApplyChainDamage(SM_IDamageable target, float damage)
    {
        var info = new SM_DamageInfo
        {
            Amount = damage,
            Element = SM_Element.Lightning,
            IgnoreDefense = false,
            CritChance = 0.1f,
            CritMultiplier = 2f
        };
        target.ApplyDamage(info);

        Debug.Log($"[闪电链] 对 {target.GetTransform().name} 造成 {damage} 点闪电伤害");
    }

    private void CreateChainEffect(Vector3 from, Vector3 to)
    {
        Debug.DrawLine(from, to, Color.yellow, 1f);

        if (chainEffect != null)
        {
            var effect = Instantiate(chainEffect, (from + to) * 0.5f, Quaternion.identity);

            Vector3 direction = to - from;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            effect.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

            var renderer = effect.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.size = new Vector2(direction.magnitude, renderer.size.y);
            }

            // 只销毁视觉效果，不销毁技能对象
            Destroy(effect, 2f);
        }
    }
}