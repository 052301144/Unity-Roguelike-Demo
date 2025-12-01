using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 浮动伤害管理器（单例）
/// </summary>
public class DamagePopupManager : MonoBehaviour
{
    public static DamagePopupManager Instance;

    [Header("预制体引用")]
    [SerializeField] private DamagePopup damagePopupPrefab; // 拖拽前面创建的预制体

    [Header("池化设置")]
    [SerializeField] private int poolSize = 10; // 初始对象池大小
    private Queue<DamagePopup> _popupPool = new Queue<DamagePopup>();

    private void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 初始化对象池
        InitializePool();
    }

    /// <summary>
    /// 初始化对象池（提前创建多个预制体实例，避免重复Instantiate）
    /// </summary>
    private void InitializePool()
    {
        if (damagePopupPrefab == null)
        {
            Debug.LogError("未赋值DamagePopup预制体！");
            return;
        }

        for (int i = 0; i < poolSize; i++)
        {
            DamagePopup popup = Instantiate(damagePopupPrefab, transform);
            popup.gameObject.SetActive(false);
            _popupPool.Enqueue(popup);
        }
    }

    /// <summary>
    /// 从对象池获取伤害数字实例
    /// </summary>
    private DamagePopup GetPopupFromPool()
    {
        // 池为空时，扩容创建新实例
        if (_popupPool.Count == 0)
        {
            DamagePopup popup = Instantiate(damagePopupPrefab, transform);
            popup.gameObject.SetActive(false);
            _popupPool.Enqueue(popup);
        }

        DamagePopup popupInstance = _popupPool.Dequeue();
        popupInstance.gameObject.SetActive(true);
        // 重置状态
        popupInstance.GetComponent<CanvasGroup>().alpha = 1f;
        popupInstance.transform.localScale = Vector3.one;
        return popupInstance;
    }

    /// <summary>
    /// 回收伤害数字到对象池
    /// </summary>
    public void ReturnPopupToPool(DamagePopup popup)
    {
        popup.gameObject.SetActive(false);
        _popupPool.Enqueue(popup);
    }

    /// <summary>
    /// 外部调用：显示伤害数字
    /// </summary>
    /// <param name="damageAmount">伤害值</param>
    /// <param name="worldPosition">弹出位置（世界坐标）</param>
    /// <param name="isCritical">是否暴击</param>
    public void ShowDamagePopup(int damageAmount, Vector3 worldPosition, bool isCritical = false)
    {
        DamagePopup popup = GetPopupFromPool();
        popup.SetDamage(damageAmount, isCritical);
        popup.SetPosition(worldPosition);

        // 生命周期结束后自动回收（修改DamagePopup脚本的销毁逻辑）
        // 注：需要在DamagePopup的Update中，销毁前调用ReturnPopupToPool
        // 因此修改DamagePopup的Update末尾：
        // if (_timer >= lifeTime || _canvasGroup.alpha <= 0)
        // {
        //     DamagePopupManager.Instance.ReturnPopupToPool(this);
        // }
    }
}