using UnityEngine;

/// <summary>
/// 可选择面板接口
/// 由 ShowMyNumberCard / ShowMyFormula / ShowMyBlessings 实现，
/// 供 CardClickHandler 在卡牌被点击时回调，替代逐卡添加 Button 的 O(n) 方案。
/// </summary>
public interface ISelectablePanel
{
    /// <summary>
    /// 当 CardSelectionManager 的选择模式变更时调用，面板据此更新 UI 状态
    /// </summary>
    void OnSelectionModeChanged(CardSelectionManager.SelectionMode newMode);

    /// <summary>
    /// 处理卡牌点击事件，由 CardClickHandler 统一调用
    /// </summary>
    /// <param name="clickedCardRoot">被点击卡牌的根 GameObject（contentRoot 的直接子物体）</param>
    /// <param name="mode">当前选择模式</param>
    void HandleCardClick(GameObject clickedCardRoot, CardSelectionManager.SelectionMode mode);
}
