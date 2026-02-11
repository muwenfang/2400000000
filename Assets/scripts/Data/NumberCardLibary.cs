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

        allCards.Add(CreatePreset("{0}", 0, NumberCardData.LogicalType.Normal,
            new NumberComponent { value = 0, isIncremental = true }, null));

        allCards.Add(CreatePreset("~4~+~4~", 1, NumberCardData.LogicalType.Addition,
            new NumberComponent { isDice = true, diceSides = 4 }, 
            new NumberComponent { isDice = true, diceSides = 4 }
            ));

        allCards.Add(CreatePreset("~20~", 2, NumberCardData.LogicalType.Normal,
            new NumberComponent { isDice = true, diceSides = 20 }, null)); 

        allCards.Add(CreatePreset("20", 3, NumberCardData.LogicalType.Normal,
            new NumberComponent { isDice = false, isIncremental = false, value = 20 }, null));

        allCards.Add(CreatePreset("2^~6~", 4, NumberCardData.LogicalType.Power,
            new NumberComponent { isDice = false, isIncremental = false, value = 2 }, 
            new NumberComponent { isDice = true, diceSides = 6 }
            ));
        
        allCards.Add(CreatePreset("{0}*{0}", 5, NumberCardData.LogicalType.Multiplication,
            new NumberComponent { value = 0, isIncremental = true }, 
            new NumberComponent { value = 0, isIncremental = true }
            ));

        allCards.Add(CreatePreset("3^~4~", 6, NumberCardData.LogicalType.Power,
            new NumberComponent { value = 3, isIncremental = false }, 
            new NumberComponent { isDice = true, diceSides = 4 }
            ));
        
        allCards.Add(CreatePreset("~4~^~4~", 7, NumberCardData.LogicalType.Power,
            new NumberComponent { isDice = true, diceSides = 4 }, 
            new NumberComponent { isDice = true, diceSides = 4 }
            ));
        
        allCards.Add(CreatePreset("100", 8, NumberCardData.LogicalType.Normal,
            new NumberComponent { value = 100, isIncremental = false }, null)); 

        allCards.Add(CreatePreset("{0}^{0}", 9, NumberCardData.LogicalType.Power,
            new NumberComponent { value = 0, isIncremental = true }, 
            new NumberComponent { value = 0, isIncremental = true }
            ));

        allCards.Add(CreatePreset("10", 10, NumberCardData.LogicalType.Normal,
            new NumberComponent { value = 10, isIncremental = false }, null)); 

        allCards.Add(CreatePreset("2^~4~", 11, NumberCardData.LogicalType.Power,
            new NumberComponent { value = 2, isIncremental = false }, 
            new NumberComponent { isDice = true, diceSides = 4 }
            ));   

        allCards.Add(CreatePreset("15", 12, NumberCardData.LogicalType.Normal,
            new NumberComponent { value =15, isIncremental = false }, null)); 
        
        allCards.Add(CreatePreset("25", 13, NumberCardData.LogicalType.Normal,
            new NumberComponent { value = 25, isIncremental = false }, null)); 

        allCards.Add(CreatePreset("30", 14, NumberCardData.LogicalType.Normal,
            new NumberComponent { value = 30, isIncremental = false }, null)); 

        allCards.Add(CreatePreset("{0}^2", 15, NumberCardData.LogicalType.Power,
            new NumberComponent { value = 0, isIncremental = true }, 
            new NumberComponent { value = 2, isIncremental = false }
            ));
        
        allCards.Add(CreatePreset("50", 16, NumberCardData.LogicalType.Normal,
            new NumberComponent { value = 50, isIncremental = false }, null)); 
        
        allCards.Add(CreatePreset("2^~8~", 17, NumberCardData.LogicalType.Power,
            new NumberComponent { value = 2, isIncremental = false }, 
            new NumberComponent { isDice = true, diceSides = 8 }
            ));

        allCards.Add(CreatePreset("2^{0}", 18, NumberCardData.LogicalType.Power,
            new NumberComponent { value = 2, isIncremental = false }, 
            new NumberComponent { value = 0, isIncremental = true }
            ));

        allCards.Add(CreatePreset("{0}^3", 19, NumberCardData.LogicalType.Power,
            new NumberComponent { value = 0, isIncremental = true }, 
            new NumberComponent { value = 3, isIncremental = false }
            ));
        
        allCards.Add(CreatePreset("2^~12~", 20, NumberCardData.LogicalType.Power,
            new NumberComponent { value = 2, isIncremental = false }, 
            new NumberComponent { isDice = true, diceSides = 12 }
            ));
        
        allCards.Add(CreatePreset("3^~6~", 21, NumberCardData.LogicalType.Power,
            new NumberComponent { value = 3, isIncremental = false }, 
            new NumberComponent { isDice = true, diceSides = 6 }
            ));

        allCards.Add(CreatePreset("{0}^4", 22, NumberCardData.LogicalType.Power,
            new NumberComponent { value = 0, isIncremental = true }, 
            new NumberComponent { value = 4 }
            ));
        
        allCards.Add(CreatePreset("3^{0}", 23, NumberCardData.LogicalType.Power,
            new NumberComponent { value = 3 }, 
            new NumberComponent { value = 0, isIncremental = true }
            ));
        
        allCards.Add(CreatePreset("3^~8~", 24, NumberCardData.LogicalType.Power,
            new NumberComponent { value = 3, isIncremental = false }, 
            new NumberComponent { isDice = true, diceSides = 8 }
            ));

        allCards.Add(CreatePreset("{0}^5", 25, NumberCardData.LogicalType.Power,
            new NumberComponent { value = 0, isIncremental = true }, 
            new NumberComponent { value = 5 }
            ));

        allCards.Add(CreatePreset("4^{0}", 26, NumberCardData.LogicalType.Power,
            new NumberComponent { value = 4 }, 
            new NumberComponent { value = 0, isIncremental = true }
            ));   
        
        allCards.Add(CreatePreset("2^~20~", 27, NumberCardData.LogicalType.Power,
            new NumberComponent { value = 2 }, 
            new NumberComponent { isDice = true, diceSides = 20 }
            ));   
        
        allCards.Add(CreatePreset("{0}^6", 28, NumberCardData.LogicalType.Power,
            new NumberComponent { value = 0, isIncremental = true }, 
            new NumberComponent { value = 6 }
            ));   
        
        allCards.Add(CreatePreset("5^{0}", 29, NumberCardData.LogicalType.Power,
            new NumberComponent { value = 5 }, 
            new NumberComponent { value = 0, isIncremental = true }
            ));   
        
        allCards.Add(CreatePreset("3^~12~", 30, NumberCardData.LogicalType.Power,
            new NumberComponent { value = 3 }, 
            new NumberComponent { isDice = true, diceSides = 12 }
            ));   
        
        allCards.Add(CreatePreset("{0}^7", 31, NumberCardData.LogicalType.Power,
            new NumberComponent { value = 0, isIncremental = true }, 
            new NumberComponent { value = 7 }
            ));   
        
        allCards.Add(CreatePreset("6^{0}", 32, NumberCardData.LogicalType.Power,
            new NumberComponent { value = 6 }, 
            new NumberComponent { value = 0, isIncremental = true }
            ));

        allCards.Add(CreatePreset("7^{0}", 33, NumberCardData.LogicalType.Power,
            new NumberComponent { value = 7 }, 
            new NumberComponent { value = 0, isIncremental = true }
            ));
        
        allCards.Add(CreatePreset("{0}^8", 34, NumberCardData.LogicalType.Power,
            new NumberComponent { value = 0, isIncremental = true }, 
            new NumberComponent { value = 8 }
            ));
        
        allCards.Add(CreatePreset("8^{0}", 35, NumberCardData.LogicalType.Power,
            new NumberComponent { value = 8 }, 
            new NumberComponent { value = 0, isIncremental = true }
            ));
        
        allCards.Add(CreatePreset("~6~+~12~", 36, NumberCardData.LogicalType.Addition,
            new NumberComponent { isDice = true, diceSides = 6 }, 
            new NumberComponent { isDice = true, diceSides = 12 }
            ));
        
        allCards.Add(CreatePreset("{0}+{0}", 37, NumberCardData.LogicalType.Addition,
            new NumberComponent { value = 0, isIncremental = true }, 
            new NumberComponent { value = 0, isIncremental = true }
            ));
        
        allCards.Add(CreatePreset("~8~+~20~", 38, NumberCardData.LogicalType.Addition,
            new NumberComponent { isDice = true, diceSides = 8 }, 
            new NumberComponent { isDice = true, diceSides = 20 }
            ));

        allCards.Add(CreatePreset("{0}+~20~", 39, NumberCardData.LogicalType.Addition,
            new NumberComponent { value = 0, isIncremental = true }, 
            new NumberComponent { isDice = true, diceSides = 20 }
            ));

        allCards.Add(CreatePreset("{0}^~4~", 40, NumberCardData.LogicalType.Power,
            new NumberComponent { value = 0, isIncremental = true }, 
            new NumberComponent { isDice = true,diceSides = 4 }
            ));

        allCards.Add(CreatePreset("~4~^{0}", 41, NumberCardData.LogicalType.Power,
            new NumberComponent { isDice = true, diceSides = 4 }, 
            new NumberComponent { value = 0, isIncremental = true }
            ));  

        allCards.Add(CreatePreset("{0}^~6~", 42, NumberCardData.LogicalType.Power,
            new NumberComponent { value = 0, isIncremental = true }, 
            new NumberComponent { isDice = true,diceSides = 6 }
            ));

        allCards.Add(CreatePreset("~6~^{0}", 43, NumberCardData.LogicalType.Power,
            new NumberComponent { isDice = true, diceSides = 6 }, 
            new NumberComponent { value = 0, isIncremental = true }
            ));
        
        allCards.Add(CreatePreset("{0}^~8~", 44, NumberCardData.LogicalType.Power,
            new NumberComponent { value = 0, isIncremental = true }, 
            new NumberComponent { isDice = true,diceSides = 8 }
            ));
        
        allCards.Add(CreatePreset("~8~^{0}", 45, NumberCardData.LogicalType.Power,
            new NumberComponent { isDice = true, diceSides = 8 }, 
            new NumberComponent { value = 0, isIncremental = true }
            ));
        
        allCards.Add(CreatePreset("~20~*~20~", 46, NumberCardData.LogicalType.Multiplication,
            new NumberComponent { isDice = true, diceSides = 20 }, 
            new NumberComponent { isDice = true, diceSides = 20 }
            ));
        
        allCards.Add(CreatePreset("~12~*~20~", 47, NumberCardData.LogicalType.Multiplication,
            new NumberComponent { isDice = true, diceSides = 12 }, 
            new NumberComponent { isDice = true, diceSides = 20 }
            ));
        
        allCards.Add(CreatePreset("~8~*~8~", 48, NumberCardData.LogicalType.Multiplication,
            new NumberComponent { isDice = true, diceSides = 8 }, 
            new NumberComponent { isDice = true, diceSides = 8 }
            ));
        
        allCards.Add(CreatePreset("~8~*~12~", 49, NumberCardData.LogicalType.Multiplication,
            new NumberComponent { isDice = true, diceSides = 8 }, 
            new NumberComponent { isDice = true, diceSides = 12 }
            ));
        
        allCards.Add(CreatePreset("~4~^~6~",  50, NumberCardData.LogicalType.Power,
            new NumberComponent { isDice = true, diceSides = 4 }, 
            new NumberComponent { isDice = true, diceSides = 6 }
            ));

        allCards.Add(CreatePreset("~4~^~8~",  51, NumberCardData.LogicalType.Power,
            new NumberComponent { isDice = true, diceSides = 4 }, 
            new NumberComponent { isDice = true, diceSides = 8 }
            ));
        
        allCards.Add(CreatePreset("~4~^~12~",  52, NumberCardData.LogicalType.Power,
            new NumberComponent { isDice = true, diceSides = 4 }, 
            new NumberComponent { isDice = true, diceSides = 12 }
            ));
        
        allCards.Add(CreatePreset("~4~^~20~",  53, NumberCardData.LogicalType.Power,
            new NumberComponent { isDice = true, diceSides = 4 }, 
            new NumberComponent { isDice = true, diceSides = 20 }
            ));
        
        allCards.Add(CreatePreset("~6~^~4~",  54, NumberCardData.LogicalType.Power,
            new NumberComponent { isDice = true, diceSides = 6 }, 
            new NumberComponent { isDice = true, diceSides = 4 }
            ));

        allCards.Add(CreatePreset("~6~^~6~",  55, NumberCardData.LogicalType.Power,
            new NumberComponent { isDice = true, diceSides = 6 }, 
            new NumberComponent { isDice = true, diceSides = 6 }
            ));
        
        allCards.Add(CreatePreset("~6~^~8~",  56, NumberCardData.LogicalType.Power,
            new NumberComponent { isDice = true, diceSides = 6 }, 
            new NumberComponent { isDice = true, diceSides = 8 }
            ));
        
        allCards.Add(CreatePreset("~6~^~12~",  57, NumberCardData.LogicalType.Power,
            new NumberComponent { isDice = true, diceSides = 6 }, 
            new NumberComponent { isDice = true, diceSides = 12 }
            ));
        
        allCards.Add(CreatePreset("~8~^~4~",  58, NumberCardData.LogicalType.Power,
            new NumberComponent { isDice = true, diceSides = 8 }, 
            new NumberComponent { isDice = true, diceSides = 4 }
            ));
        
        allCards.Add(CreatePreset("~8~^~6~",  59, NumberCardData.LogicalType.Power,
            new NumberComponent { isDice = true, diceSides = 8 }, 
            new NumberComponent { isDice = true, diceSides = 6 }
            ));
        
        allCards.Add(CreatePreset("~8~^~8~",  60, NumberCardData.LogicalType.Power,
            new NumberComponent { isDice = true, diceSides = 8 }, 
            new NumberComponent { isDice = true, diceSides = 8 }
            ));
        
        allCards.Add(CreatePreset("~12~^~4~",  61, NumberCardData.LogicalType.Power,
            new NumberComponent { isDice = true, diceSides = 12 }, 
            new NumberComponent { isDice = true, diceSides = 4 }
            ));
        
        allCards.Add(CreatePreset("~12~^~6~",  62, NumberCardData.LogicalType.Power,
            new NumberComponent { isDice = true, diceSides = 12 }, 
            new NumberComponent { isDice = true, diceSides = 6 }
            ));
        
        allCards.Add(CreatePreset("~12~^~8~",  63, NumberCardData.LogicalType.Power,
            new NumberComponent { isDice = true, diceSides = 12 }, 
            new NumberComponent { isDice = true, diceSides = 8 }
            ));
        
        allCards.Add(CreatePreset("~20~^~4~",  64, NumberCardData.LogicalType.Power,
            new NumberComponent { isDice = true, diceSides = 20 }, 
            new NumberComponent { isDice = true, diceSides = 4 }
            ));   
        
        allCards.Add(CreatePreset("~20~^~6~",  65, NumberCardData.LogicalType.Power,
            new NumberComponent { isDice = true, diceSides = 20 }, 
            new NumberComponent { isDice = true, diceSides = 6 }
            ));
        
        allCards.Add(CreatePreset("10000", 66, NumberCardData.LogicalType.Normal,
            new NumberComponent { value = 10000 }, null));

        allCards.Add(CreatePreset("{666}", 67, NumberCardData.LogicalType.Normal,
            new NumberComponent { value = 666, isIncremental = true }, null));

        allCards.Add(CreatePreset("1000", 68, NumberCardData.LogicalType.Normal,
            new NumberComponent { value = 1000 }, null));

        // 这里可以继续添加更多预设卡牌d

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
