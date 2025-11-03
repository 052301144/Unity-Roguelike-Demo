using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    [Header("UI 组件")]
    public GameObject settingsPanel;    // 设置面板
    public Button settingsButton;       // 设置按钮
    public Button closeButton;          // 关闭按钮
    public Button confirmButton;        // 确认按钮

    [Header("设置控件")]
    public Slider volumeSlider;         // 音量滑块
    public TMP_Dropdown graphicsDropdown; // 图形质量下拉
    public TMP_Text volumeValueText;    // 音量值显示文本

    void Start()
    {
        // 初始化设置
        settingsPanel.SetActive(false);

        // 绑定按钮事件
        settingsButton.onClick.AddListener(OpenSettings);
        closeButton.onClick.AddListener(CloseSettings);
        confirmButton.onClick.AddListener(ConfirmSettings);

        // 绑定滑块事件
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);

        // 加载保存的设置
        LoadSettings();
    }

    // 打开设置面板
    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
        // 可选：暂停游戏
        // Time.timeScale = 0f;
    }

    // 关闭设置面板
    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
        // 恢复游戏
        // Time.timeScale = 1f;
    }

    // 确认设置
    public void ConfirmSettings()
    {
        SaveSettings();
        CloseSettings();
        Debug.Log("设置已被保存并应用");
    }

    // 音量变化回调
    void OnVolumeChanged(float value)
    {
        if (volumeValueText != null)
            volumeValueText.text = $"{value:F0}%";

        // 实时应用音量变化
        AudioListener.volume = value / 100f;
    }

    // 加载设置
    void LoadSettings()
    {
        // 加载音量
        float savedVolume = PlayerPrefs.GetFloat("Volume", 80f);
        volumeSlider.value = savedVolume;

        // 加载图形质量
        int graphicsQuality = PlayerPrefs.GetInt("GraphicsQuality", 2);
        graphicsDropdown.value = graphicsQuality;

        // 应用设置
        OnVolumeChanged(savedVolume);
        QualitySettings.SetQualityLevel(graphicsQuality);
    }

    // 保存设置
    void SaveSettings()
    {
        PlayerPrefs.SetFloat("Volume", volumeSlider.value);
        PlayerPrefs.SetInt("GraphicsQuality", graphicsDropdown.value);
        PlayerPrefs.Save();
    }

    // 图形质量改变回调
    public void OnGraphicsQualityChanged(int qualityLevel)
    {
        QualitySettings.SetQualityLevel(qualityLevel);
    }
}