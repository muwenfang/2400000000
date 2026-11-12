using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 图鉴条目组件：挂在"图鉴条目预制体"的根节点上。
///
/// 预制体结构（全部在 Inspector 里手工搭建）：
///   Entry（本脚本）
///     ├─ CardSlot       ← 卡片挂点：面板会把对应的卡牌预制体实例化到这里
///     ├─ LockImage      ← 上锁图片：拖到 lockImage
///     └─ LockedText     ← "未解锁" 文本：拖到 lockedText
///
/// 上锁状态只改这三处的显隐，不改任何卡牌数据。
/// </summary>
public class CardCodexEntryUI : MonoBehaviour
{
    [Header("卡片挂点")]
    [Tooltip("面板会把数字卡 / 公式卡 / 祝福的卡牌预制体实例化到这个节点下；留空则直接挂在条目根节点")]
    public Transform cardSlot;

    [Header("未解锁表现（Inspector 拖入）")]
    [Tooltip("未解锁时显示的上锁图片；解锁后自动隐藏")]
    public Image lockImage;

    [Tooltip("未解锁时显示的文本（内容固定为下方 lockedTextContent）")]
    public Text lockedText;

    [Header("文案")]
    [Tooltip("未解锁条目的文本内容")]
    public string lockedTextContent = CardCodexManager.LockedText;

    [Header("选项")]
    [Tooltip("未解锁时是否仍然生成卡面（默认不生成，只显示上锁图标与文案）")]
    public bool showCardWhenLocked = false;

    private bool isLocked;

    public bool IsLocked { get { return isLocked; } }

    /// <summary>图鉴卡片实际应该挂载的父节点</summary>
    public Transform CardParent
    {
        get { return cardSlot != null ? cardSlot : transform; }
    }

    /// <summary>设置上锁状态（只影响显示，不影响数据）</summary>
    public void SetLocked(bool locked)
    {
        isLocked = locked;

        if (lockImage != null)
        {
            lockImage.gameObject.SetActive(locked);
        }

        if (lockedText != null)
        {
            lockedText.text = locked ? lockedTextContent : string.Empty;
            lockedText.gameObject.SetActive(locked);
        }

        // 未解锁且不允许露出卡面时，隐藏挂点（面板也不会往里实例化卡牌）
        if (cardSlot != null && !showCardWhenLocked)
        {
            cardSlot.gameObject.SetActive(!locked);
        }
    }
}
