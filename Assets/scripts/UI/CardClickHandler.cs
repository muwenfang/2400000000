using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 统一卡牌点击处理器
/// 挂载在面板的 contentRoot 上，实现 IPointerClickHandler，
/// 通过事件委托替代逐卡添加 Button，将 O(n) 的按钮激活优化为 O(1) 的点击分发。
/// </summary>
public class CardClickHandler : MonoBehaviour, IPointerClickHandler
{
    // 持有对所属面板的引用（通过接口解耦）
    private ISelectablePanel ownerPanel;

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

        // ② 获取被点击的最底层 GameObject
        GameObject clicked = eventData.pointerPress;
        if (clicked == null) return;

        // ③ 向上遍历 Transform 层级，找到 contentRoot（即 this.transform）的直接子物体
        //    该直接子物体即为被点击的卡牌根节点
        Transform cardRoot = clicked.transform;
        while (cardRoot != null && cardRoot.parent != transform)
        {
            cardRoot = cardRoot.parent;
        }

        // ④ 未找到有效卡牌根节点（点击了 contentRoot 本身的空白区域），忽略
        if (cardRoot == null || cardRoot == transform) return;

        // ⑤ 获取当前选择模式并分发到所属面板处理
        CardSelectionManager.SelectionMode mode = CardSelectionManager.Instance.GetCurrentMode();
        ownerPanel.HandleCardClick(cardRoot.gameObject, mode);
    }

    private void OnDestroy()
    {
        ownerPanel = null;
    }
}
