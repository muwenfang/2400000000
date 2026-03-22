using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 删除卡牌槽位
/// </summary>
public class DeleteCardSlot : MonoBehaviour
{
    [Header("卡牌显示")]
    public Transform cardContentRoot;      // 卡牌显示的父容器

    [Header("选择按钮")]
    public Button selectButton;            // 选择/删除按钮

    [Header("引用")]
    public NumberCardUIFactory numberCardLibrary;  // 数字卡库（可选）
    public GameObject formulaCardPrefab;           // 公式卡prefab（可选）

    [Header("尺寸配置")]
    [Tooltip("数字卡的容器大小")]
    public Vector2 numberCardSize = new Vector2(280, 200);

    [Tooltip("公式卡的容器大小")]
    public Vector2 formulaCardSize = new Vector2(1450, 310);

    private NumberCardInstance boundNumberCard;
    private FormulaCardData boundFormulaCard;
    private Action<object> onSelectCallback;  // 回调参数可以是 NumberCardInstance 或 FormulaCardData

    /// <summary>
    /// 绑定数字卡到删除槽位
    /// </summary>
    public void BindNumberCardForDeletion(NumberCardInstance cardInstance, Action<object> onSelectCallback)
    {
        if (cardInstance == null || cardInstance.cardData == null)
        {
            Debug.LogError("[DeleteCardSlot] 数字卡实例或数据为空");
            return;
        }

        this.boundNumberCard = cardInstance;
        this.boundFormulaCard = null;  // 清除公式卡数据
        this.onSelectCallback = onSelectCallback;

        // 清理旧内容
        ClearCardContent();

        // 调整容器大小为数字卡大小
        AdjustContainerSize(numberCardSize);

        // 显示卡牌
        DisplayNumberCard(cardInstance);

        // 设置选择按钮
        SetupSelectButton(cardInstance.cardData.cardName);

        Debug.Log($"[DeleteCardSlot] 绑定数字卡删除槽位：{cardInstance.cardData.cardName}");
    }

    /// <summary>
    /// 绑定公式卡到删除槽位
    /// </summary>
    public void BindFormulaCardForDeletion(FormulaCardData formulaData, Action<object> onSelectCallback)
    {
        if (formulaData == null)
        {
            Debug.LogError("[DeleteCardSlot] 公式卡数据为空");
            return;
        }

        this.boundNumberCard = null;  // 清除数字卡数据
        this.boundFormulaCard = formulaData;
        this.onSelectCallback = onSelectCallback;

        // 清理旧内容
        ClearCardContent();

        // 调整容器大小为公式卡大小
        AdjustContainerSize(formulaCardSize);

        // 显示卡牌
        DisplayFormulaCard(formulaData);

        // 设置选择按钮
        SetupSelectButton(formulaData.Name);

        Debug.Log($"[DeleteCardSlot] 绑定公式卡删除槽位：{formulaData.Name}");
    }

    /// <summary>
    /// 显示数字卡
    /// </summary>
    private void DisplayNumberCard(NumberCardInstance cardInstance)
    {
        if (cardContentRoot == null)
        {
            Debug.LogError("[DeleteCardSlot] cardContentRoot 未绑定");
            return;
        }

        // 选择工厂
        NumberCardUIFactory factory = numberCardLibrary;
        if (factory == null && UIManager.Instance != null)
            factory = UIManager.Instance.numberCardLibrary;

        if (factory == null)
        {
            Debug.LogError("[DeleteCardSlot] 未配置 NumberCardUIFactory");
            return;
        }

        // 获取prefab
        GameObject prefab = factory.GetPrefab(cardInstance.cardData.layoutType);
        if (prefab == null)
        {
            Debug.LogError($"[DeleteCardSlot] 找不到布局类型 {cardInstance.cardData.layoutType} 的Prefab");
            return;
        }

        // 实例化卡牌
        GameObject cardGo = Instantiate(prefab, cardContentRoot);
        cardGo.transform.localScale = Vector3.one;
        cardGo.transform.localPosition = Vector3.zero;
        cardGo.SetActive(true);

        // 禁用拖动
        PlayerController playerController = cardGo.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.enabled = false;
        }

        // 绑定数据
        var view = cardGo.GetComponent<NumberCardLayoutView>();
        if (view != null)
        {
            view.BindInstance(cardInstance);
            Debug.Log($"[DeleteCardSlot] 显示数字卡：{cardInstance.cardData.cardName}");
        }
        else
        {
            Debug.LogWarning($"[DeleteCardSlot] 数字卡Prefab缺少NumberCardLayoutView组件");
        }
    }

    /// <summary>
    /// 显示公式卡
    /// </summary>
    private void DisplayFormulaCard(FormulaCardData formulaData)
    {
        if (cardContentRoot == null)
        {
            Debug.LogError("[DeleteCardSlot] cardContentRoot 未绑定");
            return;
        }

        // 获取公式卡prefab
        GameObject prefab = formulaCardPrefab;
        if (prefab == null && UIManager.Instance != null)
            prefab = UIManager.Instance.formulaCardPrefab;

        if (prefab == null)
        {
            Debug.LogError("[DeleteCardSlot] 未配置公式卡prefab");
            return;
        }

        // 实例化卡牌
        GameObject cardGo = Instantiate(prefab, cardContentRoot);
        cardGo.transform.localScale = Vector3.one;
        cardGo.transform.localPosition = Vector3.zero;
        cardGo.SetActive(true);

        // 绑定数据
        var view = cardGo.GetComponent<FormulaCardUI>();
        if (view != null)
        {
            view.Bind(formulaData);
            Debug.Log($"[DeleteCardSlot] 显示公式卡：{formulaData.Name}");
        }
        else
        {
            Debug.LogWarning($"[DeleteCardSlot] 公式卡Prefab缺少FormulaCardUI组件");
        }
    }

    /// <summary>
    /// 调整容器大小（根据卡牌类型）
    /// </summary>
    private void AdjustContainerSize(Vector2 newSize)
    {
        if (cardContentRoot == null) return;

        RectTransform rectTransform = cardContentRoot.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = newSize;
        }
    }

    /// <summary>
    /// 设置选择按钮
    /// </summary>
    private void SetupSelectButton(string cardName)
    {
        if (selectButton == null)
        {
            Debug.LogWarning("[DeleteCardSlot] selectButton 未绑定");
            return;
        }

        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(OnSelectClick);

        selectButton.interactable = true;
    }

    /// <summary>
    /// 选择按钮点击事件
    /// </summary>
    private void OnSelectClick()
    {
        if (boundNumberCard != null)
        {
            Debug.Log($"[DeleteCardSlot] 用户选择删除数字卡：{boundNumberCard.cardData.cardName}");
            onSelectCallback?.Invoke(boundNumberCard);
        }
        else if (boundFormulaCard != null)
        {
            Debug.Log($"[DeleteCardSlot] 用户选择删除公式卡：{boundFormulaCard.Name}");
            onSelectCallback?.Invoke(boundFormulaCard);
        }
    }

    /// <summary>
    /// 清理卡牌内容
    /// </summary>
    private void ClearCardContent()
    {
        if (cardContentRoot == null) return;

        foreach (Transform child in cardContentRoot)
        {
            Destroy(child.gameObject);
        }
    }
}