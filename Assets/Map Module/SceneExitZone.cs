using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 放在地图右侧的触发区：玩家进入后按下指定按键切换到目标场景。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class SceneExitZone : MonoBehaviour
{
    [SerializeField] private string targetSceneName = "SelectScene";
    [SerializeField] private KeyCode triggerKey = KeyCode.F;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private GameObject promptUI; // 可选的提示UI（例如“按F进入选关”）

    private int overlapCount;

    private void Reset()
    {
        // 默认触发器形状
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            overlapCount++;
            if (overlapCount < 0) overlapCount = 0; // 防护
            UpdatePrompt(true);
            if (promptUI != null) promptUI.SetActive(true);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            UpdatePrompt(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            overlapCount = Mathf.Max(0, overlapCount - 1);
            UpdatePrompt(overlapCount > 0);
            if (promptUI != null) promptUI.SetActive(false);
        }
    }

    private void Update()
    {
        if (overlapCount <= 0) return;

        if (Input.GetKeyDown(triggerKey))
        {
            SceneManager.LoadScene(targetSceneName);
        }
    }

    private void UpdatePrompt(bool show)
    {
        if (promptUI != null)
        {
            promptUI.SetActive(show);
        }
    }
}
