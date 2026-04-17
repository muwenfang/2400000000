using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 卡牌事件（抽卡，展示，储存玩家卡牌库）
/// </summary>
public class CardManager : MonoBehaviour
{
    //单例模式实现的一部分，确保全局只有一个 CardManager 实例，并提供全局访问点
    public static CardManager Instance { get; private set; }

    [Header("卡牌库")]
    public List<NumberCardData> numberCardDeck = new List<NumberCardData>();//数字卡牌库
    public List<FormulaCardData> formulaCardDeck = new List<FormulaCardData>();//填空卡牌库
    public Transform handCardParent;///建立手牌的父对象,作为后续给卡牌排版的容器

    [Header("卡牌库引用")]
    [Tooltip("数字卡库 - 拖入 NumberCardLibrary 资源")]
    public NumberCardLibrary numberCardLibrary;

    [Tooltip("公式卡库 - 拖入 FormulaCardLibrary 资源")]
    public FormulaCardLibrary formulaCardLibrary;

    // 添加 CardContent 字段并初始化为 handCardParent
    private Transform CardContent => handCardParent;

    [Header("起始牌组")]
    public List<NumberCardData> starterNumberCards = new();
    public List<FormulaCardData> starterFormulaCards = new();

    [Header("当前手牌")]
    public List<NumberCardInstance> currentNumberCards = new();
    public FormulaCardData currentFormulaCard;

    [Header("当前填入公式的数字卡")]
    public List<NumberCardInstance> selectedNumberCards = new();

    public void InitializeStarterDeck()
    {   
        Debug.Log("初始化玩家起始卡组");
        // 初始化玩家的起始卡组
        PlayerCardInventory.Instance.ClearAll();
        // 同步到当前牌堆
        SyncDeckFromInventory();

        foreach (var card in starterNumberCards)
        {
            // 如果 NumberCardData 是 ScriptableObject，就不需要 Instantiate
            // 如果它是普通 class，需要 new
            PlayerCardInventory.Instance.AddNumberCard(card);
        }

        foreach (var card in starterFormulaCards)
        {
            PlayerCardInventory.Instance.AddFormulaCard(card);
        }
        numberCardDeck = new List<NumberCardData>(starterNumberCards);
        formulaCardDeck = new List<FormulaCardData>(starterFormulaCards);

        Debug.Log($"同步完成，当前牌堆公式卡数量: {formulaCardDeck.Count}");
    }

    /// <summary>
    /// 从玩家库存同步当前牌堆
    /// </summary>
    public void SyncDeckFromInventory()
    {
        numberCardDeck.Clear();
        foreach (var instance in PlayerCardInventory.Instance.GetAllNumberCards())
        {
            if (instance != null && instance.cardData != null)
            {
                numberCardDeck.Add(instance.cardData);
            }
        }

        formulaCardDeck = new List<FormulaCardData>(PlayerCardInventory.Instance.GetAllFormulaCards());
    }
    public void DrawCardsForTurn()// 抽取当前回合手牌
    {
        ClearHand();

        if (numberCardDeck == null || numberCardDeck.Count == 0)
        {
            Debug.LogError("数字卡库为空！");
            return;
        }
        if (formulaCardDeck == null || formulaCardDeck.Count == 0)
        {
            Debug.LogWarning("公式卡库为空！无法抽卡。");
            return;
        }  

        // 先执行抽卡
        DrawFormulaCards();

        if (currentFormulaCard == null)
        {
            Debug.LogError("严重错误：本回合未能抽到公式卡！停止发牌。");
        }
        DrawNumberCards(currentFormulaCard.RequiredCount);

        // 通知UI管理器更新显示
        UIManager.Instance.ShowHandCards(currentNumberCards);
        UIManager.Instance.ShowFormulaCard(currentFormulaCard);

    }
    void ClearHand()// 清空当前手牌
    {
        currentNumberCards.Clear();
        currentFormulaCard = null;

        // 清空上一回合填入的卡牌
        selectedNumberCards.Clear();

        foreach (Transform child in CardContent)
        {
            Destroy(child.gameObject);
        }
    }


    void DrawNumberCards(int count)
    {
        //从库存中获取实例
        var inventoryInstances = PlayerCardInventory.Instance.GetAllNumberCards();

        // 创建临时池（使用库存中的实例）
        List<NumberCardInstance> tempPool = new List<NumberCardInstance>(inventoryInstances);

        // 经验主义祝福
        if (BlessingManager.Instance.GetBlessingTypeCount(BlessingData.BlessingType.Empiricism) == 1)
        {
            if (tempPool.Count == 0)
            {
                Debug.LogWarning($"卡牌不足！只抽到 0 张");
                return;
            }
            // 抽取上一回合最大值的卡牌
            NumberCardInstance selectedInstance = GameManager.Instance.lastRoundMaxCard;

            // 抽中时处理骰子和递增
            selectedInstance.OnDrawn();
            currentNumberCards.Add(selectedInstance);
            Debug.Log($"抽到卡牌: {selectedInstance.cardData.cardName}," +
                $" 当前值: A={selectedInstance.currentA}, B={selectedInstance.currentB}");

            for (int i = 1; i < count; i++)
            {
                if (tempPool.Count == 0)
                {
                    Debug.LogWarning($"卡牌不足！只抽到 {i} 张");
                    break;
                }

                int randomIndex = Random.Range(0, tempPool.Count);
                selectedInstance = tempPool[randomIndex];
                tempPool.RemoveAt(randomIndex);

                //抽中时处理骰子和递增
                selectedInstance.OnDrawn();

                currentNumberCards.Add(selectedInstance);

                Debug.Log($"抽到卡牌: {selectedInstance.cardData.cardName}, 当前值: A={selectedInstance.currentA}, B={selectedInstance.currentB}");
            }
        }

        //无经验主义祝福逻辑
        else
        {
            for (int i = 0; i < count; i++)
            {
                if (tempPool.Count == 0)
                {
                    Debug.LogWarning($"卡牌不足！只抽到 {i} 张");
                    break;
                }

                int randomIndex = Random.Range(0, tempPool.Count);
                NumberCardInstance selectedInstance = tempPool[randomIndex];
                tempPool.RemoveAt(randomIndex);

                //抽中时处理骰子和递增
                selectedInstance.OnDrawn();

                currentNumberCards.Add(selectedInstance);

                Debug.Log($"抽到卡牌: {selectedInstance.cardData.cardName}, 当前值: A={selectedInstance.currentA}, B={selectedInstance.currentB}");
            }
        }
    }
    public void DrawFormulaCards()
    {   
        if (formulaCardDeck == null || formulaCardDeck.Count == 0)
        {
            Debug.LogError("公式卡库为空，无法抽取公式卡！");
            return;
        }
        Debug.Log("正在抽取填空卡牌");

        int index = Random.Range(0, formulaCardDeck.Count);
        currentFormulaCard = formulaCardDeck[index];

        Debug.Log("抽到公式卡：" + currentFormulaCard.Name);

    }

    public NumberCardInstance PrepareCardsForCalculation()
    {
        //实现保存上一回合最大值的功能，供经验主义祝福使用
        BigInteger maxValue = 0;
        NumberCardInstance lastRoundMaxCard = null;

        if (selectedNumberCards == null) 
        {return null;}


        for (int i = 0; i < selectedNumberCards.Count; i++)
        {
            if (selectedNumberCards[i] != null)
            {
                // 计算骰子和递增后数值
                selectedNumberCards[i].PrepareForCalculation();

                // 经验主义祝福：记录本回合填入的数字卡中数值最大的卡牌
                if (selectedNumberCards[i].GetOutPutValue() > maxValue)
                {
                maxValue = selectedNumberCards[i].GetOutPutValue();
                lastRoundMaxCard = selectedNumberCards[i];
                }
             }
         }
       
        return lastRoundMaxCard;
    }
    
    public BigInteger CalculateResult()
    {
        if (currentFormulaCard == null)
        {
            Debug.LogWarning("没有公式卡");
            return 0;
        }

        if (selectedNumberCards.Count != currentFormulaCard.RequiredCount)
        {
            Debug.LogWarning("数字卡数量不足");
            return 0;
        }

        Debug.Log($"公式：{currentFormulaCard.Pattern}");

        BigInteger result = FormulaCalculator.Calculate(currentFormulaCard, selectedNumberCards);

        return result;
    }
    // 按槽位索引插入（保证顺序）
    public void AddNumberCardToFormula(NumberCardInstance card, int index)
    {
        if (currentFormulaCard == null)
        {
            Debug.LogWarning("还没有公式卡");
            return;
        }

        int required = currentFormulaCard.RequiredCount;
        if (index < 0 || index >= required)
        {
            Debug.LogError($"无效槽位索引 {index}（范围 0..{required - 1}）");
            return;
        }

        if (selectedNumberCards == null) selectedNumberCards = new List<NumberCardInstance>();

        // 扩展到 required 长度，使用 null 占位
        while (selectedNumberCards.Count < required)
            selectedNumberCards.Add(null);

        // 若卡牌已经在其他位置，先清除（置 null）
        for (int i = 0; i < selectedNumberCards.Count; i++)
        {
            if (selectedNumberCards[i] == card)
            {
                selectedNumberCards[i] = null;
            }
        }

        selectedNumberCards[index] = card;
    }

    /// <summary>
    /// 从公式中移除数字卡（支持退回）
    /// </summary>
    public void RemoveNumberCardFromFormula(NumberCardInstance card)
    {
        if (selectedNumberCards == null) return;
        bool found = false;
        for (int i = 0; i < selectedNumberCards.Count; i++)
        {
            if (selectedNumberCards[i] == card)
            {
                selectedNumberCards[i] = null; // 置 null 保持索引
                found = true;
                Debug.Log($"移除卡牌（置空）: {card.cardData.cardName} at {i}");
                // 不 break，防止重复实例（一般只会有一个）
            }
        }
        if (!found)
            Debug.LogWarning($"尝试移除不存在的卡牌: {card.cardData.cardName}");
    }
    // 按索引移除（FormulaSlot 点击或 ClearSlot 使用）
    public void RemoveNumberCardFromFormulaAtIndex(int index)
    {
        if (selectedNumberCards == null) return;
        if (index < 0 || index >= selectedNumberCards.Count) return;
        if (selectedNumberCards[index] != null)
        {
            Debug.Log($"按索引移除卡牌 at {index}: {selectedNumberCards[index].cardData.cardName}");
            selectedNumberCards[index] = null;
        }
    }
    /// <summary>
    /// 检查卡牌是否在公式中
    /// </summary>
    public bool IsCardInFormula(NumberCardInstance card)
    {
        return selectedNumberCards.Contains(card);
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
}

