using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugTest : MonoBehaviour
{
    [Header("���Զ���")]
    public GameObject player;
    public GameObject enemy;

    [Header("��������")]
    public bool autoTest = true;
    public float testInterval = 2f;

    private Attribute playerAttr;
    private Attribute enemyAttr;
    private Attack playerAttack;
    private Attack enemyAttack;

    private void Start()
    {
        // ��ȷ���������
        if (player == null || enemy == null)
        {
            Debug.LogError("��һ���˶���δ���䣡");
            return;
        }

        // ��ȡ���
        playerAttr = player.GetComponent<Attribute>();
        enemyAttr = enemy.GetComponent<Attribute>();
        playerAttack = player.GetComponent<Attack>();
        enemyAttack = enemy.GetComponent<Attack>();

        // �������Ƿ��ȡ�ɹ�
        if (playerAttr == null) Debug.LogError("δ�ҵ���ҵ�Attribute���");
        if (enemyAttr == null) Debug.LogError("δ�ҵ����˵�Attribute���");
        if (playerAttack == null) Debug.LogError("δ�ҵ���ҵ�Attack���");

        // ֻ�����������ʱ��ע���¼�
        if (playerAttr != null && enemyAttr != null && playerAttack != null)
        {
            RegisterEvents();
            Debug.Log("=== ��ʼDebug���� ===");

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
            Debug.LogError("ȱ�ٱ�Ҫ��������޷���������");
        }
    }

    private void RegisterEvents()
    {
        // ��������¼�
        playerAttr.OnHealthChanged += (health) =>
            Debug.Log($"�������ֵ�仯: {health}/{playerAttr.MaxHealth}");
        playerAttr.OnTakeDamage += (damage, attacker) =>
            Debug.Log($"����ܵ��˺�: {damage}�� (����: {attacker?.name})");
        playerAttr.OnDeath += () => Debug.Log("�������!");

        // ���������¼�
        enemyAttr.OnHealthChanged += (health) =>
            Debug.Log($"��������ֵ�仯: {health}/{enemyAttr.MaxHealth}");
        enemyAttr.OnTakeDamage += (damage, attacker) =>
            Debug.Log($"�����ܵ��˺�: {damage}�� (����: {attacker?.name})");
        enemyAttr.OnDeath += () => Debug.Log("��������!");

        // �����¼�
        playerAttack.OnAttackPerformed += (type) =>
            Debug.Log($"���ִ��{type}����");
        playerAttack.OnAttackHit += (target, damage, type) =>
            Debug.Log($"���{type}��������{target.name}, �˺�: {damage}");
        playerAttack.OnElementEffectApplied += (type, target) =>
            Debug.Log($"���{type}Ԫ��Ч��Ӧ�õ�{target.name}");
    }

    private IEnumerator AutoTestCoroutine()
    {
        Debug.Log("=== ��ʼ�Զ����� ===");

        // ����1: �������Բ���
        yield return StartCoroutine(TestBasicAttributes());

        // ����2: ����������
        yield return StartCoroutine(TestPhysicalAttack());

        // ����3: Ԫ�ع�������
        yield return StartCoroutine(TestElementalAttacks());

        // ����4: ����Ч������
        yield return StartCoroutine(TestHealing());

        Debug.Log("=== �Զ�������� ===");
    }

    private IEnumerator TestBasicAttributes()
    {
        Debug.Log("\n--- ����1: �������� ---");

        Debug.Log($"��ҳ�ʼ����: ����{playerAttr.CurrentHealth}/{playerAttr.MaxHealth}, " +
                 $"����{playerAttr.Attack}, ����{playerAttr.Defense}");
        Debug.Log($"���˳�ʼ����: ����{enemyAttr.CurrentHealth}/{enemyAttr.MaxHealth}, " +
                 $"����{enemyAttr.Attack}, ����{enemyAttr.Defense}");

        // ���������޸�
        playerAttr.SetAttack(20);
        playerAttr.SetDefense(5);
        Debug.Log($"�޸ĺ��������: ����{playerAttr.Attack}, ����{playerAttr.Defense}");

        yield return new WaitForSeconds(1f);
    }

    private IEnumerator TestPhysicalAttack()
    {
        Debug.Log("\n--- ����2: ������ ---");

        playerAttack.SetAttackType(Attack.AttackType.Physical);
        Debug.Log($"���ù�������: {playerAttack.Type}");

        // ִ�й���
        playerAttack.ForceAttack();

        yield return new WaitForSeconds(1f);

        // �������ӷ�������ʵ�˺�
        Debug.Log("������ʵ�˺�...");
        enemyAttr.TakeTrueDamage(15, player);

        yield return new WaitForSeconds(1f);
    }

    private IEnumerator TestElementalAttacks()
    {
        Debug.Log("\n--- ����3: Ԫ�ع��� ---");

        // ���Ի�Ԫ��
        Debug.Log("���Ի�Ԫ�ع���...");
        playerAttack.SetAttackType(Attack.AttackType.Fire);
        playerAttack.ForceAttack();
        yield return new WaitForSeconds(2f);

        // ���Ա�Ԫ��
        Debug.Log("���Ա�Ԫ�ع���...");
        playerAttack.SetAttackType(Attack.AttackType.Ice);
        playerAttack.ForceAttack();
        yield return new WaitForSeconds(2f);

        // ���Է�Ԫ��
        Debug.Log("���Է�Ԫ�ع���...");
        playerAttack.SetAttackType(Attack.AttackType.Wind);
        playerAttack.ForceAttack();
        yield return new WaitForSeconds(2f);

        // ������Ԫ��
        Debug.Log("������Ԫ�ع���...");
        playerAttack.SetAttackType(Attack.AttackType.Thunder);
        playerAttack.ForceAttack();
        yield return new WaitForSeconds(2f);
    }

    private IEnumerator TestHealing()
    {
        Debug.Log("\n--- ����4: ����Ч�� ---");

        // �����һЩ�˺�
        enemyAttr.TakeTrueDamage(30, player);
        Debug.Log($"�������˺�����ֵ: {enemyAttr.CurrentHealth}");

        yield return new WaitForSeconds(1f);

        // ��������
        enemyAttr.Heal(20);
        Debug.Log($"���ƺ��������ֵ: {enemyAttr.CurrentHealth}");

        yield return new WaitForSeconds(1f);
    }

    private void StartManualTest()
    {
        Debug.Log("=== �ֶ�����ģʽ ===");
        Debug.Log("ʹ�����°������в���:");
        Debug.Log("1 - ������");
        Debug.Log("2 - ��Ԫ�ع���");
        Debug.Log("3 - ��Ԫ�ع���");
        Debug.Log("4 - ��Ԫ�ع���");
        Debug.Log("5 - ��Ԫ�ع���");
        Debug.Log("H - ���Ƶ���");
        Debug.Log("R - ���õ�������ֵ");
        Debug.Log("D - �Ե������ֱ���˺�");
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
            Debug.Log("���Ƶ���25������ֵ");
        }
        else if (Input.GetKeyDown(KeyCode.R))
        {
            enemyAttr.ResetHealth();
            Debug.Log("���õ�������ֵ");
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            enemyAttr.TakeDamage(15, player);
            Debug.Log("�Ե������15���˺�");
        }
    }

    // �����������������Գ���
    [ContextMenu("�������Գ���")]
    public void CreateTestScene()
    {
        Debug.Log("�������Գ���...");

        // �������
        GameObject playerObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        playerObj.name = "TestPlayer";
        playerObj.transform.position = Vector3.zero;

        // ������
        Attribute playerAttr = playerObj.AddComponent<Attribute>();
        Attack playerAttack = playerObj.AddComponent<Attack>();
        Rigidbody playerRb = playerObj.AddComponent<Rigidbody>();
        playerRb.useGravity = false;

        // ��������
        GameObject enemyObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        enemyObj.name = "TestEnemy";
        enemyObj.transform.position = new Vector3(3, 0, 0);

        // ������
        Attribute enemyAttr = enemyObj.AddComponent<Attribute>();
        Rigidbody enemyRb = enemyObj.AddComponent<Rigidbody>();
        enemyRb.useGravity = false;

        // ���ò��Բ���
        playerAttr.SetMaxHealth(100, true);
        playerAttr.SetAttack(15);
        enemyAttr.SetMaxHealth(80, true);
        enemyAttr.SetDefense(10);

        Debug.Log("���Գ����������!");
    }

    private void OnDestroy()
    {
        // �����¼�ע��
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