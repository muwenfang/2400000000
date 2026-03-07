using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 确认对话框 - 可选
/// 用于显示删除卡牌等需要确认的操作
/// 
/// 使用方法：
/// 1. 创建一个 UI Canvas
/// 2. 添加此脚本到 Canvas 上
/// 3. 设置或自动创建按钮和文本组件
/// 4. 通过 ShowConfirmation() 方法显示对话框
/// </summary>
public class ConfirmationDialog : MonoBehaviour
{
    [Header("UI 组件")]
    public Text messageText; // 消息文本
    public Button confirmButton; // 确认按钮
    public Button cancelButton; // 取消按钮

    private Action onConfirm;
    private Action onCancel;

    void Awake()
    {
        // 自动初始化UI组件
        if (messageText == null)
        {
            messageText = GetComponentInChildren<Text>();
        }

        Button[] buttons = GetComponentsInChildren<Button>();
        if (buttons.Length >= 2)
        {
            confirmButton = buttons[0];
            cancelButton = buttons[1];
        }

        // 如果没有找到按钮，创建一些简单的
        if (confirmButton == null || cancelButton == null)
        {
            CreateSimpleButtons();
        }
    }

    /// <summary>
    /// 显示确认对话框
    /// </summary>
    public void ShowConfirmation(string message, Action onConfirmCallback, Action onCancelCallback)
    {
        // 设置消息
        if (messageText != null)
        {
            messageText.text = message;
        }

        onConfirm = onConfirmCallback;
        onCancel = onCancelCallback;

        // 设置按钮回调
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnConfirmClick);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(OnCancelClick);
        }

        // 显示对话框
        gameObject.SetActive(true);
    }

    void OnConfirmClick()
    {
        onConfirm?.Invoke();
        Close();
    }

    void OnCancelClick()
    {
        onCancel?.Invoke();
        Close();
    }

    void Close()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 创建简单的按钮（备用方案）
    /// </summary>
    void CreateSimpleButtons()
    {
        // 创建确认按钮
        GameObject confirmGo = new GameObject("ConfirmButton");
        confirmGo.transform.SetParent(transform);
        confirmGo.transform.localPosition = new Vector3(-50, -50, 0);

        RectTransform confirmRT = confirmGo.AddComponent<RectTransform>();
        confirmRT.sizeDelta = new Vector2(100, 40);

        Image confirmImage = confirmGo.AddComponent<Image>();
        confirmImage.color = Color.green;

        confirmButton = confirmGo.AddComponent<Button>();

        Text confirmText = new GameObject("Text").AddComponent<Text>();
        confirmText.transform.SetParent(confirmGo.transform);
        confirmText.text = "确认";
        confirmText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        confirmText.alignment = TextAnchor.MiddleCenter;

        // 创建取消按钮
        GameObject cancelGo = new GameObject("CancelButton");
        cancelGo.transform.SetParent(transform);
        cancelGo.transform.localPosition = new Vector3(50, -50, 0);

        RectTransform cancelRT = cancelGo.AddComponent<RectTransform>();
        cancelRT.sizeDelta = new Vector2(100, 40);

        Image cancelImage = cancelGo.AddComponent<Image>();
        cancelImage.color = Color.red;

        cancelButton = cancelGo.AddComponent<Button>();

        Text cancelText = new GameObject("Text").AddComponent<Text>();
        cancelText.transform.SetParent(cancelGo.transform);
        cancelText.text = "取消";
        cancelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        cancelText.alignment = TextAnchor.MiddleCenter;

        Debug.Log("[ConfirmationDialog] 自动创建了简单按钮");
    }
}
