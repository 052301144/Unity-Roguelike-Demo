# 角色动画控制器设计指南

## 🚀 快速上手指南

**问题1**: 动画不立即切换？
➡️ **解决方案**: 在 Animator 中选择过渡箭头，取消勾选 **Has Exit Time** ✅

**问题2**: 短跳跃动画看起来不流畅？
➡️ **解决方案**: Jump-Start Speed = `2.5`，Exit Time = `0.6` ⚡

**问题3**: 同时按攻击和跳跃键角色闪现？
➡️ **解决方案**: 已修复！Attack 组件不再修改 transform.localScale，只通过 SpriteRenderer.flipX 控制视觉翻转 ✅

**核心原则**：
- **即时响应** (移动/攻击/跳跃) → ❌ 取消勾选 Has Exit Time
- **需要播放动画** (落地/起跳完成) → ✅ 勾选 Has Exit Time + 设置合适的 Speed 和 Exit Time

---

## 一、可用动画资源

根据 `Assets/Player Module/Player image/` 目录，您拥有以下动画序列：

1. **Idle** - 4帧 - 待机动画
2. **Run** - 8帧 - 跑步动画  
3. **Jump-Start** - 4帧 - 起跳动画
4. **Jump** - (可能与其他跳跃动画相关)
5. **Jump-End** - 落地动画
6. **Attack-01** - 8帧 - 攻击动画
7. **Dead** - 死亡动画
8. **Jump-All** - 完整跳跃动画序列

## 二、Animator 参数设计

根据 `PlayerController.cs` 中的 `UpdateAnimationParameters()` 方法，您需要创建以下参数：

### 必需的 Animator 参数

| 参数名 | 类型 | 说明 |
|--------|------|------|
| **Speed** | Float | 横向绝对速度，用于Idle/Run混合 |
| **IsJumping** | Bool | 是否正在跳跃（在空中）|
| **IsGrounded** | Bool | 是否在地面上 |
| **FacingRight** | Bool | 是否面向右侧 |
| **Facing** | Float | 朝向数值（1或-1）|
| **Attack** | Trigger | 触发攻击动画 |

## 三、动画状态机设计

### 状态层次结构

```
Base Layer (Base Layer)
├── Any State (特殊状态)
│   ├──→ Dead (死亡) [无条件，任何状态都可触发]
│   └──→ Attack-01 [通过 Attack Trigger 触发]
│
├── Entry (入口)
│   └──→ Idle (默认状态)
│
├── Idle (待机动画)
│   ├──→ Run [条件: Speed > 0.1]
│   └──→ Jump-Start [条件: IsJumping = true]
│
├── Run (跑步动画)
│   ├──→ Idle [条件: Speed < 0.1]
│   └──→ Jump-Start [条件: IsJumping = true]
│
├── Jump-Start (起跳动画)
│   └──→ Jump-All [条件: IsJumping = true, Exit Time = 0.6-0.9, Speed = 2.5]
│
├── Jump-All (跳跃中动画)
│   ├──→ Jump-End [条件: IsGrounded = true, Exit Time = 0.1]
│   └──→ Idle [条件: IsGrounded = true && Speed < 0.1]
│
├── Jump-End (落地动画)
│   ├──→ Run [条件: Exit Time = 0.8, Speed > 0.1]
│   └──→ Idle [条件: Exit Time = 0.8]
│
└── Attack-01 (攻击动画)
    ├──→ Idle [条件: Exit Time = 1.0, Speed < 0.1]
    └──→ Run [条件: Exit Time = 1.0, Speed > 0.1]
```

## 四、详细设置步骤

### 1. 创建状态

在 Animator 窗口中创建以下状态：

1. **Idle** - 将 Idle 动画剪辑拖入
2. **Run** - 将 Run 动画剪辑拖入  
3. **Jump-Start** - 将 Jump-Start 动画剪辑拖入
4. **Jump-All** - 将 Jump-All 动画剪辑拖入
5. **Jump-End** - 将 Jump-End 动画剪辑拖入
6. **Attack-01** - 将 Attack-01 动画剪辑拖入
7. **Dead** - 将 Dead 动画剪辑拖入

### 2. 设置过渡条件

#### ⚠️ 重要：Has Exit Time 设置

**关键点**：取消勾选 `Has Exit Time` 才能实现立即切换！

- ✅ **Has Exit Time 取消勾选** = 条件满足时立即切换
- ❌ **Has Exit Time 勾选** = 等待当前动画播放完成后才切换

**在 Unity Animator 窗口中的操作步骤**：
1. 点击要设置的 **过渡箭头** (连接两个状态的箭头)
2. 在 **Inspector 面板**右侧找到 **Settings** 区域
3. 找到 **Has Exit Time** 复选框
4. 根据需求勾选或取消：
   - **需要立即响应输入** (攻击、跳跃、移动) → ❌ 取消勾选
   - **需要播放完部分动画** (落地、起跳完成) → ✅ 勾选
5. 如果勾选了，可以设置 **Exit Time** 值（0-1之间）：
   - `0.5` = 播放到一半就切换
   - `0.9` = 播放到90%后切换
   - `1.0` = 完整播放一遍后切换

**Inspector 面板示例**：
```
┌─────────────────────────────┐
│ Transition Settings         │
├─────────────────────────────┤
│ ☐ Has Exit Time       ← 取消勾选即可！│
│ Exit Time: 0.9               │
│ Transition Duration: 0.2     │
│ Transition Offset: 0         │
│ Conditions:                  │
│   [ IsJumping == true ]      │
└─────────────────────────────┘
```

**常见陷阱**：初学者经常忘记取消勾选，导致手感"拖沓"！

### 📊 快速参考表：何时使用 Has Exit Time

| 过渡类型 | Has Exit Time | Exit Time | 原因 |
|---------|--------------|-----------|------|
| Idle ↔ Run | ❌ 取消 | - | 移动需要即时响应 |
| 地面 → 起跳 | ❌ 取消 | - | 跳跃输入需要即时响应 |
| 起跳 → 空中 | ✅ 勾选 | **0.6 (短跳)** | 需要播放部分起跳动作 |
| 起跳 → 空中 | ✅ 勾选 | **0.9 (长跳)** | 完整播放起跳动作 |
| 空中 → 落地 | ❌ 取消 | - | 检测到地面立即切换 |
| 落地 → Idle/Run | ✅ 勾选 | 0.8 | 需要播放落地缓冲 |
| Any State → 攻击 | ❌ 取消 | - | 攻击需要即时触发 |
| 攻击 → Idle/Run | ✅ 勾选 | 1.0 | 完整播放攻击动画 |
| Any State → 死亡 | ❌ 取消 | - | 死亡立即发生 |

**注意**: Exit Time 值需要根据您的跳跃高度实际测试调整！

#### Idle → Run
- **条件**: `Speed > 0.1`
- **Has Exit Time**: ❌ **取消勾选** ← 重要！
- **退出时间**: 0 (不需要)
- **平滑时间**: `0.2秒`

#### Run → Idle
- **条件**: `Speed < 0.1`
- **Has Exit Time**: ❌ **取消勾选** ← 重要！
- **退出时间**: 0 (不需要)
- **平滑时间**: `0.2秒`

#### Idle/Run → Jump-Start
- **条件**: `IsJumping = true`
- **Has Exit Time**: ❌ **取消勾选** (立即切换) ← 重要！
- **退出时间**: 0 (不需要)

#### Jump-Start → Jump-All
- **条件**: `IsJumping = true`
- **Has Exit Time**: ✅ **勾选** (需要播放部分起跳动画)
- **退出时间**: `0.6` (短跳跃) 或 `0.9` (长跳跃)
- **平滑时间**: `0.1秒`
- **额外设置**: Jump-Start 的 Speed = `2.5` (加速播放)

#### Jump-All → Jump-End
- **条件**: `IsGrounded = true`
- **Has Exit Time**: ❌ **取消勾选** (落地立即切换) ← 重要！
- **退出时间**: 0 (不需要)
- **平滑时间**: `0.15秒`

#### Jump-End → Idle
- **条件**: `无特定条件`
- **Has Exit Time**: ✅ **勾选** (需要播放部分落地动画)
- **退出时间**: `0.8` (播放80%后切换)
- **平滑时间**: `0.2秒`

#### Jump-End → Run
- **条件**: `Speed > 0.1`
- **Has Exit Time**: ✅ **勾选** (需要播放部分落地动画)
- **退出时间**: `0.8`
- **平滑时间**: `0.2秒`

### 3. Any State 连接

#### Any State → Dead
- **条件**: (可选) 添加死亡触发器
- **退出时间**: `取消`
- **注意**: 死亡是最终状态，没有出口

#### Any State → Attack-01
- **条件**: `Attack Trigger`
- **Has Exit Time**: ❌ **取消勾选** (立即触发攻击) ← 重要！
- **退出时间**: 0 (不需要)
- **平滑时间**: `0.1秒`
- **优先级**: 设置为高优先级，确保攻击动画可以中断大多数状态

#### Attack-01 → Idle
- **条件**: `Speed < 0.1`
- **Has Exit Time**: ✅ **勾选** (需要完整播放攻击动画)
- **退出时间**: `1.0`
- **平滑时间**: `0.2秒`

#### Attack-01 → Run
- **条件**: `Speed > 0.1`
- **Has Exit Time**: ✅ **勾选** (需要完整播放攻击动画)
- **退出时间**: `1.0`
- **平滑时间**: `0.2秒`

### 4. 动画剪辑设置建议

#### 设置建议循环播放

- **Idle**: 循环播放 ✅
- **Run**: 循环播放 ✅
- **Jump-Start**: 不循环 ❌ (一次性)
- **Jump-All**: 不循环 ❌ (等待落地)
- **Jump-End**: 不循环 ❌ (一次性)
- **Attack-01**: 不循环 ❌ (一次性)
- **Dead**: 不循环 ❌ (保持最后一帧)

### 5. 动画播放速度调整

根据游戏手感调整以下动画速度：

| 动画 | 建议速度 | 原因 |
|------|----------|------|
| Idle | 1.0 | 标准速度 |
| Run | 0.8 - 1.2 | 根据角色移动速度调整 |
| Jump-Start | **2.5** (短跳) | 快速起跳，适合短时间跳跃 |
| Jump-Start | 1.5 (长跳) | 较慢起跳，适合高跳跃 |
| Jump-All | 1.0 | 标准速度 |
| Jump-End | 1.0 | 标准速度 |
| Attack-01 | 1.0 - 1.5 | 根据攻击速度调整 |
| Dead | 1.0 | 标准速度 |

#### 🎯 短跳跃特殊处理

**问题**: 如果跳跃时间很短，来不及播放完整的 Jump-Start 动画怎么办？

**您的资源**: Jump-Start 有 4 帧，在不同帧率下的时长：
- 6fps → 约 0.67秒 (100ms/帧)
- 10fps → 约 0.4秒 (100ms/帧)
- 15fps → 约 0.27秒 (66ms/帧)

**解决方案A：加快起跳动画速度** ⭐ 推荐
- 设置 **Jump-Start Speed** = `2.0 - 3.0`
- 让起跳动画在0.2-0.3秒内完成
- **优点**: 简单，视觉流畅
- **缺点**: 动画看起来比较"快"

**解决方案B：移除 Jump-All，直接 Start→End**
- 删除 Jump-Start → Jump-All 的过渡
- Jump-Start 完成后直接跳到 Jump-End
- **Has Exit Time**: `0.6` (播放60%起跳动画)
- **优点**: 视觉上更连贯
- **缺点**: 失去在空中时的动画

**解决方案C：使用 Exit Time 提前切换**
- Jump-Start → Jump-All: Exit Time = `0.3` (只播放30%就切换)
- **优点**: 快速进入空中状态
- **缺点**: 起跳动画看起来不完整

**解决方案D：增加动画播放速度 + 调整 Exit Time** ⭐ 最推荐
- Jump-Start Speed = `2.5`
- Jump-Start → Jump-All: Exit Time = `0.6`
- **效果**: 快速起跳动画，然后平滑过渡到空中

#### 📝 实际操作步骤（推荐方案D）

**步骤1: 调整 Jump-Start 动画速度**
1. 在 Animator 中选择 **Jump-Start** 状态
2. 在 Inspector 右侧找到 **Speed** 参数
3. 将值设置为 `2.5` (原默认是 1.0)
4. 这样 4 帧动画会在约 0.16秒内完成

**步骤2: 调整过渡 Exit Time**
1. 点击 **Jump-Start → Jump-All** 的过渡箭头
2. 勾选 **Has Exit Time**
3. 设置 **Exit Time** = `0.6` (播放60%就切换)
4. 这样实际播放约 0.1秒的起跳动画

**步骤3: 测试和微调**
1. 运行游戏测试跳跃手感
2. 如果还是太慢，增加 Speed 到 `3.0`
3. 如果太快看起来不自然，降低到 `2.0`
4. 根据实际游戏中的跳跃高度调整 Exit Time

**调试技巧**:
```csharp
// 在 PlayerController 中添加调试输出
void OnDrawGizmos()
{
    if (isJumping && jumpStartY != 0)
    {
        float jumpHeight = transform.position.y - jumpStartY;
        Debug.Log($"当前跳跃高度: {jumpHeight:F2}m");
    }
}
```

## 五、角色朝向控制

### ⚠️ 重要说明：Animator Mirror 对 2D Sprite 无效

Unity Animator 的 **Mirror** 功能**只对骨骼动画（Skinned Mesh）有效**，对 2D Sprite 动画无效！

### ✅ 正确的朝向控制方法：SpriteRenderer.flipX

代码已自动处理朝向翻转，通过 `SpriteRenderer.flipX` 控制：

```csharp
// 在UpdateAnimationParameters()中自动调用
if (spriteRenderer != null)
{
    spriteRenderer.flipX = facing < 0; // 向左时翻转
}
```

### 🎯 在 Animator 中无需任何 Mirror 设置

- **FacingRight** 和 **Facing** 参数仍会同步到 Animator
- **但不要依赖 Animator 的 Mirror 功能**
- **朝向翻转由代码自动处理**

### 📝 您需要做的

1. 确保角色视觉节点有 **SpriteRenderer** 组件
2. 代码会自动找到并翻转 Sprite
3. **不需要在 Animator 中设置任何 Mirror**

### 🔍 如果朝向仍然不对

检查以下几点：
1. 角色子节点上是否有 SpriteRenderer？
2. 初始朝向是否正确？（动画资源默认朝右）
3. 在 Scene 视图中运行游戏，观察 facing 变量是否正确变化

## 六、优化建议

### 1. 状态优先级管理

确保重要状态有正确的优先级：
- **Dead**: 最高优先级（+1）
- **Attack-01**: 高优先级（+1）
- **Jump-Start**: 正常优先级（0）
- **其他**: 正常优先级（0）

### 2. 过渡偏移

调整过渡偏移以平滑连接：
- **Idle ↔ Run**: 偏移 0.3 - 0.5
- **跳跃相关**: 偏移 0.0 - 0.2
- **攻击**: 偏移 0.0

### 3. 添加 Blend Tree（可选）

如果需要更复杂的移动动画混合，可以创建 Blend Tree：

```
Base Layer
├── Movement Blend Tree
│   ├── Idle (Speed = 0)
│   └── Run (Speed > 0)
└── Jump Blend Tree
    ├── Jump-Start
    ├── Jump-All
    └── Jump-End
```

## 七、调试技巧

### 1. 在 Animator 窗口中预览

- 打开 Animator 窗口
- 点击 "Parameters" 面板中的参数
- 手动调整参数值观察过渡效果

### 2. 使用 Debug.DrawRay

代码中已有调试射线，在 Scene 视图中查看：
- 绿色射线：地面检测成功
- 红色射线：未检测到地面

### 3. 添加 Animator Debug 信息

可以在 PlayerController 中添加调试输出：

```csharp
void Update()
{
    if (Input.GetKeyDown(KeyCode.P)) // 按P键打印状态
    {
        Debug.Log($"Speed: {rb.velocity.x}, IsJumping: {isJumping}, IsGrounded: {isGrounded}");
    }
}
```

## 八、常见问题

### Q1: ⭐ 动画不立即切换，等待播放完成才切换（最重要！）
**问题**: 按键后动画切换有延迟，等待当前动画播放完成。
**A**: 
1. 检查过渡的 **Has Exit Time** 是否勾选了
2. 如果勾选了，**取消勾选**即可立即切换
3. 参考上述"快速参考表"决定哪些过渡需要立即响应
**这是最常犯的错误！**

### Q2: 动画切换卡顿
**A**: 
1. 检查过渡的 **Smoothing Time**，适当增加平滑时间值
2. 确认 **Has Exit Time** 设置正确
3. 检查动画剪辑的循环设置

### Q3: 攻击动画无法中断跳跃
**A**: 
1. 确保 Attack-01 是从 **Any State** 连接的
2. 设置高**优先级**，确保攻击动画可以中断大多数状态
3. **Has Exit Time** 必须取消勾选！

### Q4: 角色朝向错误
**A**: 
1. 代码已自动处理，使用 `SpriteRenderer.flipX`
2. 确保角色子节点有 **SpriteRenderer** 组件
3. 检查 `Visual Root` 是否正确设置

### Q5: 落地动画看起来不自然
**A**: 
1. 调整 Jump-End 的 **Exit Time** 和平滑时间
2. 或者增加动画播放速度
3. 检查跳跃相关的 **Has Exit Time** 设置

### Q6: Idle 和 Run 切换太快
**A**: 
1. 如果希望更平滑的过渡，增加 **Smoothing Time**
2. 确认 **Has Exit Time** 已正确设置（通常应该取消勾选以实现即时响应）

## 九、完整参数列表速查

```
Animator Parameters:
├── Speed (Float) - 水平速度绝对值
├── IsJumping (Bool) - 是否跳跃中
├── IsGrounded (Bool) - 是否在地面
├── FacingRight (Bool) - 是否向右
├── Facing (Float) - 朝向数值
└── Attack (Trigger) - 攻击触发器
```

## 十、Animator 放置位置

### 🎯 正确的 Animator 挂载位置

根据您当前的角色结构：

```
Player (Root GameObject)
├── PlayerController.cs ✓
├── Rigidbody2D ✓
├── BoxCollider2D ✓
└── Square (Child GameObject)
    ├── SpriteRenderer ✓ (用于翻转)
    └── Animator ✓ (应该放在这里！) ← 重要！
```

### 📝 设置步骤

1. **在 Unity 中选择 Square 子节点**
   - 在 Hierarchy 中找到 `Player` → `Square`

2. **添加 Animator 组件**
   - 点击 `Add Component`
   - 搜索并添加 `Animator`

3. **设置 Controller**
   - 将 `Player Controller.controller` 拖拽到 Animator 的 `Controller` 字段

4. **配置 Visual Root**
   - 在 `Player` 的 `PlayerController` 组件中
   - 将 `Visual Root` 字段设置为 `Square` 子节点
   - 这样代码会自动找到 SpriteRenderer 和 Animator

### ⚠️ 为什么要把 Animator 放在子节点？

**原因1**: Animator 需要控制 SpriteRenderer
- 2D 角色使用 Sprite 动画
- Animator 必须和 SpriteRenderer 在同一 GameObject 上才能正确工作

**原因2**: 代码自动查找逻辑
```csharp
// Awake() 中的查找顺序：
1. visualRoot.GetComponent<Animator>() ← 优先
2. 子节点循环查找
3. 自己的组件

// 代码会自动找到正确位置的 Animator
```

**原因3**: 物理和视觉分离
- `Player` (根) = 物理/逻辑/碰撞
- `Square` (子) = 视觉/动画/渲染
- 这是 2D 游戏的常见模式

### 🔍 验证设置

运行游戏后，检查 Console 输出：

```
[PlayerController] 初始化完成 - CCD: True, 地面射线: 5, 墙壁射线: 3, 移动速度: 6
[PlayerController] 跳跃设置 - 缓冲时间: 0.2, 土狼时间: 0.1, 冷却: 0.1, 最小高度: 0.5
[PlayerController] Animator状态 - 已找到 ✓ ← 应该显示这个
```

如果显示"未找到"，检查：
1. Animator 是否添加到了 Square 子节点？
2. Visual Root 是否设置为 Square？
3. Animator 的 Controller 是否已分配？

## 十一、攻击冷却和无敌帧系统

### ⚡ 攻击冷却（Attack Cooldown）

**问题**: Any State 连接攻击动画后，玩家可以连续快速按下攻击键导致动画混乱。

**解决方案**: 代码中已实现冷却检查，Animator 只需要简单设置即可。

#### 代码实现

在 `PlayerController.TriggerAttackAnim()` 中：
```csharp
// 检查攻击冷却（通过 Attack 组件）
if (attackComponent != null && !attackComponent.CanAttack)
{
    return; // 冷却中，不触发动画和伤害
}
```

攻击流程：
1. 玩家按下攻击键（J键）
2. `PlayerController.TriggerAttackAnim()` 被调用
3. 检查冷却：`!attackComponent.CanAttack`
4. 如果冷却中 → 直接返回，**不设置 Attack Trigger**
5. 如果未冷却 → 设置 Attack Trigger + 执行 `PerformAttack()`

#### 配置攻击冷却

在 `Attack` 组件中设置：
- **Attack Cooldown** = `0.5秒` (默认值)
- 可以在 Inspector 中调整

#### Animator 设置

**Any State → Attack-01 的过渡设置**：
1. **Has Exit Time**: ❌ 取消勾选（立即切换）
2. **Conditions**: 
   - Attack (Trigger)
3. **Transition Duration**: `0.1 - 0.2秒`（快速过渡）

**关键点**：
- 代码层面已经阻止了冷却期间的动画触发
- Attack Trigger 只在 `CanAttack = true` 时才会被设置
- 因此 Animator 不需要额外的冷却逻辑

### 🛡️ 无敌帧（Invincibility Frames）

**问题**: 敌人受到一次攻击后，在短时间内可能被多次伤害。

**解决方案**: 在 `Attribute` 和 `EnemyDamageable` 组件中添加了无敌帧系统。

#### 实现原理

1. **受伤时启动无敌帧**
   - 受到伤害后，记录 `invincibilityEndTime = Time.time + invincibilityDuration`
   - 默认无敌帧持续 `0.3秒`

2. **检查无敌状态**
   - `IsInvincible = Time.time < invincibilityEndTime`
   - 无敌状态下，所有伤害都会被免疫

#### 配置无敌帧

在 `Attribute` 或 `EnemyDamageable` 组件中：
- **Use Invincibility Frames** = ✅ 勾选（启用）
- **Invincibility Duration** = `0.3秒`（可调整）

#### 视觉反馈（可选）

可以在无敌帧期间添加视觉效果：

```csharp
// 在 Attribute 或 EnemyDamageable 中添加
private SpriteRenderer spriteRenderer;

void Update()
{
    if (IsInvincible && spriteRenderer != null)
    {
        // 闪烁效果：每帧切换透明度
        float alpha = Mathf.PingPong(Time.time * 10f, 1f);
        spriteRenderer.color = new Color(1f, 1f, 1f, alpha);
    }
}
```

### 📋 完整的攻击流程

```
玩家按下攻击键（J键）
  ↓
PlayerController.Update() 检测到 J 键按下
  ↓
调用 TriggerAttackAnim()
  ↓
检查: 是否在空中攻击？ (allowAttackInAir)
  ↓ 否 → 检查: 是否冷却中？ (!attackComponent.CanAttack) ← 冷却检查
  ↓ 是 → 直接返回，不执行攻击
  ↓ 通过检查
  ↓
同时执行：
  ├─ animator.SetTrigger("Attack")  → Animator: Any State → Attack-01 (立即切换)
  └─ attackComponent.PerformAttack() → 执行伤害检测
      ↓
      检测到敌人 → ProcessAttackHit()
      ↓
      敌人.ApplyDamage() / Attribute.TakeDamage()
      ↓
      检查: 敌人是否无敌？ (IsInvincible) ← 无敌帧检查
      ↓
      未无敌 → 应用伤害 + 启动无敌帧 (0.3秒)
      ↓
      记录攻击时间 lastAttackTime = Time.time
      
Attack-01 动画播放（视觉效果）
```

**关键点**：
- 伤害和动画**同时触发**，而不是通过动画事件
- 冷却检查在设置 Trigger 之前，确保冷却期间不会播放动画
- 每次攻击都会更新 `lastAttackTime`，开始新的冷却周期

### 🎮 调试技巧

**检查攻击冷却**:
```csharp
// 在 PlayerController 中添加
void Update()
{
    if (Input.GetKeyDown(KeyCode.J))
    {
        if (attackComponent != null)
        {
            Debug.Log($"CanAttack: {attackComponent.CanAttack}, Cooldown: {attackComponent.attackCooldown}");
        }
    }
}
```

**检查无敌帧**:
```csharp
// 在 Attribute 中添加
[ContextMenu("测试无敌帧")]
void TestInvincibility()
{
    Debug.Log($"IsInvincible: {IsInvincible}, EndTime: {invincibilityEndTime}");
}
```

### 💡 常见问题

**Q: 为什么快速连按攻击键动画不会触发？**
**A**: 代码中的冷却检查阻止了动画触发。这是正常行为，确保攻击有节奏感。
- 第一次按键：通过冷却检查 → 触发动画
- 0.5秒内再次按键：被冷却检查拦截 → 不触发动画
- 0.5秒后：冷却结束 → 可以再次触发

**Q: 我在 Animator 中看到 Attack-01 状态，应该怎么连接 Any State？**
**A**: 
1. 从 Any State 创建一个过渡到 Attack-01
2. 在过渡条件中添加：Attack (Trigger)
3. **取消勾选** Has Exit Time（关键！）
4. Transition Duration 设置为 0.1-0.2秒
5. 从 Attack-01 创建过渡回到所有其他状态（Idle、Run、Jump-All 等）
6. 回到其他状态的过渡：**勾选** Has Exit Time，Exit Time = 1.0

**Q: 敌人受到一次攻击后为什么还能继续被攻击？**
**A**: 检查敌人的 `Attribute` 或 `EnemyDamageable` 组件，确保：
- `Use Invincibility Frames` = ✅ 已勾选
- `Invincibility Duration` > 0

**Q: 如何调整无敌帧时间？**
**A**: 在 Inspector 中修改 `Invincibility Duration`：
- 短无敌帧：`0.1 - 0.2秒`（快速连击）
- 标准无敌帧：`0.3 - 0.5秒`（平衡）
- 长无敌帧：`0.5 - 1.0秒`（防止连续伤害）

**Q: 如何禁用某个敌人的无敌帧？**
**A**: 将该敌人的 `Use Invincibility Frames` = ❌ 取消勾选

**Q: 攻击冷却时间如何调整？**
**A**: 在 Inspector 中选择 Player GameObject，找到 `Attack` 组件，修改 `Attack Cooldown`：
- 快速攻击：`0.2 - 0.3秒`（适合高攻速角色）
- 标准攻击：`0.5 - 0.8秒`（平衡）
- 慢速重击：`1.0 - 1.5秒`（高伤害技能）

**Q: 为什么我的 Attack Component 找不到？**
**A**: 代码会自动在 Awake 中查找 Attack 组件。如果仍然为 null：
1. 确保 Player GameObject 有 Attack 组件
2. 或者手动在 Inspector 中拖拽 `attackComponent` 字段

## 十二、跳跃中攻击的处理

### 🎮 跳跃攻击动画方案

您的项目现在**只有地面攻击动画**，没有专门的跳跃攻击动画。有两种处理方式：

#### 方案A：允许跳跃攻击（默认）✓

**设置**：在 `PlayerController` 中 `Allow Attack In Air` = ✅ 勾选

**效果**：
- 跳跃中按攻击键会播放 Attack-01 动画
- **优点**：玩家可以在空中攻击
- **缺点**：动画看起来有点奇怪（地面攻击动作用于空中）

**适用游戏**：
- 快节奏动作游戏
- 平台跳跃战斗游戏
- 需要空中连击的系统

#### 方案B：禁止跳跃攻击

**设置**：在 `PlayerController` 中 `Allow Attack In Air` = ❌ 取消勾选

**效果**：
- 只有在落地时才能攻击
- 空中按攻击键无效
- **优点**：更真实的战斗体验
- **缺点**：战斗自由度降低

**适用游戏**：
- 写实战斗游戏
- 强调策略的 RPG
- 需要更多精确度的系统

### 💡 推荐方案选择

**建议先使用方案A（允许跳跃攻击）**，理由：
1. 现代平台跳跃游戏通常支持空中攻击
2. 即使动画看起来不太完美，游戏性更重要
3. 后续如果想添加专门的空攻动画，只需扩展

### 🔮 未来扩展：添加空中攻击动画

如果将来有了 `Attack-Air` 动画，可以这样改进：

**新的状态机设计**：
```
Base Layer
├── Any State
│   └──→ Attack-01 [条件: Attack Trigger && IsGrounded]
│   └──→ Attack-Air [条件: Attack Trigger && IsJumping] ← 新增
```

**代码修改**：
```csharp
public void TriggerAttackAnim()
{
    if (attackComponent != null)
        attackComponent.SetFacingDirection(facing);
    
    if (animator != null)
    {
        if (isGrounded)
            animator.SetTrigger("Attack");  // 地面攻击
        else
            animator.SetTrigger("AttackAir"); // 空中攻击
    }
}
```

### 📋 Animator 设置建议

**当前（只有地面攻击）**：
- Any State → Attack-01
- Attack-01 完成后返回当前状态（Idle/Run/Jump）

**如果添加空中攻击后**：
- Any State → Attack-01 [条件: IsGrounded]
- Any State → Attack-Air [条件: !IsGrounded]
- 两个状态完成后都返回对应状态

## 十三、测试清单

### 基础设置
- [ ] Animator 组件已添加到 Square 子节点
- [ ] Visual Root 设置为 Square
- [ ] Animator Controller 已分配
- [ ] Console 显示 "Animator状态 - 已找到"

### 动画状态
- [ ] 所有状态都能正常创建和连接
- [ ] Idle ↔ Run 切换流畅（Has Exit Time 已取消）
- [ ] 跳跃序列（Start → All → End）完整播放
- [ ] 所有过渡的平滑时间合适

### 攻击系统
- [ ] 地面攻击动画正常工作
- [ ] **攻击冷却正常工作**（快速连按不会连续触发）
- [ ] **敌人无敌帧正常工作**（受击后短时间内不会再次受伤）
- [ ] 跳跃中攻击按设置正常工作（允许/禁止）
- [ ] Attack 组件的 Cooldown 时间合适

### 视觉和物理
- [ ] 角色朝向随移动方向正确翻转（使用 SpriteRenderer.flipX）
- [ ] 落地后根据速度正确返回 Idle 或 Run
- [ ] 死亡动画播放后不再切换

### 配置验证
- [ ] 决定使用哪种跳跃攻击方案（A 或 B）
- [ ] Attack 组件的 Attack Cooldown 已设置
- [ ] 敌人的 Invincibility Duration 已设置（如果需要）

---

**注意**: 您需要先从 Aseprite Importer 生成的动画剪辑中提取并创建 Animator Controller。确保所有动画剪辑都已正确导入后，按照本指南设置状态机。

