using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChestOpener : MonoBehaviour // 类名可以改为更具体的名称，如ChestOpener
{
    private bool canInteract; // 标记玩家当前是否可交互
    private bool isOpen;      // 标记宝箱是否已打开
    private Animator anim;

    void Start()
    {
        // 获取动画组件，使用大小写敏感，安全判断
        anim = GetComponent<Animator>();
        if (anim == null)
        {
            Debug.LogError("宝箱组件未找到Animator组件！", this);
        }
        isOpen = false;
        canInteract = false; // 正式初始化交互逻辑变量
    }

    void Update()
    {
        // 只在可交互且未打开且有Animator组件时才响应F键
        if (Input.GetKeyDown(KeyCode.F) && canInteract && !isOpen && anim != null)
        {
            anim.SetTrigger("opening");
            isOpen = true;
            Debug.Log("宝箱开始打开动画"); // 添加日志
        }
    }

    // 玩家进入交互范围时，通过标签判断（确保玩家有碰撞体）
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            canInteract = true;
            Debug.Log("玩家进入宝箱交互范围"); // 添加日志
        }
    }

    // 玩家离开交互范围
    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            canInteract = false;
            Debug.Log("玩家离开宝箱交互范围"); // 添加日志
        }
    }
}
