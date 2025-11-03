using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuitGameManager : MonoBehaviour
{
    [Header("退出按钮")]
    public Button quitButton;

    void Start()
    {
        // 如果没有指定按钮，自动查找
        if (quitButton == null)
        {
            quitButton = GameObject.Find("退出游戏Button")?.GetComponent<Button>();
            // 或者更通用的查找方式
            if (quitButton == null)
            {
                Button[] allButtons = FindObjectsOfType<Button>();
                foreach (Button btn in allButtons)
                {
                    TextMeshProUGUI btnText = btn.GetComponentInChildren<TextMeshProUGUI>();
                    if (btnText != null && btnText.text == "退出游戏")
                    {
                        quitButton = btn;
                        break;
                    }
                }
            }
        }

        // 绑定点击事件
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(QuitGame);
            Debug.Log("退出按钮事件绑定成功");
        }
        else
        {
            Debug.LogError("没有找到退出游戏按钮");
        }
    }

    public void QuitGame()
    {
        Debug.Log("退出游戏按钮被点击");

#if UNITY_EDITOR
        // 在编辑器中停止播放
        UnityEditor.EditorApplication.isPlaying = false;
        Debug.Log("编辑器模式下停止播放");
#else
            // 在构建版本中退出应用程序
            Application.Quit();
            Debug.Log("应用程序退出游戏");
#endif
    }
}
