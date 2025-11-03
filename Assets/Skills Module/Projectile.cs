using UnityEngine; // Unity �����ռ�

/// <summary>
/// ͨ��Ͷ������ڻ���/��׶/��ѹ����һ���Ե���
/// ʹ�÷�������һ��Ԥ���壨�� SpriteRenderer + Collider2D isTrigger�����Ҵ˽ű�������ʵ��������� Launch()
/// </summary>
[RequireComponent(typeof(Collider2D))]      // ��Ҫ���������������
public class SM_Projectile : MonoBehaviour
{
    [Header("��������")]
    public float speed = 10f;               // �����ٶ�
    public float lifetime = 3f;             // ���ʱ�䣨��ʱ�Ի٣�
    public float damage = 10f;              // �����˺�
    public SM_Element element = SM_Element.Physical; // Ԫ������

    [Header("��Ԫ�أ���ѡ��")]
    public float knockbackForce = 0f;       // ��������
    public float knockbackTime = 0.1f;      // ���˳���ʱ��

    [Header("��Ԫ�أ���ѡ��")]
    public float burnDPS = 0f;              // ȼ��ÿ���˺�
    public float burnTime = 0f;             // ȼ�ճ���ʱ��

    [Header("��Ԫ�أ���ѡ��")]
    public float freezeChance = 0f;         // ������ʣ�0~1��
    public float freezeTime = 0f;           // �������ʱ��

    private Vector2 _dir;                   // ���з���
    private float _t;                       // ����ʱ

    public void Launch(Vector2 dir)         // ���䣨�ɼ��ܵ��ã�
    {
        _dir = dir.normalized;              // ��¼��һ������
    }

    private void Update()
    {
        transform.position += (Vector3)(_dir * speed * Time.deltaTime); // ÿ֡λ��
        _t += Time.deltaTime;                                            // ��ʱ
        if (_t >= lifetime) Destroy(gameObject);                         // ��ʱ����
    }

    private void OnTriggerEnter2D(Collider2D other) // ���д���
    {
        var dmg = other.GetComponent<SM_IDamageable>();  // �õ������˽ӿ�
        if (dmg != null)                                 // ���������
        {
            // �˺�������Ĭ�ϵͱ��������ӷ�����
            dmg.ApplyDamage(new SM_DamageInfo
            {
                Amount = damage,                         // �˺�
                Element = element,                       // Ԫ��
                IgnoreDefense = (element == SM_Element.Physical),     // �������ӷ���
                CritChance = (element == SM_Element.Physical ? 0.1f : 0f), // ����ͱ���
                CritMultiplier = 1.5f                    // ��������
            });

            // ���ˣ�����Ŀ����ʵ�ֻ��˽ӿڣ�
            var kb = other.GetComponent<SM_IKnockbackable>();
            if (kb != null && knockbackForce > 0f)
                kb.Knockback(_dir, knockbackForce, knockbackTime);

            // ȼ�գ���ҪĿ��� SM_BurnEffect ���ܴ��������˺���
            if (burnDPS > 0f && burnTime > 0f)
                other.GetComponent<SM_BurnEffect>()?.Apply(burnDPS, burnTime);

            // ���ᣨ��ҪĿ��� SM_FreezeEffect ������ʾ����״̬��
            if (freezeChance > 0f && Random.value < freezeChance)
                other.GetComponent<SM_FreezeEffect>()?.Freeze(freezeTime);
        }

        Destroy(gameObject); // ���к��Ի٣�һ���Ե�����
    }
}