using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 关卡通关处理脚本（适配map_X命名规则）
/// 用于标记关卡完成并解锁下一关
/// </summary>
public class LevelCompletion : MonoBehaviour
{
    /// <summary>
    /// 标记当前关卡为已完成
    /// </summary>
    public void CompleteCurrentLevel()
    {
        // 获取当前关卡号（从map_X格式解析）
        string sceneName = SceneManager.GetActiveScene().name;
        string levelNumberStr = sceneName.Replace("map_", "");

        if (int.TryParse(levelNumberStr, out int currentLevel))
        {
            // 标记当前关卡为已通关
            PlayerPrefs.SetInt($"Level_{currentLevel}_Completed", 1);

            // 解锁下一关
            int nextLevel = currentLevel + 1;
            int currentMaxUnlocked = PlayerPrefs.GetInt("MaxUnlockedLevel", 1);

            if (nextLevel > currentMaxUnlocked)
            {
                PlayerPrefs.SetInt("MaxUnlockedLevel", nextLevel);
            }

            PlayerPrefs.Save();

            Debug.Log($"关卡 {currentLevel} (map_{currentLevel}) 已完成！下一关 {nextLevel} 已解锁！");
        }
        else
        {
            Debug.LogError($"无法从场景名 {sceneName} 解析关卡号！");
        }
    }

    // 在通关条件达成时调用此方法（如击败Boss、到达终点）
    // 例如：
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            CompleteCurrentLevel();
            // 加载下一关或胜利界面
            // SceneManager.LoadScene("LevelComplete");
        }
    }
}