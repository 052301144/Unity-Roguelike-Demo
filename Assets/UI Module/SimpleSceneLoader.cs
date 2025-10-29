using UnityEngine;
using UnityEngine.SceneManagement;

public class SimpleSceneLoader : MonoBehaviour
{
    public void LoadSampleScene()
    {
        // 直接尝试加载SampleScene
        SceneManager.LoadScene("SaveScenes");
    }

    // 备用方法：按索引加载
    public void LoadSceneByIndex(int sceneIndex)
    {
        SceneManager.LoadScene(sceneIndex);
    }
}