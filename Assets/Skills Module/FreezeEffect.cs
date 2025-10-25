using UnityEngine; // Unity 命名空间

/// <summary>
/// 冻结效果（仅负责计时；真正的“停行动作”留给你们敌人脚本自行读取 IsFrozen 来处理）
/// 这样可以做到不修改你们敌人模块的前提下，保留扩展点。
/// </summary>
public class SM_FreezeEffect : MonoBehaviour, SM_IFreezable
{
    private float _remain;                // 剩余冻结时间
    public bool IsFrozen => _remain > 0f; // 外部可读：是否冻结中

    public void Freeze(float duration)    // 实现接口：施加冻结
    {
        _remain = Mathf.Max(_remain, duration); // 刷新持续时间（取更长）
    }

    private void Update()
    {
        if (_remain > 0f) _remain -= Time.deltaTime; // 冻结时间递减
    }
}