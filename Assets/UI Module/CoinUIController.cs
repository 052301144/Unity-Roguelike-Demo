using UnityEngine;
using TMPro; // 如果使用TextMeshPro
// using UnityEngine.UI; // 如果使用传统Text

/// <summary>
/// 金币UI显示控制器
/// 负责监听金币变化事件并更新UI文本显示
/// </summary>
public class CoinUIController : MonoBehaviour
{
    [Header("UI引用")]
    [SerializeField] private TMP_Text coinValueText; // 显示金币数值的文本组件
    // [SerializeField] private Text coinValueText; // 传统Text组件

    [Header("格式设置")]
    [SerializeField] private string prefix = ""; // 前缀文本（如"金币: "）
    [SerializeField] private string suffix = ""; // 后缀文本（如"枚"）

    private CurrencyManager currencyManager;

    private void Awake()
    {
        // 获取CurrencyManager实例
        currencyManager = FindObjectOfType<CurrencyManager>();

        if (currencyManager == null)
        {
            Debug.LogError("[CoinUIController] 场景中未找到CurrencyManager组件！");
            enabled = false;
            return;
        }

        // 检查UI组件引用
        if (coinValueText == null)
        {
            Debug.LogError("[CoinUIController] 未设置金币文本组件引用！");
            enabled = false;
            return;
        }
    }

    private void OnEnable()
    {
        // 订阅金币更新事件
        if (currencyManager != null)
        {
            currencyManager.OnCoinsUpdated += UpdateCoinDisplay;
            // 初始化显示
            UpdateCoinDisplay(currencyManager.CurrentCoins);
        }
    }

    private void OnDisable()
    {
        // 取消订阅事件，防止内存泄漏
        if (currencyManager != null)
        {
            currencyManager.OnCoinsUpdated -= UpdateCoinDisplay;
        }
    }

    /// <summary>
    /// 更新金币显示文本
    /// </summary>
    /// <param name="coinAmount">当前金币数量</param>
    private void UpdateCoinDisplay(int coinAmount)
    {
        // 格式化显示文本
        coinValueText.text = $"{prefix}{coinAmount}{suffix}";

        // 可选：添加数字变化动画效果
        // StartCoroutine(AnimateNumberChange(coinAmount));
    }

    // 可选：数字变化动画效果
    /*
    private IEnumerator AnimateNumberChange(int targetAmount)
    {
        int currentDisplay = int.Parse(coinValueText.text.Replace(prefix, "").Replace(suffix, ""));
        float duration = 0.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            int displayAmount = Mathf.RoundToInt(Mathf.Lerp(currentDisplay, targetAmount, t));
            coinValueText.text = $"{prefix}{displayAmount}{suffix}";
            yield return null;
        }

        // 确保最终显示正确数值
        coinValueText.text = $"{prefix}{targetAmount}{suffix}";
    }
    */
}