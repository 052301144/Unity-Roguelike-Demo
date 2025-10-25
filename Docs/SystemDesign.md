# Unity Roguelike 系统设计说明书

版本：v0.1  日期：2025-10-20  作者：

## 1. 目标与非目标
- 目标：构建一款 2D Roguelike 地牢探索游戏，包含玩家移动与跳跃、近战/元素攻击、技能系统、敌人 AI、地图/关卡、UI（血量/背包/暂停/场景切换）与基础存档。
- 非目标：暂不支持联机对战/合作；暂不实现复杂关卡编辑器与多人同步；暂未实现全面的剧情系统。

## 2. 系统概览与上下文
- 引擎与平台：Unity 2022+（参见 `ProjectSettings/ProjectVersion.txt`），目标平台 Windows。
- 外部依赖：操作系统输入、文件系统（本地存储存档），无服务器依赖。
- 目录概览：
  - 玩家：`Assets/Player Module/PlayerController.cs`
  - 敌人：`Assets/Enemy Module/EnemyAI.cs`
  - 攻击/属性：`Assets/AttackModule/Attack.cs`, `Attribute.cs`
  - 技能系统：`Assets/Skills Module/`（`SM_SkillSystem.cs`、各系技能、投射体等）
  - UI：`Assets/UI Module/`（血条/背包/暂停/场景加载等）
  - 场景：`Assets/Scenes/StartMenuScene.unity` → `Main Scenes.unity` → `SaveScenes.unity`
  - 地图：`Assets/Map Module/`（`Grid 1.prefab`、Tile 资源与调色板）

## 3. 架构视图（模块与依赖）
- 模块与职责：
  - Player Module：处理输入、移动/跳跃（含缓冲与土狼时间）、与技能/攻击系统衔接、受伤与死亡。
  - Enemy Module：巡逻与追击、攻击决策、防止穿墙、简单状态管理。
  - Attack Module：近战/元素攻击命中体与结算、元素效果（燃烧/冰冻/风击退/雷链）。
  - Skills Module：技能数据与释放管线、投射体、技能事件、MP 消耗与冷却（通过 `SM_SkillSystem`）。
  - UI Module：血条、背包、暂停菜单与场景切换入口。
  - Map/Scenes：Tilemap/Prefab 布局与场景生命周期。
- 依赖方向：
  - Player/Enemy 依赖 Attribute 与 Attack/Skills 以完成战斗；
  - UI 读取玩家状态（生命/MP/背包等）进行展示，不反向驱动业务；
  - 场景驱动对象创建与销毁，技能/攻击遵循物理与帧循环。

## 4. 关键用例与时序（概要）
- 开局：`StartMenuScene.unity` 进入 → 选择开始 → 切入 `Main Scenes.unity`；必要的持久对象用 `DontDestroyOnLoad`（如 Skill UI/管理器，若使用）。
- 移动：键盘 A/D → `PlayerController` 计算方向 → 牵引 Rigidbody2D → 碰撞多射线防穿模（墙/地面）。
- 跳跃：K 键 → 跳跃缓冲/土狼时间判断 → 赋值垂直速度 → 更新状态与落地检测。
- 近战/元素攻击：J 键（或由技能触发）→ `Attack` 生成攻击盒 → `OverlapBoxAll` 命中 → 计算伤害与元素效果 → 目标 `Attribute` 扣血/受控。
- 技能释放：按键（由 `SM_SkillSystem` 监听）→ 检查冷却/消耗 → 生成投射体/范围判定 → 命中 → 结算与 UI 更新。
- 敌人追击攻击：检测范围触发追击 → 近身进入攻击距离 → 延迟 → 命中玩家 `Attribute`。
- 暂停与场景切换：UI 菜单触发 → `Time.timeScale` 调整/加载场景 API。
- 存档：在 `SaveScenes.unity` 或指定点保存玩家属性/背包/进度（实现位于后续计划）。

## 5. 模块设计详解

### 5.1 Player Module（`Assets/Player Module/PlayerController.cs`）
- 组件与字段：`Rigidbody2D rb`、`Collider2D bodyCollider`、移动 `moveSpeed`、跳跃 `jumpForce/allowDoubleJump/highJumpMultiplier`、落地检测参数（`groundCheckRays/groundLayer/groundCheckDistance`）、墙体检测（多射线、`wallCheckRays`、`wallCheckOffset`）、
  碰撞预测与速度上限（`collisionPredictionTime/maxSafeSpeed`）。
- 跳跃鲁棒性：
  - 跳跃缓冲 `jumpBufferTime` 与土狼时间 `coyoteTime`；
  - 双跳 `allowDoubleJump` 与最小跳跃高度 `minJumpHeight`；
  - 冷却 `jumpCooldown` 防误触。
- 移动与墙体：
  - A/D 离散输入，计算 `targetVelX` 并直接设置 `rb.velocity.x`；
  - 多射线墙检，允许角落通过，阈值判定（>60% 命中视为墙）；
  - 可选“预测性碰撞检测”。
- 生命与受击：内置基础生命/防御值与回复；实现 `SM_IDamageable.ApplyDamage(SM_DamageInfo)`，支持暴击与防御减免，死亡禁用控制。
- 技能集成：
  - `SM_SkillSystem skillSystem`，`AimOrigin` 与朝向向量 `AimDirection` 暴露；
  - MP 读写通过 `SM_SkillSystem`（`CurrentMP/MaxMP/ConsumeMP`）。
- 帧循环：输入在 `Update`（跳跃按下记录、技能瞄准更新、生命回复）；物理在 `FixedUpdate`（地面检测、移动、跳跃、状态/计时器更新）。

### 5.2 Enemy Module（`Assets/Enemy Module/EnemyAI.cs`）
- 状态：巡逻/追击/攻击；`isChasing`、`isAttacking` 控制；仅在一定阈值下翻转朝向避免抖动。
- 追击：基于玩家相对 X 方向追随；射线墙检阻挡则停下防穿墙；追击速度与巡逻速度区分。
- 攻击：进入攻击半径后延迟攻击；若命中则对玩家 `Attribute` 造成伤害。
- 可视化：Gizmos 展示检测/攻击半径与墙检线段。

### 5.3 Attack Module（`Assets/AttackModule/Attack.cs`, `Attribute.cs`）
- 攻击体：
  - 攻击类型枚举：Physical/Fire/Wind/Ice/Thunder；
  - `OverlapBoxAll` 获取命中目标，过滤自体；
  - 近战暴击率与暴伤；元素攻击按目标防御折减（最低 10%）。
- 元素效果：
  - Fire：添加 `BurnEffect`，按间隔灼烧；
  - Wind：对 `Rigidbody2D` 施加脉冲击退；
  - Ice：`FreezeEffect` 暂停刚体运动；
  - Thunder：雷链扩散周围敌人。
- 属性系统（`Attribute`）：
  - 基础属性：MaxHealth/CurrentHealth/Attack/Defense；
  - 事件：`OnHealthChanged/OnTakeDamage/OnDeath` 等；
  - 伤害：`TakeDamage`（随防御降低伤害）与 `TakeTrueDamage`（无视防御）；
  - 可视化：控制台血条与百分比输出（可替换为 UI 绑定）。

### 5.4 Skills Module（`Assets/Skills Module/`）
- 关键脚本：
  - `SM_SkillSystem.cs`（技能枢纽：按键监听、冷却/消耗、技能实例化与事件派发）。
  - 通用接口：`CommonInterfaces.cs` 与 `SkillEventBus.cs`（事件总线）。
  - 元素技能：`Fire_Fireball.cs`、`Fire_FlameCone.cs`、`Ice_IceSpike.cs`、`Ice_IceNova.cs`、`Lightning_ChainLightning.cs`、`Lightning_StaticField.cs`、物理系 `Physical_DashStab.cs`、`Physical_WhirlwindSlash.cs`。
  - 投射体与特效：`Projectile.cs`、`Tornado.cs`、`Prefabs/FireballPrefab.cs`。
- 与玩家对接：`PlayerController` 提供 `AimOrigin/AimDirection` 与 MP；技能系统内部处理按键（玩家脚本中留占位键位）。
- 释放管线（典型）：校验冷却/MP → 构建技能体（投射体/范围）→ 命中 → 结算（含元素）→ 事件/UI 刷新。

### 5.5 UI Module（`Assets/UI Module/`）
- `blood.cs`：显示与更新生命值（建议绑定玩家 `Attribute.OnHealthChanged`）。
- `InventorySystem.cs`：背包与物品管理（后续可与掉落/装备联动）。
- `SimplePauseMenu.cs`：暂停（`Time.timeScale` 切换）、菜单导航。
- `SimpleSceneLoader.cs`：切换场景（开始/返回主菜单/读取保存场景）。

### 5.6 地图与场景（`Assets/Map Module/`, `Assets/Scenes/`）
- 地图：`Grid 1.prefab` 基于 Tilemap；`palette/` 存放调色板与 Tile 资源；`cave_tileset.png` 作为素材。
- 场景流：`StartMenuScene.unity`（入口）→ `Main Scenes.unity`（游戏主循环）→ `SaveScenes.unity`（存档/读档展示或交互）。
- 对象生命周期：需要跨场景持久的对象（如技能 UI）应集中管理与 `DontDestroyOnLoad`。

## 6. 数据模型与配置
- 属性数据：`Attribute` 序列化字段（MaxHealth/Attack/Defense 等）。
- 技能数据：每个技能脚本自带可调参数（冷却、消耗、弹速/范围/持续）；可升级为 ScriptableObject 资产以实现策划配置。
- 资源组织：
  - 贴图/图集：`Assets/Image/`、`Assets/imagine/` 与 TextMeshPro 资源；
  - Prefab：技能投射体等存放在 `Assets/Skills Module/Prefabs/`。
- 命名与路径：统一英文小写+驼峰/下划线，文件夹按系统模块归类（已遵循）。

## 7. 运行时流程与帧级调用
- 主循环：
  - Update：输入采集（跳跃按键录入）、技能瞄准更新、生命回复。
  - FixedUpdate：地面/墙壁检测、移动速度设置、跳跃执行与状态计时器更新。
  - LateUpdate：保留用于相机/跟随（后续可加）。
- 物理：启用 `Rigidbody2D.collisionDetectionMode = Continuous` 防高速穿模；多射线防边缘穿模；墙体角落放行策略。

## 8. 性能与内存策略
- 渲染：建议合并 SpriteAtlas，减少材质切换；清理运行时 Gizmos/Debug 绘制开关。
- 物理：
  - 射线数量可调（`groundCheckRays/wallCheckRays`），根据平台性能折中；
  - 合理的 Layer 碰撞矩阵（`ProjectSettings/Physics2DSettings.asset`）。
- 对象池：对投射体、雷链临时体、粒子/特效建议走对象池，降低 GC 与 Instantiate/Destroy 抖动。
- 资源加载：后续可引入 Addressables；场景切换前后主动卸载暂不使用资源。

## 9. 工程质量与测试
- 日志策略：
  - 战斗与调试日志在开发期开启（`showDebugInfo` 等），发布版剔除或降级；
  - 关键异常使用 `Debug.LogError`。
- 代码规范：
  - 类与文件一一对应；公开字段用于 Inspector 配置，内部状态使用私有字段；
  - 事件驱动 UI 更新，避免轮询。
- 测试建议：
  - 单元：`Attribute.CalculateFinalDamage`、暴击/元素分支；
  - 集成：近战命中盒与多层碰撞、技能释放/冷却/消耗；
  - 场景：从开始菜单到主场景的切换回归。

## 10. 风险、取舍与替代方案
- 碰撞命中：盒碰撞便于实现与调参，但精准度有限；可替换为多个局部命中体或基于曲线采样的射线扇形。
- 单例/静态：临时便捷但易耦合；建议通过“服务定位器/依赖注入”集中管理跨场景对象。
- 技能配置：脚本直写灵活度高但不利于策划；建议逐步迁移至 ScriptableObject。

## 11. 版本与发布
- 构建配置：按平台设置分辨率、目标帧率与质量档位（参见 `ProjectSettings/QualitySettings.asset`）。
- 资源：确保 TextMeshPro 资源打包、地图素材与技能 Prefab 处于引用链中；
- 日志：发布版关闭调试绘制与大量 Debug 输出。

## 12. 安全与存档（规划）
- 存档内容：玩家 `Attribute`（生命、攻防、MP）、背包、已解锁技能、关卡进度/种子。
- 格式：建议 JSON（可读可迁移），提供版本号字段以支持升级；必要时做简单校验（hash/sign）。
- 入口：`SaveScenes.unity` 或主场景存档点；加载时校验缺失字段并给默认值。

## 13. 未来工作与路线图
- 技能系统：资产化配置、对象池、更多元素联动（冻结+雷加成等）。
- AI：行为树/状态机抽象、寻路（Grid/Tilemap A*）。
- 经济与装备：物品稀有度、词缀系统与掉落表；与 Attribute 增益整合。
- UI：血条/背包事件绑定、技能冷却与 MP 条可视化。
- 存档：完整的 Save/Load 管线、版本迁移工具。

## 14. 附录（类与接口概览）
- Player：`PlayerController`（输入/移动/跳跃/受击、与 `SM_SkillSystem` 对接）。
- Enemy：`EnemyAI`（巡逻/追击/攻击、墙检与翻转）。
- Combat：`Attack`（命中体与元素）、`Attribute`（属性与事件）、`BurnEffect`/`FreezeEffect`。
- Skills：`SM_SkillSystem`、`Projectile`、各系技能脚本与 Prefab。
- UI：`blood`、`InventorySystem`、`SimplePauseMenu`、`SimpleSceneLoader`。

---

变更记录
- v0.1（2025-10-20）：创建初稿，基于现有代码提炼模块与流程，补齐非功能章节建议。


