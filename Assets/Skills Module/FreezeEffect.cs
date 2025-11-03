using UnityEngine; // Unity 引擎命名空间

/// <summary>
/// 冰冻效果组件：在目标被冰元素冻结时，组件的 IsFrozen 会为 true，你的敌人脚本需查询 IsFrozen 来判断
/// 注意：该组件不会自动修改你的敌人模块，需在敌人类里判断 IsFrozen 进行扩展
/// </summary>
public class SM_FreezeEffect : MonoBehaviour, SM_IFreezable
{
    private float _remain;                // 剩余冰冻时长
    public bool IsFrozen => _remain > 0f; // 外部可读是否冰冻

    public void Freeze(float duration)    // 实现接口，施加冰冻
    {
        _remain = Mathf.Max(_remain, duration); // 刷新冰冻时长（取更长）
    }

    private void Update()
    {
        if (_remain > 0f) _remain -= Time.deltaTime; // 冰冻时长递减
    }
}
