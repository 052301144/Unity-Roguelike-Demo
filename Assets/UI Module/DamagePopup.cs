using UnityEngine;
using TMPro;

/// <summary>
/// 浮动伤害数字控制脚本
/// </summary>
public class DamagePopup : MonoBehaviour
{
    [Header("显示设置")]
    [SerializeField] private float moveSpeed = 1f; // 向上移动速度
    [SerializeField] private float fadeSpeed = 2f; // 渐变消失速度
    [SerializeField] private float lifeTime = 1f; // 数字存在时间
    [SerializeField] private Color normalDamageColor = Color.white; // 普通伤害颜色
    [SerializeField] private Color criticalDamageColor = Color.red; // 暴击伤害颜色

    private TextMeshProUGUI _damageText;
    private CanvasGroup _canvasGroup;
    private float _timer = 0f;
    private Vector3 _moveDirection;

    private void Awake()
    {
        _damageText = GetComponent<TextMeshProUGUI>();
        _canvasGroup = GetComponent<CanvasGroup>();

        if (_damageText == null)
        {
            Debug.LogError("DamagePopup缺少TextMeshPro组件！");
            Destroy(gameObject);
        }

        // 随机轻微偏移移动方向，避免多个数字重叠
        _moveDirection = new Vector3(Random.Range(-0.2f, 0.2f), 1f, 0f).normalized;
    }

    private void Update()
    {
        // 向上移动
        transform.Translate(_moveDirection * moveSpeed * Time.deltaTime);

        // 渐变消失
        _canvasGroup.alpha -= fadeSpeed * Time.deltaTime;

        // 生命周期结束后销毁
        _timer += Time.deltaTime;
        if (_timer >= lifeTime || _canvasGroup.alpha <= 0)
        {
            DamagePopupManager.Instance.ReturnPopupToPool(this); // 回收至对象池
        }
    }

    /// <summary>
    /// 设置伤害数值和类型（普通/暴击）
    /// </summary>
    /// <param name="damageAmount">伤害值</param>
    /// <param name="isCritical">是否暴击</param>
    public void SetDamage(int damageAmount, bool isCritical = false)
    {
        _damageText.text = damageAmount.ToString();
        // 暴击时添加额外样式（如颜色变红+字体变大）
        if (isCritical)
        {
            _damageText.color = criticalDamageColor;
            _damageText.fontSize *= 1.3f; // 暴击数字放大
        }
        else
        {
            _damageText.color = normalDamageColor;
        }
    }

    /// <summary>
    /// 设置弹出位置（世界坐标转UI坐标）
    /// </summary>
    /// <param name="worldPosition">伤害发生的世界坐标（如敌人受击点）</param>
    public void SetPosition(Vector3 worldPosition)
    {
        // 将世界坐标转换为UI画布坐标（适配不同Canvas渲染模式）
        Vector2 uiPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            transform.parent.GetComponent<RectTransform>(),
            Camera.main.WorldToScreenPoint(worldPosition),
            Camera.main,
            out uiPosition
        );
        transform.localPosition = uiPosition;
    }
}