using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 选关界面管理器（适配map_X命名规则）
/// 负责关卡按钮状态管理、解锁逻辑和场景切换
/// </summary>
public class LevelSelectManager : MonoBehaviour
{
    [Header("UI引用")]
    [SerializeField] private Button[] levelButtons;      // 所有关卡按钮数组
    [SerializeField] private Button backToMenuButton;    // 返回主菜单按钮
    [SerializeField] private GameObject lockIconPrefab;  // 锁图标预制体

    [Header("游戏设置")]
    [SerializeField] private string mainMenuSceneName = "MainMenu"; // 主菜单场景名
    [SerializeField] private string levelScenePrefix = "map_";      // 关卡场景前缀（map_）

    private int maxUnlockedLevel;  // 当前最高解锁关卡

    private void Awake()
    {
        // 注册按钮事件
        backToMenuButton?.onClick.AddListener(BackToMainMenu);
    }

    private void Start()
    {
        // 初始化关卡数据
        LoadLevelProgress();
        UpdateLevelButtons();
    }

    /// <summary>
    /// 加载关卡进度数据
    /// </summary>
    private void LoadLevelProgress()
    {
        // 从PlayerPrefs读取最高解锁关卡，默认解锁第一关
        maxUnlockedLevel = PlayerPrefs.GetInt("MaxUnlockedLevel", 1);

        // 确保解锁关卡数不超过总关卡数
        if (maxUnlockedLevel > levelButtons.Length)
        {
            maxUnlockedLevel = levelButtons.Length;
        }
    }

    /// <summary>
    /// 更新所有关卡按钮的显示状态
    /// </summary>
    private void UpdateLevelButtons()
    {
        for (int i = 0; i < levelButtons.Length; i++)
        {
            int currentLevel = i + 1;
            Button levelButton = levelButtons[i];

            // 设置按钮交互状态
            bool isUnlocked = currentLevel <= maxUnlockedLevel;
            levelButton.interactable = isUnlocked;

            // 添加/移除锁图标
            Transform lockIcon = levelButton.transform.Find("LockIcon");
            if (isUnlocked)
            {
                // 解锁状态 - 移除锁图标
                if (lockIcon != null)
                {
                    Destroy(lockIcon.gameObject);
                }

                // 设置关卡按钮点击事件
                levelButton.onClick.RemoveAllListeners();
                levelButton.onClick.AddListener(() => LoadLevel(currentLevel));

                // 检查是否通关，显示通关标识
                UpdateCompletedIcon(levelButton, currentLevel);
            }
            else
            {
                // 锁定状态 - 添加锁图标
                if (lockIcon == null && lockIconPrefab != null)
                {
                    Instantiate(lockIconPrefab, levelButton.transform);
                }
            }
        }
    }

    /// <summary>
    /// 更新通关标识显示
    /// </summary>
    private void UpdateCompletedIcon(Button levelButton, int level)
    {
        Transform completedIcon = levelButton.transform.Find("CompletedIcon");

        if (completedIcon != null)
        {
            // 检查该关卡是否已通关
            bool isCompleted = PlayerPrefs.GetInt($"Level_{level}_Completed", 0) == 1;
            completedIcon.gameObject.SetActive(isCompleted);
        }
    }

    /// <summary>
    /// 加载指定关卡（适配map_X命名）
    /// </summary>
    private void LoadLevel(int levelIndex)
    {
        string sceneName = $"{levelScenePrefix}{levelIndex}";

        // 检查场景是否存在
        if (IsSceneExists(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogError($"场景 {sceneName} 不存在！");
        }
    }

    /// <summary>
    /// 返回主菜单
    /// </summary>
    public void BackToMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }

    /// <summary>
    /// 检查场景是否存在
    /// </summary>
    private bool IsSceneExists(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            Scene scene = SceneManager.GetSceneByBuildIndex(i);
            string sceneNameInBuild = scene.name;

            if (sceneNameInBuild == sceneName)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 重置所有关卡进度（用于测试）
    /// </summary>
    [ContextMenu("Reset Level Progress")]
    public void ResetLevelProgress()
    {
        PlayerPrefs.DeleteKey("MaxUnlockedLevel");

        for (int i = 1; i <= levelButtons.Length; i++)
        {
            PlayerPrefs.DeleteKey($"Level_{i}_Completed");
        }

        PlayerPrefs.Save();
        LoadLevelProgress();
        UpdateLevelButtons();

        Debug.Log("关卡进度已重置！");
    }
}