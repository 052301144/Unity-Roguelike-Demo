using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class InventorySystem : MonoBehaviour
{
    [Header("背包UI")]
    public GameObject inventoryPanel; // 背包面板
    public Button closeButton; // 关闭按钮（可选）

    [Header("操作设置")]
    public KeyCode toggleKey = KeyCode.B; // 切换快捷键
    public bool pauseGameWhenOpen = true; // 打开时是否暂停游戏

    private bool isInventoryOpen = false;
    private CanvasGroup canvasGroup; // 用于淡入淡出效果（可选）

    void Start()
    {
        InitializeInventory();
    }

    void InitializeInventory()
    {
        // 确保背包初始时关闭
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);

            // 创建CanvasGroup用于可能的淡入效果
            canvasGroup = inventoryPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = inventoryPanel.AddComponent<CanvasGroup>();
            }
        }

        // 绑定关闭按钮事件
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseInventory);
        }
        else
        {
            // 自动查找关闭按钮
            FindCloseButton();
        }

        Debug.Log("背包系统初始化完成，按 " + toggleKey + " 打开/关闭");
    }

    void Update()
    {
        // 检测快捷键
        if (Input.GetKeyDown(toggleKey))
        {
            // 检查暂停菜单是否打开
            SimplePauseMenu pauseMenu = FindObjectOfType<SimplePauseMenu>();
            SimplePauseMenu pauseSetting = FindObjectOfType<SimplePauseMenu>();
            if (pauseMenu != null && pauseMenu.IsMenuOpen()|| pauseSetting != null && pauseSetting.IssettingsOpen())
            {
                Debug.Log("暂停菜单已打开，无法打开背包");
                return; // 如果暂停菜单打开，不执行背包操作
            }

            ToggleInventory();
        }

        // ESC键也可以关闭背包（可选）
        if (isInventoryOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseInventory();
        }
    }

    void FindCloseButton()
    {
        if (inventoryPanel != null)
        {
            Transform closeBtnTransform = inventoryPanel.transform.Find("CloseButton");
            if (closeBtnTransform == null)
            {
                closeBtnTransform = inventoryPanel.transform.Find("ExitButton");
            }
            if (closeBtnTransform == null)
            {
                closeBtnTransform = inventoryPanel.transform.Find("BackButton");
            }

            if (closeBtnTransform != null)
            {
                closeButton = closeBtnTransform.GetComponent<Button>();
                if (closeButton != null)
                {
                    closeButton.onClick.AddListener(CloseInventory);
                    Debug.Log("自动找到关闭按钮: " + closeButton.name);
                }
            }
        }
    }

    // 切换背包显示/隐藏
    public void ToggleInventory()
    {
        if (isInventoryOpen)
        {
            CloseInventory();
        }
        else
        {
            OpenInventory();
        }
    }

    // 打开背包
    public void OpenInventory()
    {
        if (inventoryPanel != null && !isInventoryOpen)
        {
            inventoryPanel.SetActive(true);
            isInventoryOpen = true;

            // 暂停游戏
            if (pauseGameWhenOpen)
            {
                Time.timeScale = 0f;
                AudioListener.pause = true;
            }

            // 可选：播放打开动画
            StartCoroutine(PlayOpenAnimation());

            Debug.Log("背包已打开");
        }
    }

    // 关闭背包
    public void CloseInventory()
    {
        if (inventoryPanel != null && isInventoryOpen)
        {
            // 可选：播放关闭动画
            StartCoroutine(PlayCloseAnimation());

            Debug.Log("背包已关闭");
        }
    }

    // 播放打开动画（可选）
    IEnumerator PlayOpenAnimation()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            inventoryPanel.SetActive(true);

            float duration = 0.2f;
            float timer = 0f;

            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime; // 使用unscaled时间，因为游戏可能被暂停
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / duration);
                yield return null;
            }

            canvasGroup.alpha = 1f;
        }
    }

    // 播放关闭动画（可选）
    IEnumerator PlayCloseAnimation()
    {
        if (canvasGroup != null)
        {
            float duration = 0.15f;
            float timer = 0f;
            float startAlpha = canvasGroup.alpha;

            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, timer / duration);
                yield return null;
            }
        }

        inventoryPanel.SetActive(false);
        isInventoryOpen = false;

        // 恢复游戏
        if (pauseGameWhenOpen)
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }
    }

    // 强制关闭背包（在需要时调用）
    public void ForceCloseInventory()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
            isInventoryOpen = false;

            // 恢复游戏
            Time.timeScale = 1f;
            AudioListener.pause = false;

            Debug.Log("背包强制关闭");
        }
    }

    // 检查背包是否打开，供其他脚本查询
    public bool IsInventoryOpen()
    {
        return isInventoryOpen;
    }

    // 在编辑器中测试的方法
    [ContextMenu("测试打开背包")]
    public void TestOpenInventory()
    {
        OpenInventory();
    }

    [ContextMenu("测试关闭背包")]
    public void TestCloseInventory()
    {
        CloseInventory();
    }
}
