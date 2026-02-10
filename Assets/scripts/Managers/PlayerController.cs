using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 拖动卡牌
/// </summary>
public interface NumberCardLayoutView
{
    void Bind(NumberCardData data);
}
public class PlayerController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private UnityEngine.Vector2 originalLocalPos;
    private UnityEngine.Vector2 dragOffset;
    private RectTransform rectTransform;
    private Canvas canvas;
    CanvasGroup canvasGroup;
    private Transform originalParent;
    public NumberCardInstance BoundCard { get; private set; }
    // 【关键】记录当前卡牌在哪
    public FormulaSlot currentSlot;
    public bool isPlacedInSlot = false;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();

    }
    public void Bind(NumberCardInstance card)
    {
        BoundCard = card;
        // 检查是否每张卡牌都被绑定
        Debug.Log($"绑定卡牌：{card.cardData.layoutType}");
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 1. 如果是从槽位里拖出来的，通知槽位清空数据
        if (currentSlot != null)
        {
            currentSlot.ClearSlot();
            currentSlot = null;
        }
        isPlacedInSlot = false; // 开始拖拽时重置标记

        // 2. 记录原始父物体和位置
        originalParent = transform.parent;
        originalLocalPos = rectTransform.localPosition;
        // 添加半透明效果
        GetComponent<CanvasGroup>().alpha = 0.6f;
        GetComponent<CanvasGroup>().blocksRaycasts = false;

        // 【优化】拖拽时把父物体设为 Canvas，防止被手牌区的 LayoutGroup 限制
        transform.SetParent(canvas.transform, true);
        transform.SetAsLastSibling();
        //RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, Input.mousePosition, canvas.worldCamera, out dragOffset);
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 直接将物体的世界坐标设为鼠标的屏幕坐标
        // 这样无论 UI 缩放是多少，卡牌都会精准在鼠标指针下方
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // 恢复视觉效果
        GetComponent<CanvasGroup>().alpha = 1f;
        GetComponent<CanvasGroup>().blocksRaycasts = true;

        if (!isPlacedInSlot)
        {
            // ❌ 没进槽位 → 回原位
            transform.SetParent(originalParent, false);
            rectTransform.localPosition = originalLocalPos;
        }

    }
    // 给 FormulaSlot 调用
    public void OnDroppedIntoSlot(Transform slot)
    {
        isPlacedInSlot = true;
        transform.SetParent(slot, false);
        rectTransform.anchoredPosition = Vector2.zero;
    }

}


