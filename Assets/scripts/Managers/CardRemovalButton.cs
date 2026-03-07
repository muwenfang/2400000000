using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 卡牌库关闭按钮脚本
/// 附加到卡牌库面板（myNumberCardPanel 等）的关闭按钮上
/// </summary>
public class CardDeckCloseButton : MonoBehaviour
{
    [Header("按钮引用")]
    public Button closeButton;

    [Header("删除模式")]
    [Tooltip("是否在删除模式下")]
    public bool isInDeleteMode = false;

    void Start()
    {
        // 获取按钮组件
        if (closeButton == null)
        {
            closeButton = GetComponent<Button>();
        }

        // 添加点击事件
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseButtonClick);
            Debug.Log("[CardDeckCloseButton] 卡牌库关闭按钮已初始化");
        }
        else
        {
            Debug.LogError("[CardDeckCloseButton] 找不到 Button 组件");
        }
    }

    /// <summary>
    /// 关闭按钮点击事件
    /// </summary>
    void OnCloseButtonClick()
    {
        // 检查是否在删除模式
        if (isInDeleteMode)
        {
            Debug.Log("[CardDeckCloseButton] 退出删除模式");

            // 关闭删除模式
            if (CardRemovalManager.Instance != null)
            {
                CardRemovalManager.Instance.CloseDeleteMode();
            }
        }
        else
        {
            Debug.Log("[CardDeckCloseButton] 关闭卡牌库");

            // 只是关闭卡牌库
            UIManager.Instance.CloseCardDeck();
        }
    }

    /// <summary>
    /// 设置删除模式状态（ShowMyCard 调用）
    /// </summary>
    public void SetDeleteMode(bool inDeleteMode)
    {
        isInDeleteMode = inDeleteMode;
        Debug.Log($"[CardDeckCloseButton] 删除模式状态: {isInDeleteMode}");
    }
}
