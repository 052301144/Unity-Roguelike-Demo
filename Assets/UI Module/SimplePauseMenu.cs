using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SimplePauseMenu : MonoBehaviour
{
    [Header("UI组件")]
    public GameObject menuPanel;
    public GameObject settingsPanel; // 设置面板（可选）
    public Button menuButton;
    public Button resumeButton; // 继续游戏按钮
    public Button settingsButton; // 打开设置按钮
    public Button mainMenuButton; // 返回主菜单按钮
    public Button backFromSettingsButton; // 从设置面板返回按钮

    [Header("场景设置")]
    public string mainMenuScene = "StartMenuScene"; // 主菜单场景名称

    void Start()
    {
        // 确保菜单初始关闭
        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
        }

        // 确保设置面板关闭
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        // 绑定菜单按钮事件
        if (menuButton != null)
        {
            menuButton.onClick.AddListener(ToggleMenu);
        }

        // 绑定继续游戏按钮事件
        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(ResumeGame);
        }
        else
        {
            // 自动查找继续游戏按钮
            FindResumeButton();
        }

        // 绑定设置按钮事件
        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(OpenSettings);
        }
        else
        {
            // 自动查找设置按钮
            FindSettingsButton();
        }

        // 绑定返回主菜单按钮事件
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        }
        else
        {
            // 自动查找返回主菜单按钮
            FindMainMenuButton();
        }

        // 绑定从设置返回按钮事件
        if (backFromSettingsButton != null)
        {
            backFromSettingsButton.onClick.AddListener(CloseSettings);
        }
        else
        {
            // 自动查找从设置返回按钮
            FindBackFromSettingsButton();
        }
    }

    void FindResumeButton()
    {
        // 在菜单面板内部查找继续游戏按钮
        if (menuPanel != null)
        {
            Transform resumeBtnTransform = menuPanel.transform.Find("ResumeButton");
            if (resumeBtnTransform != null)
            {
                resumeButton = resumeBtnTransform.GetComponent<Button>();
            }

            if (resumeButton != null)
            {
                resumeButton.onClick.AddListener(ResumeGame);
                Debug.Log("自动找到继续游戏按钮: " + resumeButton.name);
            }
        }
    }

    void FindSettingsButton()
    {
        // 在菜单面板内部查找设置按钮
        if (menuPanel != null)
        {
            Transform settingsBtnTransform = menuPanel.transform.Find("SettingsButton");
            if (settingsBtnTransform == null)
            {
                settingsBtnTransform = menuPanel.transform.Find("SettingButton");
            }

            if (settingsBtnTransform != null)
            {
                settingsButton = settingsBtnTransform.GetComponent<Button>();
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(OpenSettings);
                Debug.Log("自动找到设置按钮: " + settingsButton.name);
            }
        }
    }

    void FindMainMenuButton()
    {
        // 在菜单面板内部查找返回主菜单按钮
        if (menuPanel != null)
        {
            Transform mainMenuBtnTransform = menuPanel.transform.Find("MainMenuButton");
            if (mainMenuBtnTransform == null)
            {
                mainMenuBtnTransform = menuPanel.transform.Find("QuitToMenuButton");
            }
            if (mainMenuBtnTransform == null)
            {
                mainMenuBtnTransform = menuPanel.transform.Find("ReturnToMenuButton");
            }

            if (mainMenuBtnTransform != null)
            {
                mainMenuButton = mainMenuBtnTransform.GetComponent<Button>();
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.AddListener(ReturnToMainMenu);
                Debug.Log("自动找到返回主菜单按钮: " + mainMenuButton.name);
            }
        }
    }

    void FindBackFromSettingsButton()
    {
        // 在设置面板内部查找返回按钮
        if (settingsPanel != null)
        {
            Transform backBtnTransform = settingsPanel.transform.Find("BackButton");
            if (backBtnTransform == null)
            {
                backBtnTransform = settingsPanel.transform.Find("CloseButton");
            }
            if (backBtnTransform == null)
            {
                backBtnTransform = settingsPanel.transform.Find("ReturnButton");
            }

            if (backBtnTransform != null)
            {
                backFromSettingsButton = backBtnTransform.GetComponent<Button>();
            }

            if (backFromSettingsButton != null)
            {
                backFromSettingsButton.onClick.AddListener(CloseSettings);
                Debug.Log("自动找到设置返回按钮: " + backFromSettingsButton.name);
            }
        }
    }

    void ToggleMenu()
    {
        if (menuPanel.activeInHierarchy)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    // 暂停游戏并显示菜单
    void PauseGame()
    {
        if (menuPanel != null)
        {
            menuPanel.SetActive(true);
        }

        // 确保设置面板关闭
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        Time.timeScale = 0f; // 暂停游戏
        AudioListener.pause = true;

        Debug.Log("游戏已暂停");
    }

    // 继续游戏并关闭菜单
    public void ResumeGame()
    {
        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
        }

        // 确保设置面板关闭
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        Time.timeScale = 1f; // 恢复游戏
        AudioListener.pause = false;

        Debug.Log("游戏已继续");
    }

    // 打开设置面板
    public void OpenSettings()
    {
        Debug.Log("打开设置面板");

        // 关闭菜单面板
        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
        }

        // 显示设置面板
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("设置面板未指定");
        }
    }

    // 关闭设置面板，返回菜单
    public void CloseSettings()
    {
        Debug.Log("关闭设置面板");

        // 关闭设置面板
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        // 显示菜单面板
        if (menuPanel != null)
        {
            menuPanel.SetActive(true);
        }
    }

    // 返回主菜单
    public void ReturnToMainMenu()
    {
        Debug.Log("返回主菜单按钮被点击");

        // 取消游戏暂停
        Time.timeScale = 1f;
        AudioListener.pause = false;

        Debug.Log("游戏暂停已取消，正在加载主菜单...");

        // 加载主菜单场景
        if (!string.IsNullOrEmpty(mainMenuScene))
        {
            SceneManager.LoadScene(mainMenuScene);
        }
        else
        {
            Debug.LogError("主菜单场景名称未设置");
        }
    }

    public bool IsMenuOpen()
    {
        return menuPanel != null && menuPanel.activeInHierarchy;
    }

    public bool IssettingsOpen()
    {
        return settingsPanel != null && settingsPanel.activeInHierarchy;
    }

    // 在编辑器中测试
    [ContextMenu("测试暂停")]
    void TestPause() => PauseGame();

    [ContextMenu("测试继续")]
    void TestResume() => ResumeGame();

    [ContextMenu("测试打开设置")]
    void TestOpenSettings() => OpenSettings();

    [ContextMenu("测试关闭设置")]
    void TestCloseSettings() => CloseSettings();

    [ContextMenu("测试返回主菜单")]
    void TestReturnToMainMenu() => ReturnToMainMenu();
}
