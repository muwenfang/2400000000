using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PlayerCardInventory : MonoBehaviour// 玩家卡牌库存
{
    public static PlayerCardInventory Instance;
    public NumberCardLibrary numberCardLibrary;

    [Header("玩家拥有的数字卡")]
    public List<NumberCardInstance> numberCards = new();

    [Header("玩家拥有的公式卡")]
    public List<FormulaCardData> formulaCards = new();

    // 删卡约束常数
    [Header("删卡约束")]
    [Tooltip("数字卡最少保留数量")]
    public int minNumberCardCount = 6;
    [Tooltip("公式卡最少保留数量")]
    public int minFormulaCardCount = 1;

    //倍率逻辑:获取玩家拥有的公式卡数量，作为每回合的基础倍率
    public int GetFormulaCardCount()
    {
        return formulaCards.Count;
    }

    public int GetNumberCardCount()
    {
        return numberCards.Count;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    // =========================
    // 初始化
    // =========================

    public void ClearAll()
    {
        numberCards.Clear();
        formulaCards.Clear();
    }

    public void InitStarterDeck(List<NumberCardData> starterNumbers,
                                List<FormulaCardData> starterFormulas)
    {
        ClearAll();

        // 将 NumberCardData 转换为 NumberCardInstance 后再添加
        foreach (var card in starterNumbers)
        {
            numberCards.Add(new NumberCardInstance(card));
        }
        formulaCards.AddRange(starterFormulas);
    }

    // =========================
    // 添加卡牌
    // =========================

    public void AddNumberCard(NumberCardData card)
    {
        NumberCardInstance instance = new NumberCardInstance(card);
        numberCards.Add(instance);
        Debug.Log("获得数字卡：" + card.name);
    }

    public void AddFormulaCard(FormulaCardData card)
    {
        formulaCards.Add(card);
        Debug.Log("获得公式卡：" + card.Name);
    }
    // =========================
    // 删除卡牌
    // =========================
    public bool RemoveNumberCard(NumberCardInstance card)
    {
        if (numberCards.Remove(card))
        {
            Debug.Log($"[PlayerCardInventory] 成功删除数字卡：{card.cardData.name}（剩余 {numberCards.Count} 张）");
            return true;
        }
        else
        {
            Debug.LogWarning($"[PlayerCardInventory] 删除失败：卡牌不在库存中");
            return false;
        }
    }
    /// <summary>
    /// 尝试删除公式卡
    /// 添加约束检查，确保至少保留minFormulaCardCount张卡牌
    /// </summary>
    public bool RemoveFormulaCard(FormulaCardData card)
    {
        if (formulaCards.Remove(card))
        {
            Debug.Log($"[PlayerCardInventory] 成功删除公式卡：{card.Name}（剩余 {formulaCards.Count} 张）");
            return true;
        }
        else
        {
            Debug.LogWarning($"[PlayerCardInventory] 删除失败：卡牌不在库存中");
            return false;
        }
    }

    /// <summary>
    /// 检查是否可以删除数字卡
    /// </summary>
    public bool CanRemoveNumberCard()
    {
        return numberCards.Count > minNumberCardCount;
    }

    /// <summary>
    /// 检查是否可以删除公式卡
    /// </summary>
    public bool CanRemoveFormulaCard()
    {
        return formulaCards.Count > minFormulaCardCount;
    }
    // =========================
    // 大小卡牌包祝福效果：添加随机n张数字卡
    // =========================
    public void AddRandomNumberCards(int count)
    {
        for (int i = 0; i < count; i++)
        {
            int randomIndex = Random.Range(0, numberCardLibrary.allCards.Count);
            NumberCardData randomCard = numberCardLibrary.allCards[randomIndex];
            AddNumberCard(randomCard);
        }
    }   

    // =========================
    // 给抽卡系统使用
    // =========================

    public List<NumberCardInstance> GetAllNumberCards()
    {
        return numberCards;
    }

    public List<FormulaCardData> GetAllFormulaCards()
    {
        return formulaCards;
    }
}

