# 角色静止动画生成工具

本工具用于基于原始Player.png生成4帧角色静止动画。

## 使用方法

### 1. 安装依赖

首先需要安装Python和Pillow库：

```bash
# 安装Pillow库
pip install Pillow
```

### 2. 运行脚本

在项目根目录运行：

```bash
python generate_idle_animation.py
```

脚本会自动查找 `Assets/Player Module/Player image/Player.png`，如果找不到会提示您输入路径。

### 3. 输出结果

脚本会在 `Assets/Player Module/Player image/` 目录下生成以下文件：

- `Player_idle_1.png` - 第1帧（原始图像）
- `Player_idle_2.png` - 第2帧（轻微上移和缩小）
- `Player_idle_3.png` - 第3帧（轻微下移和放大）
- `Player_idle_4.png` - 第4帧（回到原始位置）
- `Player_idle_sheet.png` - 精灵表（所有4帧横向排列）

## 动画效果说明

生成的4帧动画模拟了角色的呼吸效果：

- **第1帧**：原始姿态（静止）
- **第2帧**：吸气 - 轻微上移并缩小
- **第3帧**：呼气 - 轻微下移并放大
- **第4帧**：回到原始位置（过渡回第1帧）

## Unity中使用

### 方法1：使用单独的帧文件

1. 将所有4个帧文件导入Unity
2. 选择所有帧，在Inspector中设置：
   - Texture Type: Sprite (2D and UI)
   - Sprite Mode: Single
   - Pixels Per Unit: 根据您的需求设置（通常100）
3. 在Animator Controller中创建Idle动画
4. 将4帧拖入Animation窗口，设置合适的帧率（如0.2秒/帧）

### 方法2：使用精灵表

1. 导入 `Player_idle_sheet.png`
2. 在Inspector中设置：
   - Texture Type: Sprite (2D and UI)
   - Sprite Mode: Multiple
   - Pixels Per Unit: 根据您的需求设置
3. 点击 "Sprite Editor"
4. 在Sprite Editor中点击 "Slice" -> "Grid By Cell Count"
   - Column & Row: 4 x 1
5. 应用设置
6. 创建Animation Clip，选择4个切片帧

## 注意事项

- 脚本使用NEAREST插值模式保持像素艺术风格
- 调整幅度较小（1像素移动，0.5%缩放），适合静止动画
- 如需调整动画幅度，可以修改脚本中的 `scale` 和 `offset_y` 值

