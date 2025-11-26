using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Melee skill using an oriented ellipse in front of the character. Triggered via SM_MeleeSkillBase hit window or animation events.
/// </summary>
public class Melee_EllipseSlash : SM_MeleeSkillBase
{
    [Header("Damage")]
    public float damage = 20f;
    public SM_Element damageElement = SM_Element.Physical;
    public bool ignoreDefense = false;
    [Range(0f, 1f)] public float critChance = 0f;
    public float critMultiplier = 1.5f;

    [Header("Hit Area (Ellipse in facing direction)")]
    public float forwardRange = 2f; // semi-major axis along facing
    public float sideRange = 1f;    // semi-minor axis perpendicular to facing
    public LayerMask targetLayer;

    private readonly List<SM_IDamageable> _hitCache = new List<SM_IDamageable>(16);

    protected override void OnHitboxBegin()
    {
        PerformHitCheck();
    }

    private void PerformHitCheck()
    {
        _hitCache.Clear();

        if (character == null)
        {
            Debug.LogWarning("[Melee_EllipseSlash] No character provider; cannot resolve aim/origin.");
            return;
        }

        Vector2 origin = character.AimOrigin != null ? (Vector2)character.AimOrigin.position : (Vector2)transform.position;
        Vector2 forward = character.AimDirection.normalized;
        if (forward == Vector2.zero) forward = Vector2.right;
        Vector2 perp = new Vector2(-forward.y, forward.x);

        // Bounding radius for overlap to reduce checks
        float overlapRadius = Mathf.Max(forwardRange, sideRange);
        var hits = Physics2D.OverlapCircleAll(origin, overlapRadius, targetLayer);
        foreach (var col in hits)
        {
            if (col == null) continue;
            var damageable = col.GetComponentInParent<SM_IDamageable>();
            if (damageable == null) continue;
            if (_hitCache.Contains(damageable)) continue; // avoid double hits per check

            Transform t = damageable.GetTransform();
            Vector2 toTarget = (Vector2)t.position - origin;
            float forwardProj = Vector2.Dot(toTarget, forward);
            if (forwardProj < 0f) continue; // behind the character
            float sideProj = Vector2.Dot(toTarget, perp);

            float ellipseVal = (forwardProj * forwardProj) / (forwardRange * forwardRange) +
                               (sideProj * sideProj) / (sideRange * sideRange);
            if (ellipseVal <= 1f)
            {
                _hitCache.Add(damageable);
                ApplyDamage(damageable);
            }
        }
    }

    private void ApplyDamage(SM_IDamageable target)
    {
        var info = new SM_DamageInfo
        {
            Amount = damage,
            Element = damageElement,
            IgnoreDefense = ignoreDefense,
            CritChance = critChance,
            CritMultiplier = critMultiplier
        };
        target.ApplyDamage(info);
    }

    private void OnDrawGizmosSelected()
    {
        if (character == null) return;
        Vector2 origin = character.AimOrigin != null ? (Vector2)character.AimOrigin.position : (Vector2)transform.position;
        Vector2 forward = character.AimDirection.normalized;
        if (forward == Vector2.zero) forward = Vector2.right;
        Vector2 perp = new Vector2(-forward.y, forward.x);

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.25f);
        const int segments = 32;
        Vector3 prev = Vector3.zero;
        for (int i = 0; i <= segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            float x = Mathf.Cos(angle) * forwardRange;
            float y = Mathf.Sin(angle) * sideRange;
            Vector3 pos = origin + forward * x + perp * y;
            if (i > 0) Gizmos.DrawLine(prev, pos);
            prev = pos;
        }
    }
}
