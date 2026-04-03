using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 卡牌选择管理器 
/// 根据模式选择性激活卡牌
/// </summary>
public class CardSelectionManager : MonoBehaviour
{
    public static CardSelectionManager Instance;

    public enum SelectionMode
    {
        None,           // 无模式
        CardCheat,      // 老千祝福：只能选择数字卡
        RemoveCard,     // 删除卡牌：可以选择数字卡和公式卡
        WishCoinSelect  // 许愿币祝福：只能选择祝福
    }

    private SelectionMode currentMode;
    private Action<object> selectionCallback;  // 选择完成后的回调

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

    /// <summary>
    /// 开启卡牌选择模式
    /// </summary>
    public void StartCardSelection(SelectionMode mode, Action<object> callback)
    {
        currentMode = mode;
        selectionCallback = callback;

        Debug.Log($"[CardSelectionManager] 开启卡牌选择模式：{mode}");
    }
    
    /// <summary>
    /// 当卡牌被选中时调用
    /// </summary>
    public void OnCardSelected(object selectedCard)
    {
        if (selectedCard == null)
        {
            Debug.LogError("[CardSelectionManager] 选择的卡牌为空");
            return;
        }

        Debug.Log($"[CardSelectionManager] 用户选择卡牌：{selectedCard}");

        // 触发回调
        selectionCallback?.Invoke(selectedCard);

        //如果不是删卡模式才自动结束；删卡模式允许连续点击
        if (currentMode != SelectionMode.RemoveCard)
        {
            EndCardSelection();
        }
    }
    /// <summary>
    /// 关闭卡牌选择模式
    /// </summary>
    public void EndCardSelection()
    {
        Debug.Log("[CardSelectionManager] 关闭卡牌选择模式");
        currentMode = SelectionMode.None;
        selectionCallback = null;
    }

    /// <summary>
    /// 取消选择（用户主动取消）
    /// </summary>
    public void CancelSelection()
    {
        Debug.Log("[CardSelectionManager] 用户取消卡牌选择");
        EndCardSelection();
    }

    /// <summary>
    /// 获取当前选择模式
    /// </summary>
    public SelectionMode GetCurrentMode()
    {
        return currentMode;
    }
    /// <summary>
    /// 检查是否处于选择模式
    /// </summary>
    public bool IsSelecting()
    {
        return currentMode != SelectionMode.None;
    }

    /// <summary>
    /// 检查当前模式是否是删卡模式
    /// </summary>
    public bool IsDeletionMode()
    {
        return currentMode == SelectionMode.RemoveCard;
    }
}