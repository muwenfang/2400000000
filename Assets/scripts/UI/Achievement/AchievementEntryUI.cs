using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 成就条目组件：挂在"成就条目预制体"的根节点上。
///
/// 预制体结构（全部在 Inspector 里手工搭建）：
///   Entry（本脚本）
///     ├─ NameText   ← 成就名字（固定显示），拖到 nameText
///     ├─ DescText   ← 描述文本：解锁显示描述，未解锁显示"未解锁"，拖到 descText
///     └─ LockImage  ← 可选，未解锁标记（如锁图标），拖到 lockImage
///
/// 本条目只做显示，不持有任何数据；解锁状态由 AchievementManager 提供。
/// </summary>
public class AchievementEntryUI : MonoBehaviour
{
    /// <summary>未解锁时描述文本的固定文案</summary>
    public const string LockedTextContent = "未解锁";

    [Header("文本（Inspector 拖入）")]
    [Tooltip("成就名字，任何状态都固定显示")]
    public Text nameText;

    [Tooltip("描述文本：解锁 → 显示描述；未解锁 → 显示「未解锁」")]
    public Text descText;

    [Header("未解锁标记（可选，Inspector 拖入）")]
    [Tooltip("未解锁时显示的图片（如锁图标）；解锁后自动隐藏。留空则只用文本区分")]
    public Image lockImage;

    /// <summary>填充一条成就的显示状态</summary>
    public void SetData(AchievementData data)
    {
        bool unlocked = data != null && data.IsUnlocked;

        if (nameText != null)
        {
            nameText.text = data != null ? data.name : string.Empty;
        }

        if (descText != null)
        {
            descText.text = unlocked ? data.description : LockedTextContent;
        }

        if (lockImage != null)
        {
            lockImage.gameObject.SetActive(!unlocked);
        }
    }
}
