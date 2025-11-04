using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;

/// <summary>
/// Unity Test Framework 简单测试示例
/// Unity自带的测试框架，基于NUnit
/// 放在Editor文件夹中，Unity会自动处理引用
/// </summary>
public class SimpleAttributeTest
{
    /// <summary>
    /// 最简单的测试 - 验证测试框架是否工作
    /// </summary>
    [Test]
    public void OnePlusOne_EqualsTwo()
    {
        // Arrange（准备）
        int a = 1;
        int b = 1;
        
        // Act（执行）
        int result = a + b;
        
        // Assert（断言）
        Assert.AreEqual(2, result, "1 + 1 应该等于 2");
    }

    /// <summary>
    /// 测试Attribute组件的创建
    /// </summary>
    [Test]
    public void Attribute_CanBeCreated()
    {
        // Arrange & Act
        GameObject obj = new GameObject("TestAttribute");
        Attribute attribute = obj.AddComponent<Attribute>();
        
        // Assert
        Assert.IsNotNull(attribute, "Attribute组件应该被创建");
        Assert.IsTrue(attribute.IsAlive, "新创建的Attribute应该是存活的");
        
        // 清理
        Object.DestroyImmediate(obj);
    }

    /// <summary>
    /// 测试受到伤害功能
    /// </summary>
    [Test]
    public void Attribute_TakeDamage_HealthDecreases()
    {
        // Arrange
        GameObject obj = new GameObject("TestAttribute");
        Attribute attribute = obj.AddComponent<Attribute>();
        int initialHealth = attribute.CurrentHealth;
        int damage = 10;
        
        // Act
        attribute.TakeDamage(damage);
        
        // Assert
        Assert.Less(attribute.CurrentHealth, initialHealth, 
            "受到伤害后生命值应该减少");
        
        // 清理
        Object.DestroyImmediate(obj);
    }
}
