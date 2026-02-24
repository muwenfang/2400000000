using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 公式槽位 - 增强版
/// 支持拖放、点击退回、防止重复
/// </summary>
public class FormulaSlot : MonoBehaviour, IDropHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    private FormulaCardUI parentUI;
    public NumberCardInstance currentCard;

    [Header("UI组件")]
    public Image background;

    [Header("颜色配置")]
    public Color emptyColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    public Color occupiedColor = new Color(0f, 1f, 0f, 0.8f);
    public Color hoverColor = new Color(1f, 1f, 0f, 0.5f);

    /// <summary>
    /// 初始化，由 FormulaCardUI 调用
    /// </summary>
    public void Init(FormulaCardUI ui)
    {
        parentUI = ui;
        UpdateVisual();
    }

    /// <summary>
    /// 当玩家拖入卡牌时
    /// </summary>
    public void OnDrop(PointerEventData eventData)
    {
        // 检查1：槽位是否已占用
        if (currentCard != null)
        {
            Debug.LogWarning("槽位已有卡牌！");
            return;
        }

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

        //  检查2：卡牌是否已在其他槽位
        if (IsCardInAnySlot(draggedCard.BoundCard))
        {
            Debug.LogWarning("卡牌已在其他槽位！");
            return;
        }

        Debug.Log($"接收卡牌: {draggedCard.BoundCard.cardData.cardName} → 值: {draggedCard.BoundCard.GetOutPutValue()}");

        // 标记卡牌已放置
        draggedCard.isPlacedInSlot = true;
        draggedCard.currentSlot = this;

        // 通知父级 UI
        parentUI.OnSlotFilled(draggedCard.BoundCard);

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

        // 更新视觉
        UpdateVisual();
    }

    /// <summary>
    /// 点击槽位退回卡牌
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
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

        // 从 CardManager 移除
        CardManager.Instance.RemoveNumberCardFromFormula(currentCard);

        // 找到卡牌的 UI 并显示回手牌区
        ShowCardInHand(currentCard);

        // 清空槽位
        ClearSlot();
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
    /// 检查卡牌是否已在其他槽位
    /// </summary>
    bool IsCardInAnySlot(NumberCardInstance card)
    {
        if (CardManager.Instance == null)
        {
            return false;
        }

        return CardManager.Instance.selectedNumberCards.Contains(card);
    }

    /// <summary>
    /// 鼠标悬停
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentCard == null && background != null)
        {
            background.color = hoverColor;
        }
    }

    /// <summary>
    /// 鼠标离开
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        UpdateVisual();
    }

    /// <summary>
    /// 清空槽位
    /// </summary>
    public void ClearSlot()
    {
        currentCard = null;
        UpdateVisual();
        Debug.Log("槽位已清空");
    }

    /// <summary>
    /// 更新视觉显示
    /// </summary>
    void UpdateVisual()
    {
        if (background != null)
        {
            background.color = currentCard != null ? occupiedColor : emptyColor;
        }
    }

}