using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NumberCardLibrary", menuName = "Cards/NumberCardLibrary")]
public class NumberCardLibrary : ScriptableObject
{
    public List<NumberCardData> allCards = new List<NumberCardData>();

    public void InitializeDefaultCards()
    {
        allCards.Clear();

        allCards.Add(CreatePreset("递增卡", 0, NumberCardData.LogicalType.Normal,
            new NumberComponent { value = 0, isIncremental = true }, null));

        allCards.Add(CreatePreset("六面骰", 1, NumberCardData.LogicalType.Addition,
            new NumberComponent { isDice = true, diceSides = 4 }, 
            new NumberComponent { isDice = true, diceSides = 4 }
            ));

        // 这里可以继续添加更多预设卡牌

    }

    private NumberCardData CreatePreset(string name, int id, NumberCardData.LogicalType logic, NumberComponent a, NumberComponent b)
    {
        NumberCardData ds = ScriptableObject.CreateInstance<NumberCardData>();
        ds.cardName = name;
        ds.logicalType = logic;
        ds.partA = a;
        ds.partB = b;
        return ds;
    }
}
