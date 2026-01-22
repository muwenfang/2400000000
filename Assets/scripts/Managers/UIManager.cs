using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("UI 面板引用")]
    public GameObject startMenuPanel; // 在 Inspector 中拖入主菜单面板
    public GameObject gameUIPanel; // 游戏内UI面板
    public GameObject gameOverPanel; // 游戏结束面板
    public GameObject shopPanel; // 商店面板
    public GameObject confirmPanel;// 确认面板

    [Header("UI组件")]
    public Text pointsText;//显示当前点数
    public Text roundText;//显示当前回合
    public Transform handArea;//手牌区域
    public Transform formulaArea;//填空卡区域

    public GameObject numberCardPrefab;
    public GameObject formulaCardPrefab;

    void Awake()
    {
        Instance = this;
    }
    public void ShowStartMenu() 
    {
        if (startMenuPanel != null)
        {
            startMenuPanel.SetActive(true);
            Debug.Log("显示主菜单");
        }
    }
    public void HideStartMenu() 
    {
        if (startMenuPanel != null)
        {
            startMenuPanel.SetActive(false);
        }
    }
    public void UpdatePointsDisplay(BigInteger points)
    {
        pointsText.text = $"点数: {points}";
    }

    public void UpdateRoundDisplay(int round)
    {
        roundText.text = $"回合: {round}";
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


}

public class CardUI : MonoBehaviour// 卡牌UI显示脚本
{
    public Text titleText;
    public Text contentText;

    public void BindNumberCard(NumberCardInstance card)
    {
        titleText.text = "数字卡";
        contentText.text = $"{card.currentA} , {card.currentB}";
    }

    public void BindFormulaCard(FormulaCardData card)
    {
        titleText.text = "公式卡";
        contentText.text = card.Pattern;
    }
}


public class ShopCardUI : MonoBehaviour
{
    NumberCardData numberData;
    FormulaCardData formulaData;
    bool isNumber;

    public void BindNumberCard(NumberCardData data)// 绑定数字卡数据
    {
        numberData = data;
        isNumber = true;
        // 刷UI
    }

    public void BindFormulaCard(FormulaCardData data)// 绑定填空卡数据
    {
        formulaData = data;
        isNumber = false;
    }

    public void OnBuyClick()// 购买按钮点击事件
    {
        if (isNumber)
        {
            PlayerCardInventory.Instance.AddNumberCard(Instantiate(numberData));
        }
        else
        {
            PlayerCardInventory.Instance.AddFormulaCard(formulaData);
        }

        Destroy(gameObject); // 从商店移除
    }
}

