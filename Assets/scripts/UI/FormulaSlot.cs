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

            // 1. 通知父级 UI (CardManager) 记录数据
            parentUI.OnSlotFilled(draggedCard.BoundCard);

            // 2. 视觉效果：把卡牌吸附到槽位中心，并设为子物体
            draggedCard.transform.SetParent(transform);
            draggedCard.transform.localPosition = Vector3.zero;

            // 3. 记录当前卡
            currentCard = draggedCard.BoundCard;
        }
    }
}
