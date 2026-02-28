using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 公式卡商店槽位UI组件
/// 包含：卡牌内容区域、价格文本、购买按钮、锁定状态
/// </summary>
public class ShopFormulaCardSlot : MonoBehaviour
{
    [Header("卡牌内容区域")]
    public Transform cardContentRoot;  // 公式卡显示的父容器

    [Header("价格和购买")]
    public Text priceText;              // 价格文本
    public Button buyButton;            // 购买按钮

    [Header("锁定状态")]
    public GameObject lockedPanel;      // 锁定状态的遮罩面板
    public Text lockedText;             // 锁定提示文本

    [Header("公式卡信息文本（在cardContentRoot内创建）")]
    public Text formulaNameText;

    //[Header("Prefab引用")]
    //public GameObject formulaCardPrefab; // 公式卡UI的Prefab

    private ShopItem<FormulaCardData> currentItem;
    private int slotIndex;

    /// <summary>
    /// 绑定公式卡到槽位
    /// </summary>
    public void BindFormulaCard(ShopItem<FormulaCardData> item, int index)
    {
        slotIndex = index;
        currentItem = item;

        // 如果是锁定槽位
        if (item == null || item.cardData == null)
        {
            ShowLockedState();
            return;
        }

        // 显示正常卡牌
        ShowUnlockedState();

        // 清理旧内容
        ClearCardContent();

        //// 生成公式卡UI
        //if (formulaCardPrefab != null)
        //{
        //    GameObject cardGo = Instantiate(formulaCardPrefab, cardContentRoot);
        //    cardGo.transform.localScale = Vector3.one;
        //    cardGo.transform.localPosition = Vector3.zero;

        //    // 绑定公式卡数据
        //    var formulaUI = cardGo.GetComponent<FormulaCardUI>();
        //    if (formulaUI != null)
        //    {
        //        formulaUI.Bind(item.cardData);
        //    }
        //    else
        //    {
        //        Debug.LogError($"公式卡槽位 {index} 的 Prefab 缺少 FormulaCardUI 组件！");
        //    }
        //}
        //else
        //{
        //    Debug.LogError("formulaCardPrefab 未设置！");
        //}
        // 防御性检查：cardContentRoot 必须存在
        if (cardContentRoot == null)
        {
            Debug.LogError($"[ShopFormulaCardSlot] 槽位 {index}: cardContentRoot 未绑定！");
            ShowLockedState();
            return;
        }

        //  新逻辑：直接显示公式卡信息，不生成完整UI
        DisplayFormulaInfo(item.cardData);

        // 设置价格
        UpdatePriceDisplay(item.price, item.sold);

        // 设置购买按钮
        SetupBuyButton(item.sold);
    }
    /// <summary>
    /// 显示公式卡信息（名字、图案、所需数量）
    /// </summary>
    void DisplayFormulaInfo(FormulaCardData formulaData)
    {
        if (formulaData == null)
        {
            Debug.LogError($"[ShopFormulaCardSlot] 槽位 {slotIndex}: formulaData 为 null");
            return;
        }

        // 显示公式卡名字（就是Pattern，例如 "#*#*#"）
        if (formulaNameText != null)
        {
            formulaNameText.text = formulaData.Pattern;
            formulaNameText.fontSize = 90;
            formulaNameText.color = Color.black;
            Debug.Log($"[ShopFormulaCardSlot] 槽位 {slotIndex}: 显示公式名字 = {formulaData.Pattern}");
        }
    }

        /// <summary>
        /// 更新价格显示
        /// </summary>
        void UpdatePriceDisplay(int price, bool sold)
    {
        if (priceText == null) return;

        if (sold)
        {
            priceText.text = "已售出";
            priceText.color = Color.gray;
        }
        else
        {
            priceText.text = $"${price}";
            priceText.color = Color.white;
        }
    }

    /// <summary>
    /// 设置购买按钮
    /// </summary>
    void SetupBuyButton(bool sold)
    {
        if (buyButton == null) return;

        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(OnBuyClick);
        buyButton.interactable = !sold;

    }

    /// <summary>
    /// 购买按钮点击事件
    /// </summary>
    void OnBuyClick()
    {
        if (currentItem == null || currentItem.cardData == null)
        {
            Debug.LogWarning("没有有效的公式卡可购买");
            return;
        }

        bool success = ShopManager.Instance.TryBuyFormulaCard(currentItem);

        if (success)
        {
            // 购买成功，更新UI
            UpdatePriceDisplay(currentItem.price, true);
            SetupBuyButton(true);

            // 刷新点数显示
            UIManager.Instance.UpdatePointsDisplay(GameManager.Instance.currentPoints);

            Debug.Log($"成功购买公式卡：{currentItem.cardData.Name}");
        }
        else
        {
            Debug.Log("购买失败！点数不足");
        }
    }

    /// <summary>
    /// 显示锁定状态
    /// </summary>
    void ShowLockedState()
    {
        if (lockedPanel != null)
            lockedPanel.SetActive(true);

        if (cardContentRoot != null)
            cardContentRoot.gameObject.SetActive(false);

        if (buyButton != null)
            buyButton.gameObject.SetActive(false);

        if (priceText != null)
            priceText.gameObject.SetActive(false);

        if (lockedText != null)
            lockedText.text = "已锁定";
    }

    /// <summary>
    /// 显示解锁状态
    /// </summary>
    void ShowUnlockedState()
    {
        if (lockedPanel != null)
            lockedPanel.SetActive(false);

        if (cardContentRoot != null)
            cardContentRoot.gameObject.SetActive(true);

        if (buyButton != null)
            buyButton.gameObject.SetActive(true);

        if (priceText != null)
            priceText.gameObject.SetActive(true);
    }

    /// <summary>
    /// 清理卡牌内容
    /// </summary>
    void ClearCardContent()
    {
        if (cardContentRoot == null) return;

        foreach (Transform child in cardContentRoot)
        {
            Destroy(child.gameObject);
        }
    }
}