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
    [Header("卡牌库")]
    public List<NumberCardData> numberCardDeck = new List<NumberCardData>();//数字卡牌库
    public List<FormulaCardData> formulaCardDeck = new List<FormulaCardData>();//填空卡牌库
    public Transform handCardParent;///建立手牌的父对象,作为后续给卡牌排版的容器

    [Header("起始牌组")]
    public List<NumberCardData> starterNumberCards = new();
    public List<FormulaCardData> starterFormulaCards = new();

    [Header("当前手牌")]
    public List<NumberCardInstance> currentNumberCards = new();
    public FormulaCardData currentFormulaCard;

    public GameObject CardUIPrefab;
    public Transform CardContent;
    public void InitializeStarterDeck()
    {   
        Debug.Log("初始化玩家起始卡组");
        // 初始化玩家的起始卡组
        PlayerCardInventory.Instance.ClearAll();

        foreach (var card in starterNumberCards)
        {
            var runtimeCard = Instantiate(card);
            PlayerCardInventory.Instance.AddNumberCard(runtimeCard);
            Debug.Log(PlayerCardInventory.Instance.numberCards.Count);
        }

        foreach (var card in starterFormulaCards)
        {
            PlayerCardInventory.Instance.AddFormulaCard(card);
        }
    }

    public void DrawCardsForTurn()// 抽取当前回合手牌
    {
        ClearHand();

        numberCardDeck = PlayerCardInventory.Instance.GetAllNumberCards();
        formulaCardDeck = PlayerCardInventory.Instance.GetAllFormulaCards();

        DrawFormulaCards();
        DrawNumberCards(currentFormulaCard.RequiredCount);
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

            // 放入手牌
            currentNumberCards.Add(instance);

            //  UI
            CreateCardUI(instance);
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

        CreateFormulaCardUI(currentFormulaCard);
        Debug.Log("填空卡牌抽取完成");
    }
    void CreateCardUI(NumberCardInstance instance)
    {
        GameObject cardUI = Instantiate(CardUIPrefab, CardContent);

        CardUI ui = cardUI.GetComponent<CardUI>();
        if (ui != null)
        {
            ui.BindNumberCard(instance);
        }
    }
    void CreateFormulaCardUI(FormulaCardData formula)
    {
        GameObject cardUI = Instantiate(CardUIPrefab, CardContent);

        CardUI ui = cardUI.GetComponent<CardUI>();
        if (ui != null)
        {
            ui.BindFormulaCard(formula);
        }
    }
    public BigInteger CalculateResult(List<NumberCardData> numberCards)
    {   
        Debug.Log("正在计算填空卡牌结果");
        
        BigInteger result = 0;
        //计算逻辑
        //【to do】
        Debug.Log($"点数为{result}");
        return result;
    }
}

public class PlayerCardInventory : MonoBehaviour// 玩家卡牌库存
{
    public static PlayerCardInventory Instance;

    [Header("玩家拥有的数字卡")]
    public List<NumberCardData> numberCards = new();

    [Header("玩家拥有的公式卡")]
    public List<FormulaCardData> formulaCards = new();

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
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

        numberCards.AddRange(starterNumbers);
        formulaCards.AddRange(starterFormulas);
    }

    // =========================
    // 添加卡牌
    // =========================

    public void AddNumberCard(NumberCardData card)
    {
        numberCards.Add(card);
        Debug.Log("获得数字卡：" + card.name);
    }

    public void AddFormulaCard(FormulaCardData card)
    {
        formulaCards.Add(card);
        Debug.Log("获得公式卡：" + card.Name);
    }

    // =========================
    // 给抽卡系统使用
    // =========================

    public List<NumberCardData> GetAllNumberCards()
    {
        return numberCards;
    }

    public List<FormulaCardData> GetAllFormulaCards()
    {
        return formulaCards;
    }
}
