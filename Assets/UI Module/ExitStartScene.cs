using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitStartScene : MonoBehaviour
{
    public void LoadExitStartScene()
    {
        // 直接加载自己设定的SampleScene
        SceneManager.LoadScene("StartMenuScene");
    }

    // 提供按索引加载场景的方法
    public void LoadSceneByIndex(int sceneIndex)
    {
        SceneManager.LoadScene(sceneIndex);
    }
}
