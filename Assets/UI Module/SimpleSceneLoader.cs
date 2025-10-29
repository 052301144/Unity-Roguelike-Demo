using UnityEngine;
using UnityEngine.SceneManagement;

public class SimpleSceneLoader : MonoBehaviour
{
    public void LoadSampleScene()
    {
        // ֱ�ӳ��Լ���SampleScene
        SceneManager.LoadScene("SaveScenes");
    }

    // ���÷���������������
    public void LoadSceneByIndex(int sceneIndex)
    {
        SceneManager.LoadScene(sceneIndex);
    }
}