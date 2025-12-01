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

    private bool playerInside;

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
            playerInside = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInside = false;
        }
    }

    private void Update()
    {
        if (!playerInside) return;

        if (Input.GetKeyDown(triggerKey))
        {
            SceneManager.LoadScene(targetSceneName);
        }
    }
}
