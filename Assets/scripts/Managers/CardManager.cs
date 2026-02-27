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
    //非常典型的 C# 单例模式（Singleton Pattern） 实现的一部分，用于创建一个全局唯一、易于访问的 GameManager 实例
    public static CardManager Instance { get; private set; }

    [Header("卡牌库")]
    public List<NumberCardData> numberCardDeck = new List<NumberCardData>();//数字卡牌库
    public List<FormulaCardData> formulaCardDeck = new List<FormulaCardData>();//填空卡牌库
    public Transform handCardParent;///建立手牌的父对象,作为后续给卡牌排版的容器

    [Header("卡牌库引用 - 新方式")]
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
    void SyncDeckFromInventory()
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

        // 先执行抽卡！
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
        Debug.Log("已清空上一回合的选择卡牌");

        foreach (Transform child in CardContent)
        {
            Destroy(child.gameObject);
        }
    }


    void DrawNumberCards(int count)
    {
        // 关键修复2：从库存中获取实例，而不是创建新实例
        var inventoryInstances = PlayerCardInventory.Instance.GetAllNumberCards();

        // 创建临时池（使用库存中的实例）
        List<NumberCardInstance> tempPool = new List<NumberCardInstance>(inventoryInstances);

        Debug.Log($"正在抽取数字卡牌，库存中共有 {tempPool.Count} 张卡");

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
        Debug.Log("数字卡牌抽取完成");
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

        Debug.Log($"========== 开始计算 ==========");
        Debug.Log($"公式：{currentFormulaCard.Pattern}");
        for (int i = 0; i < selectedNumberCards.Count; i++)
        {
            var card = selectedNumberCards[i];
            Debug.Log($"  位置 {i}: {card.cardData.cardName} → 输出值: {card.GetOutPutValue()}");
        }

        BigInteger result = FormulaCalculator.Calculate(currentFormulaCard, selectedNumberCards);

        Debug.Log($" 计算结果：{result}");
        Debug.Log($"==============================");

        //结算后更新递增卡的值
        UpdateIncrementalCards();

        return result;
    }
    /// <summary>
    /// 更新递增卡的值（结算后调用）
    /// </summary>
    void UpdateIncrementalCards()
    {
        foreach (var card in selectedNumberCards)
        {
            // 更新 Part A 的递增值
            if (card.cardData.partA.isIncremental)
            {
                card.currentA++;
                Debug.Log($"递增卡更新：{card.cardData.cardName} Part A: {card.currentA - 1} → {card.currentA}");
            }

            // 更新 Part B 的递增值
            if (card.cardData.partB != null && card.cardData.partB.isIncremental)
            {
                card.currentB++;
                Debug.Log($"递增卡更新：{card.cardData.cardName} Part B: {card.currentB - 1} → {card.currentB}");
            }

        }
    }
    public void AddNumberCardToFormula(NumberCardInstance card)
    {
        if (currentFormulaCard == null)
        {
            Debug.LogWarning("还没有公式卡");
            return;
        }

        if (selectedNumberCards.Count >= currentFormulaCard.RequiredCount)
        {
            Debug.LogWarning("数字卡数量已满");
            return;
        }

        selectedNumberCards.Add(card);
        Debug.Log($"加入数字卡：{card.GetOutPutValue()}");
    }
    /// <summary>
    /// 从公式中移除数字卡（支持退回）
    /// </summary>
    public void RemoveNumberCardFromFormula(NumberCardInstance card)
    {
        if (selectedNumberCards.Remove(card))
        {
            Debug.Log($"移除卡牌: {card.cardData.cardName}");
        }
        else
        {
            Debug.LogWarning($"尝试移除不存在的卡牌: {card.cardData.cardName}");
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

