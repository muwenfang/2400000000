using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 拖动卡牌
/// </summary>
public class PlayerController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private UnityEngine.Vector2 originalLocalPos;
    private RectTransform rectTransform;
    private Canvas canvas;
    private Transform originalParent;

    private UnityEngine.Vector2 dragOffset;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        originalParent = transform.parent;
        originalLocalPos = rectTransform.localPosition;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
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
        transform.SetParent(originalParent);
        rectTransform.localPosition = originalLocalPos;

        // 检测是否拖到公式区域
        if (eventData.pointerEnter != null &&
            eventData.pointerEnter.CompareTag("FormulaArea"))
        {
            CardUI ui = GetComponent<CardUI>();
            if (ui != null)
            {
                CardManager.Instance.AddNumberCardToFormula(ui.BoundCard);
            }
        }
    }
    public void ReturnStartMenu()
    {
        //加载起始菜单场景

    }
}
