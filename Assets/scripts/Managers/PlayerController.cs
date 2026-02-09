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

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
        originalParent = transform.parent;
        originalLocalPos = rectTransform.localPosition;
    }
    public void Bind(NumberCardInstance card)
    {
        BoundCard = card;
        // 检查是否每张卡牌都被绑定
        Debug.Log($"绑定卡牌：{card.cardData.layoutType}");
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 添加半透明效果
        GetComponent<CanvasGroup>().alpha = 0.6f;
        GetComponent<CanvasGroup>().blocksRaycasts = false;

        transform.SetAsLastSibling();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, Input.mousePosition, canvas.worldCamera, out dragOffset);
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(originalParent as RectTransform, Input.mousePosition - (UnityEngine.Vector3)dragOffset, canvas.worldCamera, out UnityEngine.Vector2 localPos))
        {
            rectTransform.localPosition = localPos;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // 恢复视觉效果
        GetComponent<CanvasGroup>().alpha = 1f;
        GetComponent<CanvasGroup>().blocksRaycasts = true;

        transform.SetParent(originalParent);
        rectTransform.localPosition = originalLocalPos;

        // 检测是否拖到公式区域
        if (eventData.pointerEnter != null &&
            eventData.pointerEnter.CompareTag("FormulaArea"))
        {
            if (eventData.pointerEnter != null &&
    eventData.pointerEnter.CompareTag("FormulaArea"))
            {
                if (BoundCard != null)
                {
                    CardManager.Instance.AddNumberCardToFormula(BoundCard);
                }
            }

        }
        canvasGroup.blocksRaycasts = true;
    }
    
}

