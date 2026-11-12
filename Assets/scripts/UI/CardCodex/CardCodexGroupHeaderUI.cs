using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 图鉴分组标题组件（可选）：挂在"分组标题预制体"的根节点上，面板只负责写入标题文本。
/// 想在图鉴里插入"数字卡 / 公式卡 / 祝福 · 总是刷新 …"这样的分隔标题时使用；
/// 面板上的 groupHeaderPrefab 留空则表示不要标题，纯卡片流。
/// </summary>
public class CardCodexGroupHeaderUI : MonoBehaviour
{
    [Tooltip("标题文本组件（Inspector 拖入）")]
    public Text titleText;

    public void SetTitle(string title)
    {
        if (titleText != null)
        {
            titleText.text = title ?? string.Empty;
        }
    }
}
