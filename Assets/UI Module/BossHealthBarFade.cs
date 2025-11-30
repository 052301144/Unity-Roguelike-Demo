using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// BOSS血条浮现控制器：检测到玩家时缓缓显示血条
/// </summary>
public class BossHealthBarFade : MonoBehaviour
{
    [Header("BOSS引用")]
    [SerializeField] private EnemyBossAI bossAI; // 拖拽BOSS对象（挂载EnemyBossAI的物体）
    [Header("浮现设置")]
    [SerializeField] private float fadeSpeed = 2f; // 浮现/隐藏速度
    [SerializeField] private float showDelay = 0.3f; // 检测到玩家后延迟浮现的时间
    [SerializeField] private float hideDelay = 1f; // 失去玩家后延迟隐藏的时间

    private CanvasGroup _canvasGroup;
    private float _targetAlpha = 0f; // 目标透明度（0=隐藏，1=显示）
    private float _delayTimer = 0f; // 延迟计时器

    private void Awake()
    {
        // 获取血条的CanvasGroup组件（控制透明度）
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
        {
            Debug.LogError("BOSS血条对象缺少CanvasGroup组件！");
            enabled = false;
            return;
        }

        // 初始隐藏血条
        _canvasGroup.alpha = 0f;
    }

    private void Update()
    {
        // 若未关联BOSS，尝试自动查找（容错）
        if (bossAI == null)
        {
            bossAI = FindObjectOfType<EnemyBossAI>();
            if (bossAI == null) return;
        }

        // 检测BOSS是否在追击玩家（isChasing为true表示已检测到玩家）
        if (bossAI.IsChasing)
        {
            // 延迟后设置目标透明度为1（显示）
            _delayTimer += Time.deltaTime;
            if (_delayTimer >= showDelay)
            {
                _targetAlpha = 1f;
            }
        }
        else
        {
            // 失去玩家后，延迟设置目标透明度为0（隐藏）
            _delayTimer += Time.deltaTime;
            if (_delayTimer >= hideDelay)
            {
                _targetAlpha = 0f;
            }
        }

        // 平滑过渡透明度（实现“缓缓浮现/隐藏”效果）
        _canvasGroup.alpha = Mathf.Lerp(_canvasGroup.alpha, _targetAlpha, fadeSpeed * Time.deltaTime);

        // 重置计时器（当状态切换时，重新计算延迟）
        if ((bossAI.IsChasing && _targetAlpha == 0f) || (!bossAI.IsChasing && _targetAlpha == 1f))
        {
            _delayTimer = 0f;
        }
    }

    // 可选：强制显示血条（用于调试）
    [ContextMenu("强制显示血条")]
    private void ForceShowHealthBar()
    {
        _targetAlpha = 1f;
        _canvasGroup.alpha = 1f;
    }
}