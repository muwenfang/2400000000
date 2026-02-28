using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 拖动卡牌
/// </summary>
public interface NumberCardLayoutView
{
    /// <summary>
    /// 绑定静态卡牌数据（仅显示初始值）
    /// </summary>
    void Bind(NumberCardData data);

    /// <summary>
    /// 绑定卡牌实例（显示当前值，支持颜色）
    /// </summary>
    void BindInstance(NumberCardInstance instance);
}
public class PlayerController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private UnityEngine.Vector2 originalLocalPos;      // 记录卡牌原始位置
    private Vector3 dragOffset;          // 鼠标与卡牌中心的偏移量
    private RectTransform rectTransform;
    private Transform originalParent;

    [Header("配置")]
    private Canvas canvas;
    CanvasGroup canvasGroup;

    [Header("卡牌数据")]
    private NumberCardInstance cardInstance;

    // 【关键】记录当前卡牌在哪
    public FormulaSlot currentSlot;
    public bool isPlacedInSlot = false;
    private int originalSiblingIndex; // 记录原始层级索引
    public NumberCardInstance BoundCard { get; private set; }

    //由槽位决定最终要返回到哪个父物体（解决 OnEndDrag 与 OnDrop 的执行顺序冲突）
    public Transform desiredDropParent;

    [Header("UI 显示引用")]
    public Text textA;       // 对应 PartA 的数值显示
    public Text textB;       // 对应 PartB 的数值显示

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();

    }
    public void Bind(NumberCardInstance card)
    {
        BoundCard = card;
        // 1. 更新 PartA 的显示
        if (textA != null)
        {
            textA.text = card.currentA.ToString();
            // 视觉反馈：如果是递增数，可以设为绿色
            if (card.cardData.partA.isIncremental) textA.color = Color.green;
            else if (card.cardData.partA.isDice) textA.color = Color.red;
            else textA.color = Color.black;
        }
        if (textB != null)
        {
            textB.text = card.currentB.ToString();
            // 视觉反馈：如果是递增数，可以设为绿色
            if (card.cardData.partB.isIncremental) textB.color = Color.green;
            else if (card.cardData.partB.isDice) textB.color = Color.red;
            else textB.color = Color.black;
        }

        // 2. 根据逻辑类型处理 PartB 和 运算符
        if (card.cardData.logicalType == NumberCardData.LogicalType.Normal)
        {
            // 单数字模式：隐藏 PartB 和 运算符
            if (textB != null) textB.gameObject.SetActive(false);
        }
        else
        {
            // 运算模式：显示并更新内容
            if (textB != null)
            {
                textB.gameObject.SetActive(true);
                textB.text = card.currentB.ToString();
                //if (card.cardData.partB.isIncremental) textB.color = Color.green;
            }

        }

        // 检查是否每张卡牌都被绑定
        Debug.Log($"绑定卡牌：{card.cardData.layoutType}");
    }
    /// <summary>
    /// 获取卡牌实例
    /// </summary>
    public NumberCardInstance GetCardInstance()
    {
        return cardInstance;
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

        //计算偏移量：记录鼠标点击时相对于卡牌中心的位置
        // transform.position 是世界坐标，eventData.position 也是屏幕世界坐标
        dragOffset = transform.position - (Vector3)eventData.position;

        // 记录原始父物体和位置
        originalParent = transform.parent;
        originalLocalPos = rectTransform.localPosition;
        // 记录它在手牌区的顺序位置
        originalSiblingIndex = transform.GetSiblingIndex();

        // 添加半透明效果
        GetComponent<CanvasGroup>().alpha = 0.6f;
        GetComponent<CanvasGroup>().blocksRaycasts = false;

        // 拖拽时把父物体设为 Canvas，防止被手牌区的 LayoutGroup 限制
        transform.SetParent(canvas.transform, true);
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 直接将物体的世界坐标设为鼠标的屏幕坐标
        // 这样无论 UI 缩放是多少，卡牌都会精准在鼠标指针下方
        transform.position = (Vector3)eventData.position + dragOffset;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // 恢复视觉效果
        GetComponent<CanvasGroup>().alpha = 1f;
        GetComponent<CanvasGroup>().blocksRaycasts = true;

        if (!isPlacedInSlot)
        {
            // 优先使用 desiredDropParent（由槽位在 OnDrop 时设置），能解决 OnEndDrag 与 OnDrop 顺序问题
            if (desiredDropParent != null)
            {
                transform.SetParent(desiredDropParent, false);
                // 将卡片放到目标父对象的末尾
                transform.SetAsLastSibling();
                // 重置本地坐标与缩放以保证布局显示合理
                rectTransform.localPosition = Vector3.zero;
                rectTransform.localScale = Vector3.one;
                desiredDropParent = null;
            }
            else
            {
                // 没进槽位 → 回原位
                transform.SetParent(originalParent, false);
                // 恢复它原来的排列顺序
                transform.SetSiblingIndex(originalSiblingIndex);
                // 恢复坐标
                rectTransform.localPosition = originalLocalPos;
            }
        }

    }
    // 给 FormulaSlot 调用
    public void OnDroppedIntoSlot(Transform slot)
    {
        isPlacedInSlot = true;
        // 把期望父物体清空（真正放入槽位，取消之前的返回指令）
        desiredDropParent = null;
        transform.SetParent(slot, false);
        rectTransform.anchoredPosition = Vector2.zero;
    }

}


