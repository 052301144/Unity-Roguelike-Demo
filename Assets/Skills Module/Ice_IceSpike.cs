using UnityEngine; // Unity 引擎命名空间

/// <summary>
/// 冰锥术：直接投射冰锥，造成伤害并在概率冻结目标
/// 需要在 Inspector 设置 spikePrefab（含 SM_Projectile 的预制体）
/// </summary>
public class SM_Ice_IceSpike : SM_BaseSkill
{
    [Header("冰锥术")]
    public SM_Projectile spikePrefab;    // 预制体
    public float damage = 18f;           // 伤害
    public float speed = 14f;            // 速度
    public float lifetime = 3f;          // 存活
    public float freezeChance = 0.4f;    // 冰冻概率
    public float freezeTime = 1.5f;      // 冰冻时长

    protected override bool DoCast()
    {
        if (spikePrefab == null) return false;                               // 未设置
        var go = Instantiate(spikePrefab, character.AimOrigin.position, Quaternion.identity);
        go.damage = damage;                                                  // 伤害
        go.element = SM_Element.Ice;
        go.speed = speed;
        go.lifetime = lifetime;
        go.freezeChance = freezeChance;
        go.freezeTime = freezeTime;
        go.Launch(character.AimDirection);                                    // 发射
        return true;
    }
}
