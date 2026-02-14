using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowMyCard : MonoBehaviour
{
    //[Header("UI 容器")]
    //public Transform numberCardContent; // 对应 UIManager 中的 myNumberCardPanel 内部的 Content
    //public Transform formulaCardContent; // 对应 UIManager 中的 myFormulaCardPanel 内部的 Content
    ////public Transform blessContent; // 对应 UIManager 中的 myBlessPanel 内部的 Content

    [Header("类型设置")]
    public bool showNumberCards = true; // 勾选则显示数字卡，不勾选显示公式卡
    public bool showFormulaCards = false; // 勾选则显示公式卡，不勾选显示数字卡
    public bool showBlessCards = false; // 勾选则显示祝福卡，不勾选不显示

    [Header("容器引用")]
    public Transform contentRoot; // ScrollView 的 Content

    [Header("显示设置")]
    public float cardScale = 1.0f; // 卡牌缩放比例，根据UI大小调整

    // 当面板被激活时自动刷新
    private void OnEnable()
    {
        RefreshAllCards();
    }

    public void RefreshAllCards()
    {
        // 1. 清理
        foreach (Transform child in contentRoot)
        {
            Destroy(child.gameObject);
        }

        // 2. 根据类型生成
        if (showNumberCards)
        {
            GenerateNumberCards();
        }
        if (showFormulaCards) 
        {
            GenerateFormulaCards();
        }
        //if (showBlessCards)
        //{
        //    GenerateBlessCards();
        //}
    }
    void GenerateNumberCards()
    {
        var deck = CardManager.Instance.numberCardDeck;
        foreach (var data in deck)
        {
            GameObject prefab = UIManager.Instance.numberCardLibrary.GetPrefab(data.layoutType);
            if (prefab != null)
            {
                GameObject go = Instantiate(prefab, contentRoot);
                // 确保缩放正确
                go.transform.localScale = Vector3.one;
                var view = go.GetComponent<NumberCardLayoutView>();
                if (view != null) view.Bind(data);
            }
        }

    }
    void GenerateFormulaCards()
    {
        var deck = CardManager.Instance.formulaCardDeck;
        var prefab = UIManager.Instance.formulaCardPrefab;
        foreach (var data in deck)
        {
            if (prefab != null)
            {
                GameObject go = Instantiate(prefab, contentRoot);
                go.transform.localScale = Vector3.one;
                var view = go.GetComponent<FormulaCardUI>();
                if (view != null) view.Bind(data);
            }
        }
    }
    //void ShowMyBless()
    //{


    //}
    private void ClearArea(Transform parent)
    {
        if (parent == null) return;
        foreach (Transform child in parent)
        {
            Destroy(child.gameObject);
        }
    }
   
}
