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
            // 在 Bind 方法的循环内
            GameObject go = Instantiate(c == '#' ? slotPrefab : textPrefab, formulaArea);

            // --- 强化显示逻辑 ---
            // 1. 确保它是在 UI 层
            go.layer = LayerMask.NameToLayer("UI");

            // 2. 强制设置 RectTransform 的基础属性
            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.localScale = Vector3.one;
                rt.localPosition = new Vector3(rt.localPosition.x, rt.localPosition.y, 0); // 确保 Z 为 0
            }
            // --- 新增：暴力开启组件，确保显示 ---
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
            //else
            //{
            //    // 如果这里报错，说明你的 slotPrefab 真的引错了文件
            //    Debug.LogError("致命错误：预制体 " + slotPrefab.name + " 及其子层级里根本没挂 Image 组件！");
            //}

            var debugger = go.GetComponent<VisibilityDebugger>();
            if (debugger != null)
            {
                debugger.enabled = true; // 强制打勾
            }

            // 确保物体本身也是激活的
            go.SetActive(true);

            if (c == '#') // 生成槽位
            {
                if (slotPrefab != null)
                {
                    // 安全获取组件 (防止报错中断)
                    FormulaSlot slot = go.GetComponent<FormulaSlot>();
                    if (slot != null)
                    {
                        slot.enabled = true; // 确保脚本也是打勾的
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

                // 确保有布局元素，否则 LayoutGroup 可能无法正确控制大小
                var le = go.GetComponent<LayoutElement>();
                if (le == null) le = go.AddComponent<LayoutElement>();
            }
        }

        // 【新增】在循环结束后，强制刷新布局
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
