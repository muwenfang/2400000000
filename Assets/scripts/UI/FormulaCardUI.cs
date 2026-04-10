using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FormulaCardUI : MonoBehaviour
{
    [Header("容器")]
    public Transform formulaArea;          // UI 容器
    [Header("预制体")]
    public GameObject textPrefab;           // 显示 + * ( )
    public GameObject slotPrefab;           // # 槽位

    private readonly List<FormulaSlot> slots = new();
    // 保存当前绑定的公式卡数据
    private FormulaCardData currentFormulaData;

    public void Bind(FormulaCardData formula)
    {
        Clear();
        // 1. 安全检查
        if (formulaArea == null)
        {
            Debug.LogError("【严重错误】FormulaCardUI: formulaArea 未赋值！请在 Inspector 中拖入。");
            return;
        }
        if (formula == null)
        {
            Debug.LogError("【错误】FormulaCardUI: 传入的 formula 数据为 null");
            return;
        }
        // 保存数据供后续使用
        currentFormulaData = formula;
        // 确保 formulaArea 有 HorizontalLayoutGroup
        HorizontalLayoutGroup layoutGroup = formulaArea.GetComponent<HorizontalLayoutGroup>();
        if (layoutGroup == null)
        {
            layoutGroup = formulaArea.gameObject.AddComponent<HorizontalLayoutGroup>();
            // 关键设置：不要由 LayoutGroup 强制控制子项尺寸或拉伸它们
            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.spacing = 10f; // 元素间距，可在 Inspector 调整
            layoutGroup.childAlignment = TextAnchor.MiddleCenter;
        }
        
        // 槽位计数器：只对 '#' 增加，保证槽位索引连续并与 CardManager.selectedNumberCards 对齐
        int slotIndexCounter = 0;

        for (int i = 0; i < formula.Pattern.Length; i++)
        {
            char c = formula.Pattern[i];
            // 在 Bind 方法的循环内
            GameObject go = Instantiate(c == '#' ? slotPrefab : textPrefab, formulaArea);
            go.transform.SetSiblingIndex(i); // 确保顺序一致

            // --- 强化显示逻辑 ---
            // 确保它是在 UI 层
            go.layer = LayerMask.NameToLayer("UI");

            // 强制设置 RectTransform 的基础属性
            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.localScale = Vector3.one;
                rt.localPosition = new Vector3(rt.localPosition.x, rt.localPosition.y, 0); // 确保 Z 为 0
            }
            // --- 开启组件，确保显示 ---
            var textComp = go.GetComponentInChildren<Text>();
            var imageComp = go.GetComponentInChildren<Image>(true);// true 表示即使被禁用了也能找到
            if (textComp != null)
            {
                textComp.enabled = true; // 强制打勾
                textComp.text = c.ToString();
                textComp.color = new Color(textComp.color.r, textComp.color.g, textComp.color.b, 1f);
            }

            if (imageComp != null)
            {
                imageComp.enabled = true;
                imageComp.color = Color.white; // 确保颜色不是全透明
            }

            var debugger = go.GetComponent<VisibilityDebugger>();
            if (debugger != null)
            {
                debugger.enabled = true; // 强制打勾
            }

            // 确保物体本身也是激活的
            go.SetActive(true);

            // 配置 LayoutElement（关键）
            LayoutElement le = go.GetComponent<LayoutElement>();
            if (le == null) le = go.AddComponent<LayoutElement>();
            {
                le.flexibleWidth = 0;
                le.flexibleHeight = 0;

            }
            // 尝试读取对应预制体的 RectTransform.sizeDelta 作为首选尺寸（更贴合视觉）
            RectTransform prefabRT = (c == '#') ? (slotPrefab ? slotPrefab.GetComponent<RectTransform>() : null)
                                                : (textPrefab ? textPrefab.GetComponent<RectTransform>() : null);
            if (prefabRT != null)
            {
                // 使用预制体的 sizeDelta 作为 preferredSize（如果预制体在 Inspector 中正确设置了宽高）
                le.preferredWidth = prefabRT.sizeDelta.x > 0 ? prefabRT.sizeDelta.x : 50f;
                le.preferredHeight = prefabRT.sizeDelta.y > 0 ? prefabRT.sizeDelta.y : 50f;
            }

            if (c == '#') // 生成槽位
            {
                if (slotPrefab != null)
                {
                    // 安全获取组件 (防止报错中断)
                    FormulaSlot slot = go.GetComponent<FormulaSlot>();
                    if (slot != null)
                    {
                        slot.enabled = true; // 确保脚本也是打勾的
                        slot.Init(this,slotIndexCounter);
                        slots.Add(slot);
                        slotIndexCounter++;

                    }
                    else
                    {
                        Debug.LogError("SlotPrefab 缺少 FormulaSlot 脚本！");
                    }
                }
            }
            else // 生成符号 (+ - * /)
            {
                Text t = go.GetComponentInChildren<Text>();
                if (t != null)
                {
                    // 确保 Alpha 值为 1，且颜色不与背景冲突
                    Color c_temp = t.color;
                    c_temp.a = 1f;
                    t.color = c_temp;

                }
                
                if (textPrefab != null)
                {
                    //使用 GetComponentInChildren (兼容 Text 在子物体的情况)
                    Text txt = go.GetComponentInChildren<Text>();
                    if (txt != null)
                    {
                        txt.text = c.ToString();
                    }
                    else
                    {
                        Debug.LogError("TextPrefab 中找不到 Text 组件！");
                    }
                }
            }

            // 强制布局参数 (防止缩放为0或被挤压)
            if (go != null)
            {
                go.transform.localPosition = Vector3.zero;
                go.transform.localScale = Vector3.one;
                go.transform.localRotation = Quaternion.identity;

                Vector3 pos = go.transform.localPosition;
                pos.z = 0f;
                go.transform.localPosition = pos;

            }
        }

        //在循环结束后，强制刷新布局
        StartCoroutine(ForceUpdateLayout());
    }
    private IEnumerator ForceUpdateLayout()
    {
        // 等待一帧，让 Unity 完成组件初始化
        yield return null;

        // 强制刷新当前物体 (FormulaCard) 及其父物体 (FormulaArea) 的布局
        LayoutRebuilder.ForceRebuildLayoutImmediate(formulaArea.GetComponent<RectTransform>());

        // 如果公式卡本身也在一个 LayoutGroup 里，可能需要刷新根节点
        LayoutRebuilder.ForceRebuildLayoutImmediate(transform.GetComponent<RectTransform>());
    }
    /// <summary>
    /// 获取当前绑定的公式卡数据
    /// 用于卡牌选择系统（删卡功能）
    /// </summary>
    public FormulaCardData GetFormulaCardData()
    {
        if (currentFormulaData == null)
        {
            Debug.LogWarning("[FormulaCardUI] 当前没有绑定公式卡数据");
            return null;
        }

        return currentFormulaData;
    }
    void Clear()
    {
        if (formulaArea == null) return;
        foreach (Transform child in formulaArea)
            Destroy(child.gameObject);
        slots.Clear();
    }

    // 父级被槽位调用：告知槽位索引与卡牌
    public void OnSlotFilled(int slotIndex, NumberCardInstance card)
    {
        // 交给 CardManager 以索引写入
        CardManager.Instance.AddNumberCardToFormula(card, slotIndex);
    }
}
