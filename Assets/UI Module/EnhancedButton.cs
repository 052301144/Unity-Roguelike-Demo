using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;

public class EnhancedButtonCoroutine : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("组件引用")]
    public Image buttonImage;
    public TextMeshProUGUI buttonText;
    public AudioSource audioSource;

    [Header("颜色设置")]
    public Color normalColor = new Color(0.2f, 0.2f, 0.4f, 1f);
    public Color hoverColor = new Color(0.3f, 0.3f, 0.6f, 1f);
    public Color pressedColor = new Color(0.4f, 0.4f, 0.8f, 1f);
    public Color disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

    [Header("缩放设置")]
    public float hoverScale = 1.1f;
    public float clickScale = 0.95f;
    public float animationDuration = 0.2f;
    public float clickAnimationDuration = 0.15f;

    [Header("音效设置")]
    public AudioClip hoverSound;
    public AudioClip clickSound;

    [Header("粒子效果")]
    public ParticleSystem hoverParticles;
    public GameObject selectionIndicator;
    public bool enableGlowEffect = true;
    public float glowIntensity = 1.5f;

    private Vector3 originalScale;
    private Color originalTextColor;
    private Color originalImageColor;
    private bool isInteractable = true;
    private Coroutine currentAnimation;
    private Material buttonMaterial;
    private bool isHovering = false;
    private bool isInitialized = false;

    void Start()
    {
        InitializeComponents();

        // 保存原始状态 - 确保在修改前保存
        originalScale = transform.localScale;

        if (buttonText != null)
        {
            originalTextColor = buttonText.color;
            // 确保文本启用
            buttonText.enabled = true;
        }

        if (buttonImage != null)
        {
            originalImageColor = buttonImage.color;
            // 确保图片启用
            buttonImage.enabled = true;
        }

        // 创建按钮材质的实例（避免共享材质导致的问题）
        if (buttonImage != null && enableGlowEffect)
        {
            buttonMaterial = new Material(buttonImage.material);
            buttonImage.material = buttonMaterial;
        }

        // 重置到正常状态，防止闪烁
        ResetToNormalStateImmediately();

        isInitialized = true;

        Debug.Log($"按钮 {name} 初始化完成 - 位置: {transform.position}, 缩放: {transform.localScale}, 激活: {gameObject.activeInHierarchy}");
    }

    void InitializeComponents()
    {
        // 自动获取组件引用
        if (buttonImage == null)
            buttonImage = GetComponent<Image>();
        if (buttonText == null)
            buttonText = GetComponentInChildren<TextMeshProUGUI>();
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // 确保组件不为空
        if (buttonImage == null)
            Debug.LogWarning($"按钮 {name} 没有找到 Image 组件");
        if (buttonText == null)
            Debug.LogWarning($"按钮 {name} 没有找到 TextMeshProUGUI 组件");
    }

    // 重置到正常状态（不通过协程，立即执行）
    private void ResetToNormalStateImmediately()
    {
        transform.localScale = originalScale;

        if (buttonImage != null)
        {
            buttonImage.color = normalColor;
            buttonImage.enabled = true;
        }

        if (buttonText != null)
        {
            buttonText.color = originalTextColor;
            buttonText.enabled = true;
        }

        if (selectionIndicator != null)
            selectionIndicator.SetActive(false);

        if (hoverParticles != null && hoverParticles.isPlaying)
            hoverParticles.Stop();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isInteractable || !isInitialized) return;

        isHovering = true;
        StopCurrentAnimation();
        currentAnimation = StartCoroutine(HoverEnterAnimation());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isInteractable || !isInitialized) return;

        isHovering = false;
        StopCurrentAnimation();
        currentAnimation = StartCoroutine(HoverExitAnimation());
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (gameObject.CompareTag("PauseMenuButton"))
        {
            // 如果是暂停菜单按钮，不执行动画逻辑
            return;
        }
        if (!isInteractable) return;

        StopCurrentAnimation();
        currentAnimation = StartCoroutine(ClickAnimation());
    }

    IEnumerator HoverEnterAnimation()
    {
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;
        Vector3 targetScale = originalScale * hoverScale;
        Color startColor = buttonImage != null ? buttonImage.color : normalColor;
        Color startTextColor = buttonText != null ? buttonText.color : originalTextColor;

        // 播放悬停音效
        PlaySound(hoverSound);

        // 启动粒子效果
        if (hoverParticles != null && !hoverParticles.isPlaying)
            hoverParticles.Play();

        // 显示选择指示器
        if (selectionIndicator != null)
            selectionIndicator.SetActive(true);

        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / animationDuration;
            float easedT = EaseOutBack(t);

            // 缩放动画
            transform.localScale = Vector3.Lerp(startScale, targetScale, easedT);

            // 按钮颜色过渡
            if (buttonImage != null)
            {
                buttonImage.color = Color.Lerp(startColor, hoverColor, t);

                // 发光效果
                if (enableGlowEffect && buttonMaterial != null)
                {
                    float glowValue = Mathf.Lerp(1f, glowIntensity, t);
                    buttonMaterial.SetFloat("_GlowPower", glowValue);
                }
            }

            // 文本颜色过渡
            if (buttonText != null)
            {
                buttonText.color = Color.Lerp(startTextColor, Color.white, t);
            }

            yield return null;
        }

        // 确保最终状态
        transform.localScale = targetScale;
        if (buttonImage != null)
            buttonImage.color = hoverColor;
        if (buttonText != null)
            buttonText.color = Color.white;

        currentAnimation = null;
    }

    IEnumerator HoverExitAnimation()
    {
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;
        Color startColor = buttonImage != null ? buttonImage.color : normalColor;
        Color startTextColor = buttonText != null ? buttonText.color : originalTextColor;

        // 停止粒子效果
        if (hoverParticles != null && hoverParticles.isPlaying)
            hoverParticles.Stop();

        // 隐藏选择指示器
        if (selectionIndicator != null)
            selectionIndicator.SetActive(false);

        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / animationDuration;
            float easedT = EaseOutQuad(t);

            // 缩放动画
            transform.localScale = Vector3.Lerp(startScale, originalScale, easedT);

            // 按钮颜色过渡
            if (buttonImage != null)
            {
                buttonImage.color = Color.Lerp(startColor, normalColor, t);

                // 发光效果
                if (enableGlowEffect && buttonMaterial != null)
                {
                    float glowValue = Mathf.Lerp(glowIntensity, 1f, t);
                    buttonMaterial.SetFloat("_GlowPower", glowValue);
                }
            }

            // 文本颜色过渡
            if (buttonText != null)
            {
                buttonText.color = Color.Lerp(startTextColor, originalTextColor, t);
            }

            yield return null;
        }

        // 确保最终状态
        transform.localScale = originalScale;
        if (buttonImage != null)
            buttonImage.color = normalColor;
        if (buttonText != null)
            buttonText.color = originalTextColor;

        currentAnimation = null;
    }

    IEnumerator ClickAnimation()
    {
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;
        Vector3 targetScale = originalScale * clickScale;
        Color startColor = buttonImage != null ? buttonImage.color : hoverColor;

        // 播放点击音效
        PlaySound(clickSound);

        // 按下阶段
        while (elapsed < clickAnimationDuration * 0.5f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / (clickAnimationDuration * 0.5f);

            transform.localScale = Vector3.Lerp(startScale, targetScale, t);

            if (buttonImage != null)
                buttonImage.color = Color.Lerp(startColor, pressedColor, t);

            yield return null;
        }

        // 弹回阶段
        elapsed = 0f;
        startScale = transform.localScale;
        Vector3 finalScale = isHovering ? originalScale * hoverScale : originalScale;
        startColor = buttonImage != null ? buttonImage.color : pressedColor;
        Color finalColor = isHovering ? hoverColor : normalColor;

        while (elapsed < clickAnimationDuration * 0.5f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / (clickAnimationDuration * 0.5f);
            float easedT = EaseOutBack(t);

            transform.localScale = Vector3.Lerp(startScale, finalScale, easedT);

            if (buttonImage != null)
                buttonImage.color = Color.Lerp(startColor, finalColor, t);

            yield return null;
        }

        // 确保最终状态
        transform.localScale = finalScale;
        if (buttonImage != null)
            buttonImage.color = finalColor;

        currentAnimation = null;
    }

    // 缓动函数
    private float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    private float EaseOutQuad(float t)
    {
        return 1f - (1f - t) * (1f - t);
    }

    // 工具方法
    private void StopCurrentAnimation()
    {
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
            currentAnimation = null;
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    // 公共接口
    public void SetInteractable(bool interactable)
    {
        if (isInteractable == interactable) return;

        isInteractable = interactable;

        StopCurrentAnimation();

        if (interactable)
        {
            currentAnimation = StartCoroutine(EnableAnimation());
        }
        else
        {
            currentAnimation = StartCoroutine(DisableAnimation());
        }
    }

    IEnumerator EnableAnimation()
    {
        float elapsed = 0f;
        Color startColor = buttonImage != null ? buttonImage.color : disabledColor;

        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / animationDuration;

            if (buttonImage != null)
                buttonImage.color = Color.Lerp(startColor, normalColor, t);
            if (buttonText != null)
                buttonText.color = Color.Lerp(disabledColor, originalTextColor, t);

            yield return null;
        }

        if (buttonImage != null)
            buttonImage.color = normalColor;
        if (buttonText != null)
            buttonText.color = originalTextColor;
    }

    IEnumerator DisableAnimation()
    {
        float elapsed = 0f;
        Color startColor = buttonImage != null ? buttonImage.color : normalColor;
        Color startTextColor = buttonText != null ? buttonText.color : originalTextColor;

        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / animationDuration;

            if (buttonImage != null)
                buttonImage.color = Color.Lerp(startColor, disabledColor, t);
            if (buttonText != null)
                buttonText.color = Color.Lerp(startTextColor, disabledColor, t);

            yield return null;
        }

        if (buttonImage != null)
            buttonImage.color = disabledColor;
        if (buttonText != null)
            buttonText.color = disabledColor;
    }

    // 强制重置状态
    public void ResetState()
    {
        if (!isInitialized) return;

        StopCurrentAnimation();
        ResetToNormalStateImmediately();
    }

    void OnDisable()
    {
        // 当对象被禁用时重置状态
        ResetState();
        isHovering = false;
    }

    void OnEnable()
    {
        // 当对象被启用时确保状态正确
        if (isInitialized)
        {
            ResetToNormalStateImmediately();
        }
    }

    // 测试方法
    [ContextMenu("检查按钮状态")]
    public void CheckButtonState()
    {
        Debug.Log($"=== 按钮 {name} 状态检查 ===");
        Debug.Log($"激活状态: {gameObject.activeInHierarchy}");
        Debug.Log($"位置: {transform.position}");
        Debug.Log($"缩放: {transform.localScale}");
        Debug.Log($"原始缩放: {originalScale}");

        if (buttonImage != null)
        {
            Debug.Log($"图片颜色: {buttonImage.color}");
            Debug.Log($"图片启用: {buttonImage.enabled}");
            Debug.Log($"图片透明度: {buttonImage.color.a}");
        }
        else
        {
            Debug.LogWarning("没有找到 Image 组件");
        }

        if (buttonText != null)
        {
            Debug.Log($"文本颜色: {buttonText.color}");
            Debug.Log($"文本启用: {buttonText.enabled}");
            Debug.Log($"文本内容: {buttonText.text}");
        }
        else
        {
            Debug.LogWarning("没有找到 TextMeshProUGUI 组件");
        }
    }
}
