using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugTest : MonoBehaviour
{
    [Header("测试对象")]
    public GameObject player;
    public GameObject enemy;

    [Header("测试设置")]
    public bool autoTest = true;
    public float testInterval = 2f;

    private Attribute playerAttr;
    private Attribute enemyAttr;
    private Attack playerAttack;
    private Attack enemyAttack;

    private void Start()
    {
        // 确保对象存在
        if (player == null || enemy == null)
        {
            Debug.LogError("请分配Player和Enemy对象！");
            return;
        }

        // 获取组件
        playerAttr = player.GetComponent<Attribute>();
        enemyAttr = enemy.GetComponent<Attribute>();
        playerAttack = player.GetComponent<Attack>();
        enemyAttack = enemy.GetComponent<Attack>();

        // 检查组件是否获取成功
        if (playerAttr == null) Debug.LogError("未找到玩家的Attribute组件");
        if (enemyAttr == null) Debug.LogError("未找到敌人的Attribute组件");
        if (playerAttack == null) Debug.LogError("未找到玩家的Attack组件");

        // 只有在所有组件都存在时才注册事件
        if (playerAttr != null && enemyAttr != null && playerAttack != null)
        {
            RegisterEvents();
            Debug.Log("=== 开始Debug测试 ===");

            if (autoTest)
            {
                StartCoroutine(AutoTestCoroutine());
            }
            else
            {
                StartManualTest();
            }
        }
        else
        {
            Debug.LogError("缺少必要组件，无法进行测试");
        }
    }

    private void RegisterEvents()
    {
        // 注册事件
        playerAttr.OnHealthChanged += (health) =>
            Debug.Log($"玩家生命值变化: {health}/{playerAttr.MaxHealth}");
        playerAttr.OnTakeDamage += (damage, attacker) =>
            Debug.Log($"玩家受到伤害: {damage}点 (来源: {attacker?.name})");
        playerAttr.OnDeath += () => Debug.Log("玩家死亡!");

        // 敌人事件
        enemyAttr.OnHealthChanged += (health) =>
            Debug.Log($"敌人生命值变化: {health}/{enemyAttr.MaxHealth}");
        enemyAttr.OnTakeDamage += (damage, attacker) =>
            Debug.Log($"敌人受到伤害: {damage}点 (来源: {attacker?.name})");
        enemyAttr.OnDeath += () => Debug.Log("敌人死亡!");

        // 攻击事件
        playerAttack.OnAttackPerformed += (type) =>
            Debug.Log($"玩家执行{type}攻击");
        playerAttack.OnAttackHit += (target, damage, type) =>
            Debug.Log($"玩家{type}攻击命中{target.name}, 伤害: {damage}");
        playerAttack.OnElementEffectApplied += (type, target) =>
            Debug.Log($"玩家{type}元素效果应用到{target.name}");
    }

    private IEnumerator AutoTestCoroutine()
    {
        Debug.Log("=== 开始自动测试 ===");

        // 测试1: 基础属性测试
        yield return StartCoroutine(TestBasicAttributes());

        // 测试2: 物理攻击测试
        yield return StartCoroutine(TestPhysicalAttack());

        // 测试3: 元素攻击测试
        yield return StartCoroutine(TestElementalAttacks());

        // 测试4: 治疗效果测试
        yield return StartCoroutine(TestHealing());

        Debug.Log("=== 自动测试完成 ===");
    }

    private IEnumerator TestBasicAttributes()
    {
        Debug.Log("\n--- 测试1: 基础属性 ---");

        Debug.Log($"玩家初始属性: 生命{playerAttr.CurrentHealth}/{playerAttr.MaxHealth}, " +
                 $"攻击{playerAttr.Attack}, 防御{playerAttr.Defense}");
        Debug.Log($"敌人初始属性: 生命{enemyAttr.CurrentHealth}/{enemyAttr.MaxHealth}, " +
                 $"攻击{enemyAttr.Attack}, 防御{enemyAttr.Defense}");

        // 修改玩家属性
        playerAttr.SetAttack(20);
        playerAttr.SetDefense(5);
        Debug.Log($"修改后玩家属性: 攻击{playerAttr.Attack}, 防御{playerAttr.Defense}");

        yield return new WaitForSeconds(1f);
    }

    private IEnumerator TestPhysicalAttack()
    {
        Debug.Log("\n--- 测试2: 物理攻击 ---");

        playerAttack.SetAttackType(Attack.AttackType.Physical);
        Debug.Log($"设置攻击类型: {playerAttack.Type}");

        // 执行攻击
        playerAttack.ForceAttack();

        yield return new WaitForSeconds(1f);

        // 测试真实伤害（无视防御）
        Debug.Log("测试真实伤害...");
        enemyAttr.TakeTrueDamage(15, player);

        yield return new WaitForSeconds(1f);
    }

    private IEnumerator TestElementalAttacks()
    {
        Debug.Log("\n--- 测试3: 元素攻击 ---");

        // 测试火元素
        Debug.Log("测试火元素攻击...");
        playerAttack.SetAttackType(Attack.AttackType.Fire);
        playerAttack.ForceAttack();
        yield return new WaitForSeconds(2f);

        // 测试冰元素
        Debug.Log("测试冰元素攻击...");
        playerAttack.SetAttackType(Attack.AttackType.Ice);
        playerAttack.ForceAttack();
        yield return new WaitForSeconds(2f);

        // 测试风元素
        Debug.Log("测试风元素攻击...");
        playerAttack.SetAttackType(Attack.AttackType.Wind);
        playerAttack.ForceAttack();
        yield return new WaitForSeconds(2f);

        // 测试雷元素
        Debug.Log("测试雷元素攻击...");
        playerAttack.SetAttackType(Attack.AttackType.Thunder);
        playerAttack.ForceAttack();
        yield return new WaitForSeconds(2f);
    }

    private IEnumerator TestHealing()
    {
        Debug.Log("\n--- 测试4: 治疗效果 ---");

        // 先造成一些伤害
        enemyAttr.TakeTrueDamage(30, player);
        Debug.Log($"造成伤害后敌人生命值: {enemyAttr.CurrentHealth}");

        yield return new WaitForSeconds(1f);

        // 进行治疗
        enemyAttr.Heal(20);
        Debug.Log($"治疗后敌人生命值: {enemyAttr.CurrentHealth}");

        yield return new WaitForSeconds(1f);
    }

    private void StartManualTest()
    {
        Debug.Log("=== 手动测试模式 ===");
        Debug.Log("使用以下按键进行操作:");
        Debug.Log("1 - 物理攻击");
        Debug.Log("2 - 火元素攻击");
        Debug.Log("3 - 冰元素攻击");
        Debug.Log("4 - 风元素攻击");
        Debug.Log("5 - 雷元素攻击");
        Debug.Log("H - 治疗敌人");
        Debug.Log("R - 重置敌人生命值");
        Debug.Log("D - 对敌人造成直接伤害");
    }

    private void Update()
    {
        if (!autoTest)
        {
            HandleManualInput();
        }
    }

    private void HandleManualInput()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            playerAttack.SetAttackType(Attack.AttackType.Physical);
            playerAttack.ForceAttack();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            playerAttack.SetAttackType(Attack.AttackType.Fire);
            playerAttack.ForceAttack();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            playerAttack.SetAttackType(Attack.AttackType.Ice);
            playerAttack.ForceAttack();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            playerAttack.SetAttackType(Attack.AttackType.Wind);
            playerAttack.ForceAttack();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            playerAttack.SetAttackType(Attack.AttackType.Thunder);
            playerAttack.ForceAttack();
        }
        else if (Input.GetKeyDown(KeyCode.H))
        {
            enemyAttr.Heal(25);
            Debug.Log("治疗敌人25点生命值");
        }
        else if (Input.GetKeyDown(KeyCode.R))
        {
            enemyAttr.ResetHealth();
            Debug.Log("重置敌人生命值");
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            enemyAttr.TakeDamage(15, player);
            Debug.Log("对敌人造成15点伤害");
        }
    }

    // 创建测试场景的辅助方法
    [ContextMenu("创建测试场景")]
    public void CreateTestScene()
    {
        Debug.Log("创建测试场景...");

        // 创建玩家
        GameObject playerObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        playerObj.name = "TestPlayer";
        playerObj.transform.position = Vector3.zero;

        // 添加组件
        Attribute playerAttr = playerObj.AddComponent<Attribute>();
        Attack playerAttack = playerObj.AddComponent<Attack>();
        Rigidbody playerRb = playerObj.AddComponent<Rigidbody>();
        playerRb.useGravity = false;

        // 创建敌人
        GameObject enemyObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        enemyObj.name = "TestEnemy";
        enemyObj.transform.position = new Vector3(3, 0, 0);

        // 添加组件
        Attribute enemyAttr = enemyObj.AddComponent<Attribute>();
        Rigidbody enemyRb = enemyObj.AddComponent<Rigidbody>();
        enemyRb.useGravity = false;

        // 设置测试参数
        playerAttr.SetMaxHealth(100, true);
        playerAttr.SetAttack(15);
        enemyAttr.SetMaxHealth(80, true);
        enemyAttr.SetDefense(10);

        Debug.Log("测试场景创建完成!");
    }

    private void OnDestroy()
    {
        // 清理事件注册
        if (playerAttr != null)
        {
            playerAttr.OnHealthChanged = null;
            playerAttr.OnTakeDamage = null;
            playerAttr.OnDeath = null;
        }

        if (enemyAttr != null)
        {
            enemyAttr.OnHealthChanged = null;
            enemyAttr.OnTakeDamage = null;
            enemyAttr.OnDeath = null;
        }

        if (playerAttack != null)
        {
            playerAttack.OnAttackPerformed = null;
            playerAttack.OnAttackHit = null;
            playerAttack.OnElementEffectApplied = null;
        }
    }
}