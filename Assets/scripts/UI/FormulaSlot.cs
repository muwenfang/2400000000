using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 支持拖放、点击退回、防止重复
/// </summary>
public class FormulaSlot : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    private FormulaCardUI parentUI;
    private int slotIndex = -1;
    public NumberCardInstance currentCard;

    public int filledNumberCardCount = 0; // 记录已填入槽位的数字卡数量

    [Header("UI组件")]
    public Image background;

    [Header("颜色配置")]
    public Color emptyColor = new Color(0f, 1f, 0f, 0.8f);

    /// <summary>
    /// 初始化，由 FormulaCardUI 调用
    /// </summary>
    public void Init(FormulaCardUI ui, int index)
    {
        parentUI = ui;
        slotIndex = index;
    }

    /// <summary>
    /// 当玩家拖入卡牌时
    /// </summary>
    public void OnDrop(PointerEventData eventData)
    {
        // 获取拖动的卡牌控制器
        var draggedCard = eventData.pointerDrag?.GetComponent<PlayerController>();

        if (draggedCard == null)
        {
            Debug.LogWarning("拖动的对象没有 PlayerController");
            return;
        }

        if (draggedCard.BoundCard == null)
        {
            Debug.LogWarning("卡牌数据为空");
            return;
        }

        // 如果槽位视觉上已有卡（子对象中存在 PlayerController），拒绝放置并把拖拽卡退回手牌
        PlayerController existingCardController = null;
        foreach (Transform child in transform)
        {
            var pc = child.GetComponent<PlayerController>();
            if (pc != null)
            {
                existingCardController = pc;
                break;
            }
        }
        // 槽位已被占用，执行退回原有卡牌的逻辑
        if (existingCardController != null)
        {
            Debug.LogWarning("槽位已有卡牌，将原卡牌退回原来的位置（手牌）。");

            // 确定要移除的旧卡牌实例
            NumberCardInstance cardToRemove = currentCard ?? existingCardController.BoundCard;

            // 1. 从 CardManager 数据层按索引移除
            if (slotIndex >= 0)
            {
                CardManager.Instance.RemoveNumberCardFromFormulaAtIndex(slotIndex);
            }
            else if (cardToRemove != null)
            {
                CardManager.Instance.RemoveNumberCardFromFormula(cardToRemove);
            }

            // 2. 将旧卡牌的 UI 物理上退回手牌区
            if (cardToRemove != null)
            {
                ShowCardInHand(cardToRemove);
            }

            // 3. 清空槽位数据
            ClearSlot();

            // 注意：这里不再 return，而是继续往下走，让新拖入的卡牌顺利落入变空的槽位
        }

        // 检查数据层：卡是否已经在某个槽位中
        if (CardManager.Instance.IsCardInFormula(draggedCard.BoundCard))
        {
            Debug.LogWarning("卡牌已在其他槽位（数据层），拒绝重复放置。");
            // 如果卡已经在其他槽位，也把拖拽卡退回手牌（保持一致性）
            Transform handArea = CardManager.Instance.handCardParent;
            draggedCard.desiredDropParent = handArea;

            draggedCard.isPlacedInSlot = false;
            draggedCard.currentSlot = null;
            var cg2 = draggedCard.GetComponent<CanvasGroup>();
            if (cg2 != null) cg2.blocksRaycasts = true;
            return;
        }

        // 使用 PlayerController 的封装方法完成父级设置与定位（保证一致性）
        draggedCard.OnDroppedIntoSlot(transform);

        // 标记卡牌已放置
        draggedCard.isPlacedInSlot = true;
        draggedCard.currentSlot = this;

        filledNumberCardCount++;

        // 立即调用 OnDrawn() 重置卡牌状态（重置为初始值）
        if (draggedCard.BoundCard != null)
        {
            draggedCard.BoundCard.OnDrawn();
           // Debug.Log($"[FormulaSlot] 卡牌放入槽位 {slotIndex}: {draggedCard.BoundCard.cardData.cardName}，已调用 OnDrawn()");
        }

        // 获取卡牌的 UI 组件并更新绑定（关键：这样 boundInstance 才会被设置）
        NumberCardLayoutView cardView = draggedCard.GetComponent<NumberCardLayoutView>();
        if (cardView != null && draggedCard.BoundCard != null)
        {
            cardView.BindInstance(draggedCard.BoundCard);
            //Debug.Log($"[FormulaSlot] UI 已绑定卡牌实例: {draggedCard.BoundCard.cardData.cardName}");
        }
        else
        {
            if (cardView == null)
                Debug.LogWarning($"[FormulaSlot] 无法获取 NumberCardLayoutView 组件");
            if (draggedCard.BoundCard == null)
                Debug.LogWarning($"[FormulaSlot] 卡牌实例为空");
        }

        // 通知父级 UI（告知槽位索引）
        if (parentUI != null)
            parentUI.OnSlotFilled(slotIndex, draggedCard.BoundCard);

        // 视觉：卡牌吸附到槽位中心
        draggedCard.transform.SetParent(transform);
        draggedCard.transform.localPosition = Vector3.zero;
        draggedCard.transform.localScale = Vector3.one;

        // 调整卡牌大小和位置
        RectTransform cardRT = draggedCard.GetComponent<RectTransform>();
        if (cardRT != null)
        {
            cardRT.anchorMin = new Vector2(0.5f, 0.5f);
            cardRT.anchorMax = new Vector2(0.5f, 0.5f);
            cardRT.pivot = new Vector2(0.5f, 0.5f);
            cardRT.anchoredPosition = Vector2.zero;
        }

        // 记录当前卡牌
        currentCard = draggedCard.BoundCard;

    }

    /// <summary>
    /// 点击槽位退回卡牌
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        // 先查找是否存在子级卡牌 UI
        PlayerController childController = null;
        foreach (Transform child in transform)
        {
            var pc = child.GetComponent<PlayerController>();
            if (pc != null) { childController = pc; break; }
        }

        if (currentCard == null)
        {
            return;
        }

        // 检查游戏状态
        if (GameManager.Instance != null &&
            GameManager.Instance.currentState != GameManager.GameState.PlayerTurn)
        {
            Debug.LogWarning("当前不是玩家回合，无法退回卡牌");
            return;
        }

        Debug.Log($" 点击槽位，退回卡牌 {currentCard.cardData.cardName}");

        // 确定要移除的卡牌实例（优先使用 currentCard）
        NumberCardInstance cardToRemove = currentCard ?? (childController != null ? childController.BoundCard : null);
        //Debug.Log($"点击槽位，退回卡牌 {(cardToRemove != null ? cardToRemove.cardData.cardName : "未知")}");

        // 从 CardManager 数据层按索引移除
        if (slotIndex >= 0)
        {
            CardManager.Instance.RemoveNumberCardFromFormulaAtIndex(slotIndex);
        }
        else if (cardToRemove != null)
        {
            CardManager.Instance.RemoveNumberCardFromFormula(cardToRemove);
        }

        // 找到卡牌的 UI 并显示回手牌区
        ShowCardInHand(currentCard);

        // 清空槽位
        ClearSlot();

        filledNumberCardCount--;
    }

    /// <summary>
    /// 将卡牌显示回手牌区
    /// </summary>
    void ShowCardInHand(NumberCardInstance card)
    {
        // 在槽位的子对象中找到卡牌
        foreach (Transform child in transform)
        {
            var controller = child.GetComponent<PlayerController>();
            if (controller != null)
            {
                // 恢复到手牌区
                Transform handArea = CardManager.Instance.handCardParent;
                child.SetParent(handArea);
                child.gameObject.SetActive(true);

                // 重置位置和缩放
                child.localPosition = Vector3.zero;
                child.localScale = Vector3.one;

                // 清除槽位标记
                controller.isPlacedInSlot = false;
                controller.currentSlot = null;

                Debug.Log($"卡牌 {card.cardData.cardName} 退回手牌区");
                return;
            }
        }

        Debug.LogWarning($"在槽位中找不到卡牌 {card.cardData.cardName} 的UI");
    }

    /// <summary>
    /// 清空槽位
    /// </summary>
    public void ClearSlot()
    {
        // 如果有记录的卡牌，从数据层移除，避免 selectedNumberCards 残留
        if (currentCard != null)
        {
            CardManager.Instance.RemoveNumberCardFromFormula(currentCard);
        }

        // 如果槽位中存在子级 UI，清理其槽位标记（不移动 UI，拖拽逻辑会处理）
        foreach (Transform child in transform)
        {
            var controller = child.GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.isPlacedInSlot = false;
                controller.currentSlot = null;
                break;
            }
        }

        currentCard = null;
        Debug.Log("槽位已清空");
    }

}