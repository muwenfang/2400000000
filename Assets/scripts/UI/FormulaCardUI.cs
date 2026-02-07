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

        foreach (char c in formula.Pattern)
        {
            if (c == '#')
            {
                GameObject go = Instantiate(slotPrefab, formulaArea);
                FormulaSlot slot = go.GetComponent<FormulaSlot>();
                slot.Init(this);
                slots.Add(slot);
            }
            else
            {
                GameObject go = Instantiate(textPrefab, formulaArea);
                go.GetComponent<Text>().text = c.ToString();
            }
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
