using UnityEngine;
using UnityEngine.SceneManagement;

public class SimpleSceneLoader : MonoBehaviour
{
    [Header("场景加载设置")]
    [SerializeField] private string defaultSceneName = "Main Scenes"; // 默认场景名称

    /// <summary>
    /// 加载默认场景
    /// </summary>
    public void LoadDefaultScene()
    {
        LoadScene(defaultSceneName);
    }

    /// <summary>
    /// 通过场景名称加载场景
    /// </summary>
    /// <param name="sceneName">要加载的场景名称</param>
    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("场景名称不能为空！");
            return;
        }

        Debug.Log($"正在加载场景: {sceneName}");
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// 通过场景索引加载场景
    /// </summary>
    /// <param name="sceneIndex">要加载的场景索引</param>
    public void LoadSceneByIndex(int sceneIndex)
    {
        int sceneCount = SceneManager.sceneCountInBuildSettings;

        if (sceneIndex < 0 || sceneIndex >= sceneCount)
        {
            Debug.LogError($"场景索引 {sceneIndex} 无效！有效范围: 0 - {sceneCount - 1}");
            return;
        }

        Debug.Log($"正在加载场景索引: {sceneIndex}");
        SceneManager.LoadScene(sceneIndex);
    }

    /// <summary>
    /// 重新加载当前场景
    /// </summary>
    public void ReloadCurrentScene()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        Debug.Log($"重新加载当前场景: {currentSceneName}");
        SceneManager.LoadScene(currentSceneName);
    }

    /// <summary>
    /// 异步加载场景（不阻塞主线程）
    /// </summary>
    /// <param name="sceneName">要加载的场景名称</param>
    public void LoadSceneAsync(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("场景名称不能为空！");
            return;
        }

        Debug.Log($"正在异步加载场景: {sceneName}");
        SceneManager.LoadSceneAsync(sceneName);
    }

    /// <summary>
    /// 检查场景是否存在
    /// </summary>
    /// <param name="sceneName">场景名称</param>
    /// <returns>是否存在</returns>
    public bool SceneExists(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneNameInBuild = System.IO.Path.GetFileNameWithoutExtension(scenePath);

            if (sceneNameInBuild == sceneName)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 安全加载场景（先检查是否存在）
    /// </summary>
    /// <param name="sceneName">场景名称</param>
    public void LoadSceneSafe(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("场景名称不能为空！");
            return;
        }

        if (SceneExists(sceneName))
        {
            Debug.Log($"安全加载场景: {sceneName}");
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogError($"场景 '{sceneName}' 不存在于Build Settings中！");
            // 可选：加载默认场景作为回退
            Debug.Log($"加载默认场景: {defaultSceneName}");
            SceneManager.LoadScene(defaultSceneName);
        }
    }

    // 上下文菜单测试方法
    [ContextMenu("测试加载默认场景")]
    private void TestLoadDefaultScene()
    {
        LoadDefaultScene();
    }

    [ContextMenu("测试重新加载当前场景")]
    private void TestReloadCurrentScene()
    {
        ReloadCurrentScene();
    }

    [ContextMenu("显示所有可用场景")]
    private void ShowAvailableScenes()
    {
        Debug.Log("=== 可用场景列表 ===");
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            Debug.Log($"[{i}] {sceneName}");
        }
    }
}