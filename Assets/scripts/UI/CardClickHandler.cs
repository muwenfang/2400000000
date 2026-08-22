using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 统一卡牌点击处理器
/// 挂在卡牌库的 contentRoot 上：
/// - 点击卡牌：根据当前选择模式分发到所属面板处理（删卡 / 选卡）
/// - 悬停卡牌：删卡模式下将卡牌背景高亮为微红色，提示该卡可删除
/// </summary>
public class CardClickHandler : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    // 持有对所属面板的引用（通过接口解耦）
    private ISelectablePanel ownerPanel;

    [Header("删卡悬停效果")]
    [Tooltip("悬停时背景叠加的微红色（叠加强度见 Lerp 系数）")]
    [SerializeField] private Color hoverTint = new Color(1f, 0.65f, 0.65f, 1f);

    // 当前悬停的卡牌根节点，以及记录各卡牌背景的原始颜色
    private GameObject currentHoveredCard;
    private readonly Dictionary<Image, Color> originalBackgroundColors = new Dictionary<Image, Color>();

    /// <summary>
    /// 初始化处理器，由面板在 OnEnable / Start 中调用
    /// </summary>
    /// <param name="panel">实现了 ISelectablePanel 的所属面板</param>
    public void Initialize(ISelectablePanel panel)
    {
        ownerPanel = panel;
    }

    /// <summary>
    /// Unity 事件系统回调：当 contentRoot 区域被点击时触发
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        // ① 前置检查：面板引用、CardSelectionManager 实例、是否处于选择模式
        if (ownerPanel == null) return;
        if (CardSelectionManager.Instance == null) return;
        if (!CardSelectionManager.Instance.IsSelecting()) return;

        // ② 获取被点击的物体：
        //    必须用射线命中的物体（pointerCurrentRaycast）而不是 pointerPress。
        //    因为卡牌本身没有 IPointerDownHandler，Unity 事件系统会把 pointerPress
        //    赋值为挂在 contentRoot 上的本处理器，导致永远定位不到卡牌根节点。
        GameObject clicked = eventData.pointerCurrentRaycast.gameObject;
        if (clicked == null)
            clicked = eventData.pointerPress;
        if (clicked == null) return;

        // ③ 向上遍历 Transform 层级，找到 contentRoot（即 this.transform）的直接子物体
        //    该直接子物体即为被点击的卡牌根节点
        Transform cardRoot = FindCardRoot(clicked.transform);
        if (cardRoot == null) return;

        // ④ 获取当前选择模式并分发到所属面板处理
        CardSelectionManager.SelectionMode mode = CardSelectionManager.Instance.GetCurrentMode();
        ownerPanel.HandleCardClick(cardRoot.gameObject, mode);
    }

    /// <summary>
    /// 悬停进入：删卡模式下高亮卡牌为微红色
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsRemoveCardMode()) return;
        if (eventData.pointerEnter == null) return;

        Transform cardRoot = FindCardRoot(eventData.pointerEnter.transform);
        if (cardRoot == null) return;

        // 若直接悬停到另一张卡（未触发 Exit），先恢复上一张
        if (currentHoveredCard != null && currentHoveredCard != cardRoot.gameObject)
        {
            RestoreHover(currentHoveredCard);
        }

        currentHoveredCard = cardRoot.gameObject;
        ApplyHover(currentHoveredCard);
    }

    /// <summary>
    /// 悬停离开：恢复卡牌背景原始颜色
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (currentHoveredCard == null) return;
        RestoreHover(currentHoveredCard);
        currentHoveredCard = null;
    }

    private void OnDisable()
    {
        // 面板被隐藏 / 卡片被重建时清理悬停状态，避免引用已销毁物体
        if (currentHoveredCard != null)
        {
            RestoreHover(currentHoveredCard);
            currentHoveredCard = null;
        }
    }

    private void OnDestroy()
    {
        ownerPanel = null;
    }

    /// <summary>
    /// 是否处于删卡选择模式
    /// </summary>
    private static bool IsRemoveCardMode()
    {
        return CardSelectionManager.Instance != null &&
               CardSelectionManager.Instance.GetCurrentMode() == CardSelectionManager.SelectionMode.RemoveCard;
    }

    /// <summary>
    /// 从被点击/悬停的物体向上找到 contentRoot 的直接子物体（卡牌根节点）
    /// </summary>
    private Transform FindCardRoot(Transform target)
    {
        Transform cardRoot = target;
        while (cardRoot != null && cardRoot.parent != transform)
        {
            cardRoot = cardRoot.parent;
        }
        if (cardRoot == null || cardRoot == transform) return null;
        return cardRoot;
    }

    /// <summary>
    /// 获取卡牌根节点上的背景 Image（优先根节点自身，其次查找子物体）
    /// </summary>
    private static Image GetCardBackground(GameObject cardRoot)
    {
        if (cardRoot == null) return null;
        Image img = cardRoot.GetComponent<Image>();
        if (img == null)
            img = cardRoot.GetComponentInChildren<Image>(true);
        return img;
    }

    /// <summary>
    /// 将卡牌背景叠加微红色
    /// </summary>
    private void ApplyHover(GameObject cardRoot)
    {
        Image bg = GetCardBackground(cardRoot);
        if (bg == null) return;

        if (!originalBackgroundColors.ContainsKey(bg))
            originalBackgroundColors[bg] = bg.color;

        Color original = originalBackgroundColors[bg];
        bg.color = new Color(
            Mathf.Lerp(original.r, hoverTint.r, 0.4f),
            Mathf.Lerp(original.g, hoverTint.g, 0.4f),
            Mathf.Lerp(original.b, hoverTint.b, 0.4f),
            original.a);
    }

    /// <summary>
    /// 恢复卡牌背景原始颜色
    /// </summary>
    private void RestoreHover(GameObject cardRoot)
    {
        if (cardRoot == null) return;
        Image bg = GetCardBackground(cardRoot);
        if (bg != null && originalBackgroundColors.TryGetValue(bg, out var original))
        {
            bg.color = original;
            originalBackgroundColors.Remove(bg);
        }
    }
}
