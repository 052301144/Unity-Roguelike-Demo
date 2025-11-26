using UnityEngine;

/// <summary>
/// 火球预制体脚本 - 用于创建火球技能预制体
/// 这个脚本应该附加到火球预制体上
/// </summary>
public class FireballPrefab : MonoBehaviour
{
    [Header("火球设置")]
    public float speed = 12f;
    public float damage = 25f;
    public float burnDPS = 5f;
    public float burnTime = 4f;
    public float lifetime = 3f;
    
    private SM_Projectile projectile;
    
    void Awake()
    {
        // 获取或添加SM_Projectile组件
        projectile = GetComponent<SM_Projectile>();
        if (projectile == null)
        {
            projectile = gameObject.AddComponent<SM_Projectile>();
        }
        
        // 设置火球属性
        projectile.speed = speed;
        projectile.damage = damage;
        projectile.element = SM_Element.Fire;
        projectile.burnDPS = burnDPS;
        projectile.burnTime = burnTime;
        projectile.lifetime = lifetime;
    }
    
    public void Launch(Vector2 direction)
    {
        if (projectile != null)
        {
            projectile.Launch(direction);
        }
    }
}
