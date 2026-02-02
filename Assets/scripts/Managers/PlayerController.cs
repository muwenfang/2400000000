using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 拖动卡牌
/// </summary>
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
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 添加半透明效果
        GetComponent<CanvasGroup>().alpha = 0.6f;
        GetComponent<CanvasGroup>().blocksRaycasts = false;

        transform.SetAsLastSibling();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, Input.mousePosition, canvas.worldCamera, out dragOffset);
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
    }
    public void ReturnStartMenu()
    {
        //加载起始菜单场景

    }
}
public class FormulaSlot : MonoBehaviour, IDropHandler
{
    public Image background;
    public Text valueText;

    private FormulaCardUI owner;
    private NumberCardInstance boundCard;

    public void Init(FormulaCardUI ownerUI)
    {
        owner = ownerUI;
        valueText.text = "?";
    }

    public void OnDrop(PointerEventData eventData)
    {
        PlayerController drag = eventData.pointerDrag?
            .GetComponent<PlayerController>();

        if (drag == null || drag.BoundCard == null)
            return;

        if (boundCard != null)
            return; // 已填

        boundCard = drag.BoundCard;
        valueText.text = boundCard.GetOutPutValue().ToString();

        owner.OnSlotFilled(boundCard);
    }
}
