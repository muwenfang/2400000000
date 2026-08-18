using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


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

    // 脏标记机制：每次库存变化时自增，面板通过比较版本号判断是否需要重建UI
    public int InventoryVersion { get; private set; }

    // 库存变化事件：当卡牌增删时触发，订阅者可在回调中刷新UI
    public event System.Action OnInventoryChanged;

    /// <summary>
    /// 通知库存变化：自增版本号并触发事件
    /// 所有增删卡牌的方法末尾都应调用此方法
    /// </summary>
    private void NotifyInventoryChanged()
    {
        InventoryVersion++;
        OnInventoryChanged?.Invoke();
    }

    //倍率逻辑:获取玩家拥有的公式卡数量，作为每回合的基础倍率
    public int GetFormulaCardCount()
    {
        return formulaCards.Count;
    }

    public int GetNumberCardCount()
    {
        return numberCards.Count;
    }

    /// <summary>
    /// 统计玩家已拥有数字卡中骰子的总数量
    /// </summary>
    /// <returns>骰子总数量</returns>
    public int CountOwnedDiceTotalNumber()
    {
        int totalDiceCount = 0;

        // 空列表校验，避免空指针
        if (numberCards == null || numberCards.Count == 0)
        {
            Debug.LogWarning("[PlayerCardInventory] 玩家未拥有任何数字卡，骰子数量为0");
            return 0;
        }

        // 遍历所有已拥有的数字卡实例
        foreach (var cardInstance in numberCards)
        {
            // 空实例/空卡牌数据校验
            if (cardInstance == null || cardInstance.cardData == null)
            {
                Debug.LogWarning("[PlayerCardInventory] 检测到空的数字卡实例，跳过统计");
                continue;
            }

            // PartA是骰子 → 计数+1
            if (cardInstance.cardData.partA != null && cardInstance.cardData.partA.isDice)
            {
                totalDiceCount++;
            }

            // PartB是骰子 → 计数+1（双骰子卡牌会累计2次）
            if (cardInstance.cardData.partB != null && cardInstance.cardData.partB.isDice)
            {
                totalDiceCount++;
            }
        }

        Debug.Log($"[PlayerCardInventory] 玩家拥有的骰子总数：{totalDiceCount}");
        return totalDiceCount;
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
        NotifyInventoryChanged();
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
        NotifyInventoryChanged();
    }

    // =========================
    // 添加卡牌
    // =========================

    public void AddNumberCard(NumberCardData card)
    {
        if (card == null)
        {
            Debug.LogError("[PlayerCardInventory] 尝试添加空的数字卡数据，已跳过。");
            return;
        }

        // partA 缺失说明该卡牌资产未配置完整，跳过并给出明确提示，避免空引用
        if (card.partA == null)
        {
            Debug.LogError($"[PlayerCardInventory] 数字卡「{card.name}」缺少 partA（未配置数字/骰子组件），已跳过。请在 Inspector 中检查该卡牌资产。");
            return;
        }

        NumberCardInstance instance = new NumberCardInstance(card);
        numberCards.Add(instance);
        NotifyInventoryChanged();
    }

    public void AddFormulaCard(FormulaCardData card)
    {
        formulaCards.Add(card);
        NotifyInventoryChanged();
    }
    // =========================
    // 删除卡牌
    // =========================
    public bool RemoveNumberCard(NumberCardInstance card)
    {
        if (numberCards.Remove(card))
        {
            Debug.Log($"[PlayerCardInventory] 成功删除数字卡：{card.cardData.name}（剩余 {numberCards.Count} 张）");
            NotifyInventoryChanged();
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
            NotifyInventoryChanged();
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
            int randomIndex = UnityEngine.Random.Range(0, numberCardLibrary.allCards.Count);
            NumberCardData randomCard = numberCardLibrary.allCards[randomIndex];
            AddNumberCard(randomCard);
        }
        NotifyInventoryChanged();
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

