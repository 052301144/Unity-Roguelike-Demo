using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    [Header("UI ���")]
    public GameObject settingsPanel;    // �������
    public Button settingsButton;       // ���ð�ť
    public Button closeButton;          // �رհ�ť
    public Button confirmButton;        // ȷ�ϰ�ť

    [Header("���ÿؼ�")]
    public Slider volumeSlider;         // ��������
    public TMP_Dropdown graphicsDropdown; // ����������
    public TMP_Text volumeValueText;    // ����ֵ��ʾ�ı�

    void Start()
    {
        // ��ʼ������
        settingsPanel.SetActive(false);

        // �󶨰�ť�¼�
        settingsButton.onClick.AddListener(OpenSettings);
        closeButton.onClick.AddListener(CloseSettings);
        confirmButton.onClick.AddListener(ConfirmSettings);

        // �󶨻����¼�
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);

        // ���ر��������
        LoadSettings();
    }

    // ���������
    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
        // ��ѡ����ͣ��Ϸ
        // Time.timeScale = 0f;
    }

    // �ر��������
    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
        // �ָ���Ϸ
        // Time.timeScale = 1f;
    }

    // ȷ������
    public void ConfirmSettings()
    {
        SaveSettings();
        CloseSettings();
        Debug.Log("�����ѱ��沢Ӧ��");
    }

    // �����仯�ص�
    void OnVolumeChanged(float value)
    {
        if (volumeValueText != null)
            volumeValueText.text = $"{value:F0}%";

        // ʵʱӦ����������
        AudioListener.volume = value / 100f;
    }

    // ��������
    void LoadSettings()
    {
        // ��������
        float savedVolume = PlayerPrefs.GetFloat("Volume", 80f);
        volumeSlider.value = savedVolume;

        // ���ػ�������
        int graphicsQuality = PlayerPrefs.GetInt("GraphicsQuality", 2);
        graphicsDropdown.value = graphicsQuality;

        // Ӧ������
        OnVolumeChanged(savedVolume);
        QualitySettings.SetQualityLevel(graphicsQuality);
    }

    // ��������
    void SaveSettings()
    {
        PlayerPrefs.SetFloat("Volume", volumeSlider.value);
        PlayerPrefs.SetInt("GraphicsQuality", graphicsDropdown.value);
        PlayerPrefs.Save();
    }

    // ���ʸı�ص�
    public void OnGraphicsQualityChanged(int qualityLevel)
    {
        QualitySettings.SetQualityLevel(qualityLevel);
    }
}