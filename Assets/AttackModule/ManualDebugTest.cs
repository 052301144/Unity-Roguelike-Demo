using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManualDebugTest : MonoBehaviour
{
    [Header("测试对象")]
    public GameObject player;
    public GameObject enemy;

    private Attribute playerAttr;
    private Attribute enemyAttr;
    private Attack playerAttack;

    private void Start()
    {
        if (player == null || enemy == null)
        {
            Debug.LogError("请分配Player和Enemy对象！");
            return;
        }

        // 获取组件
        playerAttr = player.GetComponent<Attribute>();
        enemyAttr = enemy.GetComponent<Attribute>();
        playerAttack = player.GetComponent<Attack>();

        // 检查组件
        if (playerAttr == null) Debug.LogError("Player缺少Attribute组件");
        if (enemyAttr == null) Debug.LogError("Enemy缺少Attribute组件");
        if (playerAttack == null) Debug.LogError("Player缺少Attack组件");

        if (playerAttr != null && enemyAttr != null && playerAttack != null)
        {
            Debug.Log("=== 手动测试模式已启动 ===");
            PrintInstructions();
        }
    }

    private void PrintInstructions()
    {
        Debug.Log("🎮 手动测试控制说明：");
        Debug.Log("鼠标左键 - 普通攻击");
        Debug.Log("1 - 物理攻击");
        Debug.Log("2 - 火元素攻击");
        Debug.Log("3 - 冰元素攻击");
        Debug.Log("4 - 风元素攻击");
        Debug.Log("5 - 雷元素攻击");
        Debug.Log("H - 治疗敌人");
        Debug.Log("R - 重置敌人生命");
        Debug.Log("D - 直接伤害敌人");
        Debug.Log("T - 切换攻击类型");
        Debug.Log("C - 显示当前状态");
        Debug.Log("F - 强制攻击（无视冷却）");
        Debug.Log("=== 按相应按键开始测试 ===");
    }

    private void Update()
    {
        // 检测鼠标左键点击
        if (Input.GetMouseButtonDown(0))
        {
            TestMouseClickAttack();
        }

        if (Input.GetKeyDown(KeyCode.Alpha1)) TestPhysicalAttack();
        if (Input.GetKeyDown(KeyCode.Alpha2)) TestFireAttack();
        if (Input.GetKeyDown(KeyCode.Alpha3)) TestIceAttack();
        if (Input.GetKeyDown(KeyCode.Alpha4)) TestWindAttack();
        if (Input.GetKeyDown(KeyCode.Alpha5)) TestThunderAttack();
        if (Input.GetKeyDown(KeyCode.H)) TestHealing();
        if (Input.GetKeyDown(KeyCode.R)) ResetEnemyHealth();
        if (Input.GetKeyDown(KeyCode.D)) TestDirectDamage();
        if (Input.GetKeyDown(KeyCode.T)) SwitchAttackType();
        if (Input.GetKeyDown(KeyCode.C)) ShowCurrentStatus();
        if (Input.GetKeyDown(KeyCode.F)) ForceAttack();
    }

    private void TestMouseClickAttack()
    {
        Debug.Log("🖱️ 鼠标左键点击检测");

        // 检查攻击冷却
        if (playerAttack.CanAttack)
        {
            Debug.Log("✅ 可以攻击，执行攻击...");
            playerAttack.PerformAttack();
        }
        else
        {
            // 简化版本：不显示具体冷却时间
            Debug.Log($"❌ 攻击冷却中");
        }
    }

    private void ForceAttack()
    {
        Debug.Log("⚡ 强制攻击（无视冷却）");
        playerAttack.ForceAttack();
    }

    private void TestPhysicalAttack()
    {
        Debug.Log("🔥 测试物理攻击");
        playerAttack.SetAttackType(Attack.AttackType.Physical);
        playerAttack.ForceAttack();
        ShowDamageResult();
    }

    private void TestFireAttack()
    {
        Debug.Log("🔥 测试火元素攻击");
        playerAttack.SetAttackType(Attack.AttackType.Fire);
        playerAttack.ForceAttack();
        ShowDamageResult();
    }

    private void TestIceAttack()
    {
        Debug.Log("❄️ 测试冰元素攻击");
        playerAttack.SetAttackType(Attack.AttackType.Ice);
        playerAttack.ForceAttack();
        ShowDamageResult();
    }

    private void TestWindAttack()
    {
        Debug.Log("💨 测试风元素攻击");
        playerAttack.SetAttackType(Attack.AttackType.Wind);
        playerAttack.ForceAttack();
        ShowDamageResult();
    }

    private void TestThunderAttack()
    {
        Debug.Log("⚡ 测试雷元素攻击");
        playerAttack.SetAttackType(Attack.AttackType.Thunder);
        playerAttack.ForceAttack();
        ShowDamageResult();
    }

    private void TestHealing()
    {
        Debug.Log("💚 测试治疗效果");
        int healAmount = 20;
        enemyAttr.Heal(healAmount);
        Debug.Log($"敌人恢复 {healAmount} 点生命值");
        ShowEnemyStatus();
    }

    private void ResetEnemyHealth()
    {
        Debug.Log("🔄 重置敌人生命值");
        enemyAttr.ResetHealth();
        ShowEnemyStatus();
    }

    private void TestDirectDamage()
    {
        Debug.Log("💥 测试直接伤害");
        int damage = 15;
        enemyAttr.TakeDamage(damage, player);
        Debug.Log($"对敌人造成 {damage} 点直接伤害");
        ShowEnemyStatus();
    }

    private void SwitchAttackType()
    {
        var currentType = playerAttack.Type;
        Attack.AttackType newType;

        switch (currentType)
        {
            case Attack.AttackType.Physical: newType = Attack.AttackType.Fire; break;
            case Attack.AttackType.Fire: newType = Attack.AttackType.Ice; break;
            case Attack.AttackType.Ice: newType = Attack.AttackType.Wind; break;
            case Attack.AttackType.Wind: newType = Attack.AttackType.Thunder; break;
            default: newType = Attack.AttackType.Physical; break;
        }

        playerAttack.SetAttackType(newType);
        Debug.Log($"🔄 切换攻击类型: {currentType} -> {newType}");
    }

    private void ShowCurrentStatus()
    {
        Debug.Log("=== 当前状态 ===");
        Debug.Log($"玩家: 生命{playerAttr.CurrentHealth}/{playerAttr.MaxHealth}, " +
                 $"攻击{playerAttr.Attack}, 防御{playerAttr.Defense}");
        Debug.Log($"敌人: 生命{enemyAttr.CurrentHealth}/{enemyAttr.MaxHealth}, " +
                 $"防御{enemyAttr.Defense}");
        Debug.Log($"当前攻击类型: {playerAttack.Type}");
        Debug.Log($"攻击冷却状态: {(playerAttack.CanAttack ? "就绪" : "冷却中")}");
        Debug.Log($"攻击范围: {playerAttack.AttackRange}");
        Debug.Log("=================");
    }

    private void ShowDamageResult()
    {
        Invoke("DisplayResult", 0.1f);
    }

    private void DisplayResult()
    {
        ShowEnemyStatus();
    }

    private void ShowEnemyStatus()
    {
        Debug.Log($"敌人状态: {enemyAttr.CurrentHealth}/{enemyAttr.MaxHealth} " +
                 $"({(enemyAttr.IsAlive ? "存活" : "死亡")})");
    }

    [ContextMenu("创建测试对象")]
    private void CreateTestObjects()
    {
        // 创建玩家
        if (player == null)
        {
            player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.transform.position = Vector3.zero;

            // 添加Attribute组件并设置属性
            var playerAttrComp = player.AddComponent<Attribute>();
            playerAttrComp.SetMaxHealth(100, true);
            playerAttrComp.SetAttack(20);

            // 添加Attack组件
            var playerAttackComp = player.AddComponent<Attack>();

            // 添加Rigidbody
            var playerRb = player.AddComponent<Rigidbody>();
            playerRb.useGravity = false;

            Debug.Log("玩家创建完成");
        }

        // 创建敌人
        if (enemy == null)
        {
            enemy = GameObject.CreatePrimitive(PrimitiveType.Cube);
            enemy.name = "Enemy";
            enemy.transform.position = new Vector3(1.5f, 0, 0); // 确保在攻击范围内

            // 添加Attribute组件并设置属性
            var enemyAttrComp = enemy.AddComponent<Attribute>();
            enemyAttrComp.SetMaxHealth(80, true);
            enemyAttrComp.SetDefense(10);

            // 添加Rigidbody
            var enemyRb = enemy.AddComponent<Rigidbody>();
            enemyRb.useGravity = false;

            Debug.Log("敌人创建完成");
        }

        Debug.Log("测试对象创建完成！");
    }
}