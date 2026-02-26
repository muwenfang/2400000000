using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 数字卡商店槽位UI组件
/// 包含：卡牌内容区域、价格文本、购买按钮、锁定状态
/// </summary>
public class ShopNumberCardSlot : MonoBehaviour
{
    [Header("卡牌内容区域")]
    public Transform cardContentRoot;  // 数字卡显示的父容器

    [Header("价格和购买")]
    public Text priceText;              // 价格文本
    public Button buyButton;            // 购买按钮
    public Text buyButtonText;          // 购买按钮上的文字

    [Header("锁定状态")]
    public GameObject lockedPanel;      // 锁定状态的遮罩面板
    public Text lockedText;             // 锁定提示文本

    [Header("引用")]
    public NumberCardUIFactory numberCardLibrary; // 用于获取数字卡 Prefab

    private ShopItem<NumberCardInstance> currentItem;
    private int slotIndex;

    /// <summary>
    /// 绑定数字卡到槽位
    /// </summary>
    public void BindNumberCard(ShopItem<NumberCardInstance> item, int index)
    {
        slotIndex = index;
        currentItem = item;

        // 如果是锁定槽位（item.cardData == null）
        if (item == null || item.cardData == null)
        {
            ShowLockedState();
            return;
        }

        // 显示正常卡牌
        ShowUnlockedState();

        // 清理旧内容
        ClearCardContent();

        // 获取对应布局的 Prefab
        GameObject prefab = numberCardLibrary.GetPrefab(item.cardData.cardData.layoutType);
        if (prefab != null)
        {
            // 生成卡牌主体
            GameObject cardGo = Instantiate(prefab, cardContentRoot);
            cardGo.transform.localScale = Vector3.one;
            cardGo.transform.localPosition = Vector3.zero;

            // 绑定数据
            var view = cardGo.GetComponent<NumberCardLayoutView>();
            if (view != null)
            {
                view.Bind(item.cardData.cardData);
            }
            else
            {
                Debug.LogError($"数字卡槽位 {index} 的 Prefab 缺少 NumberCardLayoutView 组件！");
            }
        }
        else
        {
            Debug.LogError($"找不到布局类型 {item.cardData.cardData.layoutType} 的 Prefab！");
        }

        // 设置价格
        UpdatePriceDisplay(item.price, item.sold);

        // 设置购买按钮
        SetupBuyButton(item.sold);
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

        if (buyButtonText != null)
        {
            buyButtonText.text = sold ? "已购买" : "购买";
        }
    }

    /// <summary>
    /// 购买按钮点击事件
    /// </summary>
    void OnBuyClick()
    {
        if (currentItem == null || currentItem.cardData == null)
        {
            Debug.LogWarning("没有有效的数字卡可购买");
            return;
        }



        bool success = ShopManager.Instance.TryBuyNumberCard(currentItem);

        if (success)
        {
            // 购买成功，更新UI
            UpdatePriceDisplay(currentItem.price, true);
            SetupBuyButton(true);

            // 刷新点数显示
            UIManager.Instance.UpdatePointsDisplay(GameManager.Instance.currentPoints);

            Debug.Log($"成功购买数字卡：{currentItem.cardData.cardData.cardName}");
        }
        else
        {
            Debug.Log("购买失败！点数不足");
            // 可以在这里添加提示动画或音效
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
            lockedText.text = "🔒 已锁定";
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
