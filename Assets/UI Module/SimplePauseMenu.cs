using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SimplePauseMenu : MonoBehaviour
{
    [Header("UI���")]
    public GameObject menuPanel;
    public GameObject settingsPanel; // �������������
    public Button menuButton;
    public Button resumeButton; // ������Ϸ��ť
    public Button settingsButton; // ���������ð�ť
    public Button mainMenuButton; // �������˵���ť
    public Button backFromSettingsButton; // �����������÷��ذ�ť

    [Header("��������")]
    public string mainMenuScene = "StartMenuScene"; // ���˵���������

    void Start()
    {
        // ȷ���˵��������
        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
        }

        // ȷ�������������
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        // �󶨲˵���ť�¼�
        if (menuButton != null)
        {
            menuButton.onClick.AddListener(ToggleMenu);
        }

        // �󶨷�����Ϸ��ť�¼�
        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(ResumeGame);
        }
        else
        {
            // �Զ����ҷ�����Ϸ��ť
            FindResumeButton();
        }

        // �����ð�ť�¼�
        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(OpenSettings);
        }
        else
        {
            // �Զ��������ð�ť
            FindSettingsButton();
        }

        // �󶨷������˵���ť�¼�
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        }
        else
        {
            // �Զ����ҷ������˵���ť
            FindMainMenuButton();
        }

        // �󶨴����÷��ذ�ť�¼�
        if (backFromSettingsButton != null)
        {
            backFromSettingsButton.onClick.AddListener(CloseSettings);
        }
        else
        {
            // �Զ����Ҵ����÷��ذ�ť
            FindBackFromSettingsButton();
        }
    }

    void FindResumeButton()
    {
        // �ڲ˵�����ڲ��ҷ�����Ϸ��ť
        if (menuPanel != null)
        {
            Transform resumeBtnTransform = menuPanel.transform.Find("ResumeButton");
            if (resumeBtnTransform != null)
            {
                resumeButton = resumeBtnTransform.GetComponent<Button>();
            }

            if (resumeButton != null)
            {
                resumeButton.onClick.AddListener(ResumeGame);
                Debug.Log("�Զ��ҵ�������Ϸ��ť: " + resumeButton.name);
            }
        }
    }

    void FindSettingsButton()
    {
        // �ڲ˵�����ڲ������ð�ť
        if (menuPanel != null)
        {
            Transform settingsBtnTransform = menuPanel.transform.Find("SettingsButton");
            if (settingsBtnTransform == null)
            {
                settingsBtnTransform = menuPanel.transform.Find("SettingButton");
            }

            if (settingsBtnTransform != null)
            {
                settingsButton = settingsBtnTransform.GetComponent<Button>();
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(OpenSettings);
                Debug.Log("�Զ��ҵ����ð�ť: " + settingsButton.name);
            }
        }
    }

    void FindMainMenuButton()
    {
        // �ڲ˵�����ڲ��ҷ������˵���ť
        if (menuPanel != null)
        {
            Transform mainMenuBtnTransform = menuPanel.transform.Find("MainMenuButton");
            if (mainMenuBtnTransform == null)
            {
                mainMenuBtnTransform = menuPanel.transform.Find("QuitToMenuButton");
            }
            if (mainMenuBtnTransform == null)
            {
                mainMenuBtnTransform = menuPanel.transform.Find("ReturnToMenuButton");
            }

            if (mainMenuBtnTransform != null)
            {
                mainMenuButton = mainMenuBtnTransform.GetComponent<Button>();
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.AddListener(ReturnToMainMenu);
                Debug.Log("�Զ��ҵ��������˵���ť: " + mainMenuButton.name);
            }
        }
    }

    void FindBackFromSettingsButton()
    {
        // ����������ڲ��ҷ��ذ�ť
        if (settingsPanel != null)
        {
            Transform backBtnTransform = settingsPanel.transform.Find("BackButton");
            if (backBtnTransform == null)
            {
                backBtnTransform = settingsPanel.transform.Find("CloseButton");
            }
            if (backBtnTransform == null)
            {
                backBtnTransform = settingsPanel.transform.Find("ReturnButton");
            }

            if (backBtnTransform != null)
            {
                backFromSettingsButton = backBtnTransform.GetComponent<Button>();
            }

            if (backFromSettingsButton != null)
            {
                backFromSettingsButton.onClick.AddListener(CloseSettings);
                Debug.Log("�Զ��ҵ����÷��ذ�ť: " + backFromSettingsButton.name);
            }
        }
    }

    void ToggleMenu()
    {
        if (menuPanel.activeInHierarchy)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    // ��ͣ��Ϸ����ʾ�˵�
    void PauseGame()
    {
        if (menuPanel != null)
        {
            menuPanel.SetActive(true);
        }

        // ȷ�������������
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        Time.timeScale = 0f; // ��ͣ��Ϸ
        AudioListener.pause = true;

        Debug.Log("��Ϸ����ͣ");
    }

    // ������Ϸ�����ز˵�
    public void ResumeGame()
    {
        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
        }

        // ȷ�������������
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        Time.timeScale = 1f; // �ָ���Ϸ
        AudioListener.pause = false;

        Debug.Log("��Ϸ�Ѽ���");
    }

    // ���������
    public void OpenSettings()
    {
        Debug.Log("���������");

        // ���ز˵����
        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
        }

        // ��ʾ�������
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("�������δָ����");
        }
    }

    // �ر�������壬���ز˵�
    public void CloseSettings()
    {
        Debug.Log("�ر��������");

        // �����������
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        // ��ʾ�˵����
        if (menuPanel != null)
        {
            menuPanel.SetActive(true);
        }
    }

    // �������˵�
    public void ReturnToMainMenu()
    {
        Debug.Log("�������˵���ť�����");

        // ȡ����Ϸ��ͣ
        Time.timeScale = 1f;
        AudioListener.pause = false;

        Debug.Log("��Ϸ��ͣ��ȡ�������ڼ������˵�...");

        // �������˵�����
        if (!string.IsNullOrEmpty(mainMenuScene))
        {
            SceneManager.LoadScene(mainMenuScene);
        }
        else
        {
            Debug.LogError("���������˵��������ƣ�");
        }
    }

    public bool IsMenuOpen()
    {
        return menuPanel != null && menuPanel.activeInHierarchy;
    }

    public bool IssettingsOpen()
    {
        return settingsPanel != null && settingsPanel.activeInHierarchy;
    }

    // �ڱ༭���в���
    [ContextMenu("������ͣ")]
    void TestPause() => PauseGame();

    [ContextMenu("���Լ���")]
    void TestResume() => ResumeGame();

    [ContextMenu("���Դ�����")]
    void TestOpenSettings() => OpenSettings();

    [ContextMenu("���Թر�����")]
    void TestCloseSettings() => CloseSettings();

    [ContextMenu("���Է������˵�")]
    void TestReturnToMainMenu() => ReturnToMainMenu();
}