using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChestOpener : MonoBehaviour // 类名建议改为更具语义的名称（如ChestOpener）
{
    private bool canInteract; // 更清晰的命名：是否可交互
    private bool isOpen;      // 更简洁的命名：是否已打开
    private Animator anim;

    void Start()
    {
        // 修正组件获取方法的大小写，并添加安全判断
        anim = GetComponent<Animator>();
        if (anim == null)
        {
            Debug.LogError("宝箱对象上未找到Animator组件！", this);
        }
        isOpen = false;
        canInteract = false; // 显式初始化，逻辑更清晰
    }

    void Update()
    {
        // 只有在可交互、未打开、且获取到Animator组件时，才响应F键
        if (Input.GetKeyDown(KeyCode.F) && canInteract && !isOpen && anim != null)
        {
            anim.SetTrigger("opening");
            isOpen = true;
            Debug.Log("宝箱开始打开动画"); // 调试日志
        }
    }

    // 玩家进入交互范围（仅通过标签判断，兼容任意玩家碰撞器）
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            canInteract = true;
            Debug.Log("玩家进入宝箱交互范围"); // 调试日志
        }
    }

    // 玩家离开交互范围
    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            canInteract = false;
            Debug.Log("玩家离开宝箱交互范围"); // 调试日志
        }
    }
}