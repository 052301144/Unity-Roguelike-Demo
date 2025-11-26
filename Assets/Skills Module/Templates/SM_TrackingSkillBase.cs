using UnityEngine;

/// <summary>
/// Template for tracking/lock-on skills. Finds a target and moves toward it before applying effect.
/// </summary>
public abstract class SM_TrackingSkillBase : SM_BaseSkill
{
    [Header("Tracking Skill")]
    public float seekRadius = 8f;
    public LayerMask targetLayer;
    public float moveSpeed = 12f;

    protected override bool DoCast()
    {
        var target = FindClosestTarget();
        if (target == null)
        {
            Debug.LogWarning($"[SM_TrackingSkillBase] {SkillName} found no target in range {seekRadius}.");
            return false;
        }

        Vector3 targetPos = target.position;
        StartCoroutine(MoveAndStrike(targetPos, target));
        return true;
    }

    private Transform FindClosestTarget()
    {
        Vector2 origin = character != null ? (Vector2)character.AimOrigin.position : (Vector2)transform.position;
        var hits = Physics2D.OverlapCircleAll(origin, seekRadius, targetLayer);
        float best = float.MaxValue;
        Transform bestTarget = null;
        foreach (var h in hits)
        {
            float d = Vector2.SqrMagnitude(h.transform.position - (Vector3)origin);
            if (d < best)
            {
                best = d;
                bestTarget = h.transform;
            }
        }
        return bestTarget;
    }

    private System.Collections.IEnumerator MoveAndStrike(Vector3 targetPos, Transform target)
    {
        while (Vector3.Distance(transform.position, targetPos) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            yield return null;
        }
        OnArrived(target);
    }

    /// <summary>
    /// Called when arriving at the target location. Apply damage/effects here.
    /// </summary>
    protected abstract void OnArrived(Transform target);
}
