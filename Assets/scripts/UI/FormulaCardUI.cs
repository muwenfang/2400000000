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

            if (c == '#') // 生成槽位
            {
                if (slotPrefab != null)
                {
                    go = Instantiate(slotPrefab, formulaArea);

                    // [修复 2] 安全获取组件 (防止报错中断)
                    FormulaSlot slot = go.GetComponent<FormulaSlot>();
                    if (slot != null)
                    {
                        slot.Init(this);
                        slots.Add(slot);
                    }
                    else
                    {
                        Debug.LogError("SlotPrefab 缺少 FormulaSlot 脚本！");
                    }
                }
            }
            else // 生成符号 (+ - * /)
            {
                if (textPrefab != null)
                {
                    go = Instantiate(textPrefab, formulaArea);

                    // [修复 3] 使用 GetComponentInChildren (兼容 Text 在子物体的情况)
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

                // 确保有布局元素，否则 LayoutGroup 可能无法正确控制大小
                var le = go.GetComponent<LayoutElement>();
                if (le == null) le = go.AddComponent<LayoutElement>();
            }
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
