using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CardSelectionManager : MonoBehaviour
{
    public static CardSelectionManager Instance;

    public enum SelectionMode
    {
        None,
        CardCheat,
        RemoveCard,
        WishCoinSelect,
        MoreMoreBetter,
        DarkBoxSelect,
        MorningStarSelect
    }

    private SelectionMode currentMode;
    private Action<object> selectionCallback;

    // 模式变更事件：各面板订阅此事件以切换点击行为
    public event System.Action<SelectionMode> OnSelectionModeChanged;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void StartCardSelection(SelectionMode mode, Action<object> callback)
    {
        currentMode = mode;
        selectionCallback = callback;
        Debug.Log($"[CardSelectionManager] 开启卡牌选择模式：{mode}");
        if (mode == SelectionMode.RemoveCard)
        {
            UIManager.Instance.OpenCardDeletionDeck();
        }

        // 通知所有订阅面板切换点击模式
        OnSelectionModeChanged?.Invoke(mode);
    }

    public void OnCardSelected(object selectedObject)
    {
        if (selectedObject == null)
        {
            Debug.LogWarning("[CardSelectionManager] 选择了空对象");
            return;
        }
        Debug.Log($"[CardSelectionManager] 选择了：{selectedObject}");
        selectionCallback?.Invoke(selectedObject);
        if (currentMode != SelectionMode.RemoveCard)
        {
            EndCardSelection();
        }
    }

    public void EndCardSelection()
    {
        Debug.Log("[CardSelectionManager] 关闭卡牌选择模式");
        currentMode = SelectionMode.None;
        selectionCallback = null;

        // 通知所有订阅面板切换回普通模式
        OnSelectionModeChanged?.Invoke(SelectionMode.None);

        // 选择结束：关闭祝福选择提示文本（若处于祝福选择流程中）
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideCardSelectionBlessing();
        }
    }

    public SelectionMode GetCurrentMode()
    {
        return currentMode;
    }

    public bool IsSelecting()
    {
        return currentMode != SelectionMode.None;
    }

    // 便捷属性：判断当前是否为删卡模式
    public static bool IsDeleteMode => Instance != null && Instance.GetCurrentMode() == SelectionMode.RemoveCard;
}
