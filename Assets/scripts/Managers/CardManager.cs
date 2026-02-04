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
    public static CardManager Instance { get; private set; }

    [Header("卡牌库")]
    public List<NumberCardData> numberCardDeck = new List<NumberCardData>();//数字卡牌库
    public List<FormulaCardData> formulaCardDeck = new List<FormulaCardData>();//填空卡牌库
    public Transform handCardParent;///建立手牌的父对象,作为后续给卡牌排版的容器

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

        foreach (var card in starterNumberCards)
        {
            // 如果 NumberCardData 是 ScriptableObject，就不需要 Instantiate
            // 如果它是普通 class，需要 new
            PlayerCardInventory.Instance.AddNumberCard(card);
            Debug.Log(PlayerCardInventory.Instance.numberCards.Count);
        }

        foreach (var card in starterFormulaCards)
        {
            PlayerCardInventory.Instance.AddFormulaCard(card);
        }
        numberCardDeck = new List<NumberCardData>(starterNumberCards);
        formulaCardDeck = new List<FormulaCardData>(starterFormulaCards);

        Debug.Log($"同步完成，当前牌堆公式卡数量: {formulaCardDeck.Count}");
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

        numberCardDeck = new List<NumberCardData>();
        foreach (var instance in PlayerCardInventory.Instance.GetAllNumberCards())
        {
            if (instance != null && instance.cardData != null)
            {
                numberCardDeck.Add(instance.cardData);
            }
        }
        formulaCardDeck = PlayerCardInventory.Instance.GetAllFormulaCards();

        DrawFormulaCards();
        DrawNumberCards(currentFormulaCard.RequiredCount);
        // 通知UI管理器更新显示
        UIManager.Instance.ShowHandCards(currentNumberCards);
        UIManager.Instance.ShowFormulaCard(currentFormulaCard);

    }
    void ClearHand()// 清空当前手牌
    {
        currentNumberCards.Clear();
        currentFormulaCard = null;

        foreach (Transform child in CardContent)
        {
            Destroy(child.gameObject);
        }
    }


    void DrawNumberCards(int count)
    {
        // 用临时池，避免一回合内重复抽
        List<NumberCardData> tempDeck = new List<NumberCardData>(numberCardDeck);
        Debug.Log("正在抽取数字卡牌");
        for (int i = 0; i < count; i++)
        {
            if (tempDeck.Count == 0) break;

            int randomIndex = Random.Range(0, tempDeck.Count);
            NumberCardData selectedData = tempDeck[randomIndex];
            tempDeck.RemoveAt(randomIndex);// 从临时池中移除已抽取的卡牌

            // 创建卡牌实例
            NumberCardInstance instance = new NumberCardInstance(selectedData);

            // 抽中,掷骰
            instance.OnDrawn();

            currentNumberCards.Add(instance);

        }
        Debug.Log("数字卡牌抽取完成");
    }
    public void DrawFormulaCards()
    {   
        Debug.Log("正在抽取填空卡牌");
        // 抽取填空卡牌
        if (formulaCardDeck.Count == 0)
        {
            Debug.LogWarning("公式卡库为空！");
            return;
        }

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

        BigInteger result =
            FormulaCalculator.Calculate(
                currentFormulaCard,
                selectedNumberCards);

        Debug.Log($"公式结果：{result}");
        return result;
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

