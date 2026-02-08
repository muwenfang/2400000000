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

        foreach (char c in formula.Pattern)
        {
            GameObject go;
            if (c == '#')
            {
                go = Instantiate(slotPrefab, formulaArea);
                FormulaSlot slot = go.GetComponent<FormulaSlot>();
                slot.Init(this);
                slots.Add(slot);
            }
            else
            {
                go = Instantiate(textPrefab, formulaArea);
                go.GetComponent<Text>().text = c.ToString();
                Debug.Log($"生成公式符号：{c}");
            }

            // --- 新增：强制纠正缩放和基础尺寸 ---
            go.transform.localScale = Vector3.one;
            var le = go.GetComponent<LayoutElement>();
            if (le == null) le = go.AddComponent<LayoutElement>();
            le.preferredWidth = 80;  // 给符号和槽位一个默认宽度
            le.preferredHeight = 160; // 给一个默认高度
            
        }
    }

    void Clear()
    {
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
