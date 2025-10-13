using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuitGameManager : MonoBehaviour
{
    [Header("�˳���ť")]
    public Button quitButton;

    void Start()
    {
        // ���û��ָ����ť���Զ�����
        if (quitButton == null)
        {
            quitButton = GameObject.Find("�˳���ϷButton")?.GetComponent<Button>();
            // ���߸������ֲ���
            if (quitButton == null)
            {
                Button[] allButtons = FindObjectsOfType<Button>();
                foreach (Button btn in allButtons)
                {
                    TextMeshProUGUI btnText = btn.GetComponentInChildren<TextMeshProUGUI>();
                    if (btnText != null && btnText.text == "�˳���Ϸ")
                    {
                        quitButton = btn;
                        break;
                    }
                }
            }
        }

        // �󶨵���¼�
        if (quitButton != null)
        {
            quitButton.onClick.AddListener(QuitGame);
            Debug.Log("�˳���ť�¼��󶨳ɹ�");
        }
        else
        {
            Debug.LogError("û���ҵ��˳���Ϸ��ť��");
        }
    }

    public void QuitGame()
    {
        Debug.Log("�˳���Ϸ��ť�����");

#if UNITY_EDITOR
        // �ڱ༭����ֹͣ����
        UnityEditor.EditorApplication.isPlaying = false;
        Debug.Log("�༭��ģʽ��ֹͣ����");
#else
            // �ڴ�������Ϸ���˳�����
            Application.Quit();
            Debug.Log("Ӧ�ó����˳���Ϸ");
#endif
    }
}