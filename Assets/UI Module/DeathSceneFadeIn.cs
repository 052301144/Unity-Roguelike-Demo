using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class DeathSceneFadeIn : MonoBehaviour
{
    [Header("淡入设置")]
    [SerializeField] private float fadeInDuration = 2f; // 淡入持续时间
    [SerializeField] private CanvasGroup fadeCanvasGroup; // 用于淡入的CanvasGroup

    [Header("UI元素")]
    [SerializeField] private GameObject deathText; // 死亡文字
    [SerializeField] private GameObject restartButton; // 重新开始按钮

    private void Start()
    {
        // 初始化
        InitializeFade();

        // 开始淡入效果
        StartCoroutine(FadeInCoroutine());
    }

    /// <summary>
    /// 初始化淡入设置
    /// </summary>
    private void InitializeFade()
    {
        // 获取或创建CanvasGroup
        if (fadeCanvasGroup == null)
            fadeCanvasGroup = GetComponent<CanvasGroup>();

        if (fadeCanvasGroup == null)
        {
            fadeCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // 设置初始状态：完全透明
        fadeCanvasGroup.alpha = 0f;

        // 隐藏UI元素，等待淡入完成
        if (deathText != null) deathText.SetActive(false);
        if (restartButton != null) restartButton.SetActive(false);
    }

    /// <summary>
    /// 淡入协程
    /// </summary>
    private IEnumerator FadeInCoroutine()
    {
        float elapsedTime = 0f;

        // 淡入效果
        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsedTime / fadeInDuration);
            fadeCanvasGroup.alpha = alpha;
            yield return null;
        }

        // 确保完全不透明
        fadeCanvasGroup.alpha = 1f;

        // 显示UI元素
        ShowUIElements();

        Debug.Log("死亡场景淡入完成");
    }

    /// <summary>
    /// 显示UI元素
    /// </summary>
    private void ShowUIElements()
    {
        if (deathText != null)
        {
            deathText.SetActive(true);
            // 可以在这里添加文字动画效果
        }

        if (restartButton != null)
        {
            restartButton.SetActive(true);
            // 可以在这里添加按钮动画效果
        }
    }

    /// <summary>
    /// 重新开始游戏
    /// </summary>
    public void RestartGame()
    {
        SceneManager.LoadScene("Main Scenes"); // 修改为你的主场景名称
    }

    /// <summary>
    /// 返回主菜单
    /// </summary>
    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene("MainMenu"); // 修改为你的主菜单场景名称
    }
}