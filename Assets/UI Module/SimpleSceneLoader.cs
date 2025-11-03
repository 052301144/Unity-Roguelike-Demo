using UnityEngine;
using UnityEngine.SceneManagement;

public class SimpleSceneLoader : MonoBehaviour
{
    public void LoadSampleScene()
    {
        // 直接加载自己设定的SampleScene
        SceneManager.LoadScene("Main Scenes");
    }

    // 提供按索引加载场景的方法
    public void LoadSceneByIndex(int sceneIndex)
    {
        SceneManager.LoadScene(sceneIndex);
    }
}
