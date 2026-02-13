using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 公式卡库 - 存储所有公式卡数据
/// 类似于 NumberCardLibrary
/// </summary>
[CreateAssetMenu(fileName = "FormulaCardLibrary", menuName = "Cards/FormulaCardLibrary")]
public class FormulaCardLibrary : ScriptableObject
{
    [Header("所有公式卡")]
    public List<FormulaCardData> allCards = new List<FormulaCardData>();

    /// <summary>
    /// 初始化默认公式卡（可选）
    /// </summary>
    [ContextMenu("Initialize Default Formula Cards")]
    public void InitializeDefaultCards()
    {
        allCards.Clear();

        // 使用 FormulaCardData 中定义的默认公式卡
        // 注意：这里需要手动创建 ScriptableObject 实例

        allCards.Add(CreateCard("#*#*#", 3, 0, 1000));
        allCards.Add(CreateCard("(#+#)*#*#", 4, 1, 2000));
        allCards.Add(CreateCard("#*#*#*#", 4, 2, 10000));
        allCards.Add(CreateCard("(#+#)*(#+#)", 4, 3, 400));
        allCards.Add(CreateCard("(#+#+#)*#", 4, 4, 100));
        allCards.Add(CreateCard("(#+#+#+#)*#", 5, 5, 400));
        allCards.Add(CreateCard("(#+#+#)*(#+#)", 5, 6, 600));
        allCards.Add(CreateCard("(#+#+#)*#*#", 5, 7, 3000));
        allCards.Add(CreateCard("(#+#)*(#+#)*#", 5, 8, 4000));
        allCards.Add(CreateCard("(#+#)*#*#*#", 5, 9, 20000));
        allCards.Add(CreateCard("#*#*#*#*#", 5, 10, 100000));
        allCards.Add(CreateCard("(#+#+#+#+#)*#", 6, 11, 500));
        allCards.Add(CreateCard("(#+#+#+#)*(#+#)", 6, 12, 800));
        allCards.Add(CreateCard("(#+#+#)*(#+#+#)", 6, 13, 900));
        allCards.Add(CreateCard("(#+#+#+#)*#*#", 6, 14, 4000));
        allCards.Add(CreateCard("(#+#+#)*(#+#)*#", 6, 15, 6000));
        allCards.Add(CreateCard("(#+#)*(#+#)*(#+#)", 6, 16, 8000));
        allCards.Add(CreateCard("(#+#+#)*#*#*#", 6, 17, 30000));
        allCards.Add(CreateCard("(#+#)*(#+#)*#*#", 6, 18, 40000));
        allCards.Add(CreateCard("(#+#)*#*#*#*#", 6, 19, 200000));
        allCards.Add(CreateCard("#*#*#*#*#*#", 6, 20, 1000000));

        Debug.Log($"✅ 成功初始化 {allCards.Count} 张公式卡！");
    }

    /// <summary>
    /// 创建单张公式卡（运行时创建，不会保存为资源文件）
    /// </summary>
    FormulaCardData CreateCard(string pattern, int requiredCount, int id, int price)
    {
        FormulaCardData card = ScriptableObject.CreateInstance<FormulaCardData>();
        card.Pattern = pattern;
        card.Name = pattern;
        card.RequiredCount = requiredCount;
        card.FormulaCardId = id;
        card.CardPrice = price;
        return card;
    }

    /// <summary>
    /// 根据ID获取公式卡
    /// </summary>
    public FormulaCardData GetCardById(int id)
    {
        return allCards.Find(card => card.FormulaCardId == id);
    }

    /// <summary>
    /// 根据所需数量筛选公式卡
    /// </summary>
    public List<FormulaCardData> GetCardsByRequiredCount(int count)
    {
        return allCards.FindAll(card => card.RequiredCount == count);
    }

    /// <summary>
    /// 随机获取一张公式卡
    /// </summary>
    public FormulaCardData GetRandomCard()
    {
        if (allCards.Count == 0)
        {
            Debug.LogError("公式卡库为空！");
            return null;
        }

        int randomIndex = Random.Range(0, allCards.Count);
        return allCards[randomIndex];
    }
}
