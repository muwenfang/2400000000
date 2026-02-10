using UnityEngine;
using UnityEngine.EventSystems;

// 这个脚本必须挂载在你的 "Slot Prefab" (方框预制体) 上
public class FormulaSlot : MonoBehaviour, IDropHandler
{
    private FormulaCardUI parentUI;
    public NumberCardInstance currentCard; // 当前填入的卡

    // 初始化，由 FormulaCardUI 调用
    public void Init(FormulaCardUI ui)
    {
        parentUI = ui;
    }

    // 当玩家把数字卡拖到这个方框上松手时触发
    public void OnDrop(PointerEventData eventData)
    {
        // 尝试获取拖拽物体的控制器
        var draggedCard = eventData.pointerDrag?.GetComponent<PlayerController>();

        if (draggedCard != null && draggedCard.BoundCard != null)
        {
            Debug.Log($"接收到卡牌: {draggedCard.BoundCard.GetOutPutValue()}");

            // 如果槽位里已经有牌了，这里可以加个逻辑：要么交换，要么不让放
            if (currentCard != null) return;

            // 相互绑定引用
            draggedCard.isPlacedInSlot = true;
            draggedCard.currentSlot = this; // 告诉卡牌它现在在这个槽位里

            // 通知父级 UI (CardManager) 记录数据
            parentUI.OnSlotFilled(draggedCard.BoundCard);

            //  视觉效果：把卡牌吸附到槽位中心，并设为子物体
            draggedCard.transform.SetParent(transform);
            draggedCard.transform.localPosition = Vector3.zero;

            // 强制重置 RectTransform 属性
            RectTransform cardRT = draggedCard.GetComponent<RectTransform>();
            if (cardRT != null)
            {
                // 铺满槽位或居中
                cardRT.anchorMin = new Vector2(0.5f, 0.5f);
                cardRT.anchorMax = new Vector2(0.5f, 0.5f);
                cardRT.pivot = new Vector2(0.5f, 0.5f);
                cardRT.anchoredPosition = Vector2.zero; // 绝对居中
            }
            // 记录当前卡
            currentCard = draggedCard.BoundCard;
        }
    }
    // 【新增】卡牌被拖走时调用
    public void ClearSlot()
    {
        currentCard = null;
        Debug.Log("槽位已清空");
    }
}
