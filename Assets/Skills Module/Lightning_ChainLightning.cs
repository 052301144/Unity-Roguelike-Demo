using UnityEngine;                // Unity �����ռ�
using System.Collections.Generic; // HashSet/List

/// <summary>
/// ���������ӵ�һ��ǰ��Ŀ�꿪ʼ�������Ŀ����Ծ N ��
/// </summary>
public class SM_Lightning_ChainLightning : SM_BaseSkill
{
    [Header("����������")]
    public float firstRange = 6f;     // ��һ��Ŀ��������Χ
    public float jumpRange = 5f;      // ��Ծ��Χ����һ���ڸ÷�Χ�ڣ�
    public int maxJumps = 4;          // �����Ծ����������Ŀ����=����+1��
    public float damage = 14f;        // ÿ�����е��˺�
    public LayerMask enemyMask;       // ����ͼ��

    protected override bool DoCast()
    {
        var origin = (Vector2)character.AimOrigin.position;    // �ͷ�ԭ��
        var dir = character.AimDirection.normalized;           // �泯����

        // 1) �ҵ�һ��Ŀ�꣺Ҫ���� firstRange �ڡ��Ҿ�����ǰ������н�С
        Collider2D first = null;                               // ��һ��Ŀ��
        float bestDot = 0.5f;                                  // ���ٳ������ֵ��>0.5 ���н� < ~60�㣩
        var cands = Physics2D.OverlapCircleAll(origin, firstRange, enemyMask); // ��ѡ
        foreach (var c in cands)
        {
            var v = ((Vector2)c.transform.position - origin).normalized; // ָ����
            var d = Vector2.Dot(dir, v);                                 // ��ǰ���н�����
            if (d > bestDot) { bestDot = d; first = c; }                 // ѡ����ǰ��
        }
        if (first == null) return false;                                 // �Ҳ�����ʧ�ܣ�������ȴ/���ٿ�������Ϊ�ѿ�����

        // 2) ������ÿ�ζ� current ����˺���Ȼ����丽������һ��δ����Ŀ��
        var visited = new HashSet<Collider2D>();                         // �����ظ�����
        var current = first;                                             // ��ǰĿ��
        for (int i = 0; i < maxJumps + 1 && current != null; i++)        // ���д���=����+1
        {
            if (visited.Contains(current)) break;                        // ���������˳�
            visited.Add(current);                                        // �������
            var dmg = current.GetComponent<SM_IDamageable>();            // ���˽ӿ�
            if (dmg != null)
            {
                dmg.ApplyDamage(new SM_DamageInfo
                {
                    Amount = damage,                 // ÿ���˺�
                    Element = SM_Element.Lightning,  // ��Ԫ��
                    IgnoreDefense = false,           // �����ӷ���
                    CritChance = 0f,                 // �ޱ���
                    CritMultiplier = 1f              // ����
                });
            }
            current = FindNextTarget(current.transform.position, visited); // ������һ��
        }
        return true;                                                      // �ɹ�
    }

    private Collider2D FindNextTarget(Vector2 from, HashSet<Collider2D> visited)
    {
        var targets = Physics2D.OverlapCircleAll(from, jumpRange, enemyMask); // Ѱ�� jumpRange �ڵ�Ŀ��
        float closest = float.MaxValue;                                       // �������
        Collider2D best = null;                                               // ���Ŀ��
        foreach (var t in targets)
        {
            if (visited.Contains(t)) continue;                                // ����������Ŀ��
            float dist = Vector2.Distance(from, t.transform.position);        // ����
            if (dist < closest) { closest = dist; best = t; }                 // ȡ���
        }
        return best;                                                          // �������Ŀ�꣨����Ϊ�գ�
    }
}