using UnityEngine;
using UnityEngine.SceneManagement;

public class SimpleSceneLoader : MonoBehaviour
{
    public void LoadSampleScene()
    {
        // ֱ�ӳ��Լ���SampleScene
        SceneManager.LoadScene("Main Scenes");
    }

    // ���÷���������������
    public void LoadSceneByIndex(int sceneIndex)
    {
        SceneManager.LoadScene(sceneIndex);
    }
}