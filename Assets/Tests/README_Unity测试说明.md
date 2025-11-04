# Unity 自带的测试框架使用说明

## 📚 Unity Test Framework（UTF）

Unity 自带了测试框架，叫做 **Unity Test Framework**（UTF），它基于 **NUnit**。

### ✅ 优点

- **无需额外安装**：Unity 2021.2+ 已经内置
- **集成在编辑器中**：可以直接在 Unity 中运行
- **支持 EditMode 和 PlayMode** 两种测试模式
- **基于 NUnit**：使用标准的单元测试语法

## 🚀 快速开始

### 1. 查看测试框架是否已安装

1. 打开 Unity 编辑器
2. 菜单：`Window > Package Manager`
3. 搜索 `Test Framework`
4. 确认已安装（通常版本在 1.1.x 以上）

### 2. 创建测试文件

在 `Assets` 目录下创建 `Tests` 文件夹（Unity 会自动识别）

### 3. 编写测试

```csharp
using UnityEngine;
using NUnit.Framework;  // Unity Test Framework 基于 NUnit

public class MyTest
{
    [Test]
    public void MyFirstTest()
    {
        // 你的测试代码
        Assert.AreEqual(2, 1 + 1);
    }
}
```

### 4. 运行测试

1. 打开 `Window > General > Test Runner`
2. 选择 `EditMode` 标签页（编辑模式测试，快速）
3. 点击 `Run All` 运行所有测试

## 📝 测试类型

### EditMode 测试（编辑模式）

- **特点**：不需要运行游戏，速度快
- **适用**：纯逻辑测试、数据结构测试、算法测试
- **示例**：伤害计算、属性管理、数据验证

```csharp
[Test]
public void CalculateDamage_Test()
{
    // 测试伤害计算逻辑
    int damage = 100;
    int defense = 50;
    int expected = 50; // 假设50%减伤
    
    Assert.AreEqual(expected, Calculate(damage, defense));
}
```

### PlayMode 测试（播放模式）

- **特点**：需要启动 Unity 运行时环境
- **适用**：物理系统、动画系统、组件交互测试
- **示例**：角色移动、碰撞检测、UI交互

```csharp
[UnityTest]
public IEnumerator Player_Moves_WithInput()
{
    // 创建玩家对象
    GameObject player = new GameObject();
    // ... 设置组件
    
    yield return null; // 等待一帧
    
    // 测试移动逻辑
    Assert.IsNotNull(player);
}
```

## 🔧 常用测试特性

### [Test] 属性

标记一个普通测试方法（EditMode）

```csharp
[Test]
public void MyTest()
{
    Assert.AreEqual(1, 1);
}
```

### [UnityTest] 属性

标记一个协程测试方法（PlayMode）

```csharp
[UnityTest]
public IEnumerator MyUnityTest()
{
    yield return null;
    Assert.IsTrue(true);
}
```

### [SetUp] 和 [TearDown]

在每个测试前/后执行的代码

```csharp
private GameObject testObject;

[SetUp]
public void SetUp()
{
    testObject = new GameObject("Test");
}

[TearDown]
public void TearDown()
{
    Object.DestroyImmediate(testObject);
}
```

### Assert 断言

常用的断言方法：

- `Assert.AreEqual(expected, actual)` - 相等
- `Assert.IsTrue(condition)` - 为真
- `Assert.IsNull(obj)` - 为空
- `Assert.IsNotNull(obj)` - 不为空
- `Assert.Greater(a, b)` - a 大于 b
- `Assert.Less(a, b)` - a 小于 b

## 📁 文件夹结构建议

```
Assets/
└── Tests/                    # Unity 会自动识别
    ├── SimpleAttributeTest.cs
    └── README_Unity测试说明.md
```

**注意**：
- `Tests` 文件夹名称会被 Unity Test Framework 自动识别
- 测试文件可以放在 `Assets` 的任何子文件夹中
- 如果有 `Editor` 文件夹，EditMode 测试应该放在其中

## 🎯 示例测试文件

查看 `SimpleAttributeTest.cs` 了解基本的测试写法。

## 📖 更多资源

- [Unity Test Framework 官方文档](https://docs.unity3d.com/Packages/com.unity.test-framework@latest)
- Unity 编辑器中的 Test Runner 窗口有内置的帮助文档

## ❓ 常见问题

### Q: 测试文件找不到 NUnit？

**A**: 确保 Unity Test Framework 包已安装（Package Manager 中搜索 "Test Framework"）

### Q: Test Runner 窗口看不到测试？

**A**: 
1. 确保脚本已编译完成（没有编译错误）
2. 点击 Test Runner 窗口中的 `Refresh` 按钮
3. 确保切换到正确的标签页（EditMode 或 PlayMode）

### Q: 测试需要访问项目中的其他类？

**A**: 如果项目使用了 Assembly Definition，测试程序集需要引用主程序集。但对于大多数简单项目，不需要额外配置。

---

**提示**：Unity Test Framework 是 Unity 官方的测试解决方案，推荐使用它进行自动化测试！
