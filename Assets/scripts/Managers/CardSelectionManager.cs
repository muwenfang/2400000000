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
        WishCoinSelect, // 许愿币祝福：只能选择祝福
        MoreMoreBetter  // 多多益善：只能选择公式卡 
    }

    private SelectionMode currentMode;
    private Action<object> selectionCallback;  // 选择完成后的回调

    [Header("冷却配置")]
    [SerializeField] private float selectionCooldown = 0.1f;  // 单位：秒

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
        // 冷却检查：防止连续快速点击
        if (CooldownManager.Instance != null &&
            CooldownManager.Instance.IsInCooldown(CooldownManager.CooldownType.CardSelection))
        {
            Debug.LogWarning($"[CardSelectionManager] 卡牌选择操作在冷却中");
            return;
        }
        if (selectedCard == null)
        {
            Debug.LogError("[CardSelectionManager] 选择的卡牌为空");
            return;
        }

        Debug.Log($"[CardSelectionManager] 用户选择卡牌：{selectedCard}");

        // 触发回调
        selectionCallback?.Invoke(selectedCard);

        // 开始冷却，防止连续点击
        if (CooldownManager.Instance != null)
        {
            CooldownManager.Instance.StartCooldown(
                CooldownManager.CooldownType.CardSelection,
                selectionCooldown
            );
        }

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
    /// <summary>
    /// 强制重置冷却（用于特殊情况）
    /// </summary>
    public void ResetSelectionCooldown()
    {
        if (CooldownManager.Instance != null)
        {
            CooldownManager.Instance.ResetCooldown(CooldownManager.CooldownType.CardSelection);
            Debug.Log("[CardSelectionManager] 已重置卡牌选择冷却");
        }
    }

    /// <summary>
    /// 设置选择冷却时间
    /// </summary>
    public void SetSelectionCooldown(float cooldownSeconds)
    {
        selectionCooldown = Mathf.Max(0.1f, cooldownSeconds);
        Debug.Log($"[CardSelectionManager] 已设置选择冷却时间为: {selectionCooldown}秒");
    }
}