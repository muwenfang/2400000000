using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowMyCard : MonoBehaviour
{
    [Header("UI 容器")]
    public Transform numberCardContent; // 对应 UIManager 中的 myNumberCardPanel 内部的 Content
    public Transform formulaCardContent; // 对应 UIManager 中的 myFormulaCardPanel 内部的 Content
    //public Transform blessContent; // 对应 UIManager 中的 myBlessPanel 内部的 Content
    
    [Header("显示设置")]
    public float cardScale = 1.0f; // 卡牌缩放比例，根据UI大小调整

    // 当面板被激活时自动刷新
    private void OnEnable()
    {
        RefreshAllCards();
    }

    public void RefreshAllCards()
    {
        ShowMyNumberCards();
        ShowMyFormulaCards();
    }
    public void ShowMyFormulaCards()
    {
        // 1. 清空旧的 UI 物体
        ClearArea(numberCardContent);

        // 2. 从 CardManager 获取玩家当前的数字卡库
        List<NumberCardData> myCards = CardManager.Instance.numberCardDeck;

        foreach (var cardData in myCards)
        {
            // 3. 使用 UIManager 中的工厂获取对应的 Prefab
            GameObject prefab = UIManager.Instance.numberCardLibrary.GetPrefab(cardData.layoutType);

            if (prefab != null)
            {
                CreateAndBindNumberCard(prefab, cardData);
            }
            else
            {
                Debug.LogWarning($"找不到布局 {cardData.layoutType} 的 Prefab");
            }
        }

    }
    public void ShowMyNumberCards()
    {
        // 1. 清空旧的 UI 物体
        ClearArea(formulaCardContent);

        // 2. 从 CardManager 获取公式卡库
        List<FormulaCardData> myFormulas = CardManager.Instance.formulaCardDeck;

        if (myFormulas == null) return;

        // 获取公式卡 Prefab (通常在 UIManager 中配置)
        GameObject prefab = UIManager.Instance.formulaCardPrefab;

        if (prefab == null)
        {
            Debug.LogError("UIManager 未配置 FormulaCardPrefab！");
            return;
        }

        foreach (var data in myFormulas)
        {
            CreateAndBindFormulaCard(prefab, data);
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
    // --- 辅助方法 ---

    private void CreateAndBindNumberCard(GameObject prefab, NumberCardData data)
    {
        GameObject go = Instantiate(prefab, numberCardContent);
        go.transform.localScale = Vector3.one * cardScale;

        // 确保 UI 显示正确
        go.SetActive(true);

        // 获取视图接口进行绑定 (SingleNumberView 或 CompositeNumberView)
        var view = go.GetComponent<NumberCardLayoutView>();
        if (view != null)
        {
            view.Bind(data);
        }
        else
        {
            Debug.LogError($"Prefab {go.name} 缺少 NumberCardLayoutView 接口组件！");
        }

        // 如果你的卡牌有点击查看详情功能，可以在这里添加 Button 监听
    }

    private void CreateAndBindFormulaCard(GameObject prefab, FormulaCardData data)
    {
        GameObject go = Instantiate(prefab, formulaCardContent);
        go.transform.localScale = Vector3.one * cardScale;
        go.SetActive(true);

        var view = go.GetComponent<FormulaCardUI>();
        if (view != null)
        {
            view.Bind(data);
        }
        else
        {
            Debug.LogError("公式卡 Prefab 缺少 FormulaCardUI 组件！");
        }
    }
}
