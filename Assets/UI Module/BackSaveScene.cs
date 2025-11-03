using UnityEngine;
using UnityEngine.SceneManagement;

public class BackSaveScene : MonoBehaviour
{
    public void LoadBackSaveScene()
    {
        // 直接加载自己设定的SampleScene
        SceneManager.LoadScene("SaveScenes");
    }

    // 提供按索引加载场景的方法
    public void LoadSceneByIndex(int sceneIndex)
    {
        SceneManager.LoadScene(sceneIndex);
    }
}
