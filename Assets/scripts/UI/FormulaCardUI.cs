using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FormulaCardUI : MonoBehaviour
{
    public Transform formulaArea;          // UI 容器
    public GameObject textPrefab;           // 显示 + * ( )
    public GameObject slotPrefab;           // # 槽位

    private readonly List<FormulaSlot> slots = new();

    public void Bind(FormulaCardData formula)
    {
        Clear();
        Debug.Log("开始绑定公式卡");
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

        foreach (char c in formula.Pattern)
        {
            GameObject go = null;
            if (c == '#')
            {
                // 生成填空槽
                if (slotPrefab != null)
                {
                    go = Instantiate(slotPrefab, formulaArea);
                    FormulaSlot slot = go.GetComponent<FormulaSlot>();
                    if (slot != null)
                    {
                        slot.Init(this);
                        slots.Add(slot);
                    }
                }
            }
            else
            {
                // 生成符号文本
                if (textPrefab != null)
                {
                    go = Instantiate(textPrefab, formulaArea);
                    // 【修正点】使用 GetComponentInChildren 以防 Text 在子物体上
                    Text txt = go.GetComponentInChildren<Text>();
                    if (txt != null)
                    {
                        txt.text = c.ToString();
                        txt.fontSize = 80; // 确保字体足够大可见
                        txt.color = Color.black; // 确保颜色不是透明或白色
                    }
                    else
                    {
                        Debug.LogError($"textPrefab {go.name} 上找不到 Text 组件！");
                    }
                }
            }

            // 强制布局参数 (防止缩放为0或被挤压)
            if (go != null)
            {
                go.transform.localScale = Vector3.one;
                go.SetActive(true);
            }
            // 强制刷新布局，防止 UI 没对齐
            LayoutRebuilder.ForceRebuildLayoutImmediate(formulaArea.GetComponent<RectTransform>());
        }
    }

    void Clear()
    {
        if (formulaArea == null) return;
        foreach (Transform child in formulaArea)
            Destroy(child.gameObject);
        slots.Clear();
    }

    // 被 Slot 调用
    public void OnSlotFilled(NumberCardInstance card)
    {
        CardManager.Instance.AddNumberCardToFormula(card);
    }
}
