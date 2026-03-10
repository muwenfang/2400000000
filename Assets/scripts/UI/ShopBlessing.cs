using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 商店中的祝福卡展示 - 包含祝福UI、价格和购买按钮
/// 该脚本应该挂在商店中每个祝福卡槽位上
/// </summary>
public class BlessingInShop : MonoBehaviour
{
    [Header("祝福数据")]
    private ShopItem<BlessingData> shopItem; // 商品数据

    [Header("UI 组件")]
    [SerializeField] private BlessingUI blessingUI;           // 祝福UI面板
    [SerializeField] private Text priceText;                  // 价格文本
    [SerializeField] private Button purchaseButton;           // 购买按钮
    [SerializeField] private Text purchaseButtonText;         // 购买按钮文本
    [SerializeField] private Image purchaseButtonImage;       // 购买按钮背景
    [SerializeField] private CanvasGroup canvasGroup;         // 用于控制整体透明度

    [Header("视觉反馈")]
    [SerializeField] private Color normalButtonColor = Color.white;      // 正常按钮颜色
    [SerializeField] private float soldOutAlpha = 0.5f;                  // 已售出时的透明度

    private int currentPurchaseCount = 0; // 该祝福的当前购买次数
    private bool isSoldOut = false;

    private void OnEnable()
    {
        if (purchaseButton != null)
        {
            purchaseButton.onClick.AddListener(OnPurchaseButtonClicked);
        }

        // 监听点数变化事件（如果有的话）
        if (GameManager.Instance != null)
        {
            // 可以在这里订阅点数变化事件
        }
    }

    private void OnDisable()
    {
        if (purchaseButton != null)
        {
            purchaseButton.onClick.RemoveListener(OnPurchaseButtonClicked);
        }
    }

    /// <summary>
    /// 初始化商店中的祝福卡
    /// </summary>
    public void Initialize(ShopItem<BlessingData> item)
    {
        if (item == null || item.cardData == null)
        {
            Debug.LogError("祝福商品数据为空！");
            return;
        }

        shopItem = item;
        isSoldOut = item.sold;
        currentPurchaseCount = BlessingManager.Instance.GetBlessingCount(item.cardData.blessingId);

        // 设置祝福UI
        if (blessingUI != null)
        {
            blessingUI.SetBlessingData(item.cardData);
        }

        // 更新价格显示
        UpdatePriceDisplay();

        // 更新按钮状态
        UpdateButtonState();

        // 显示或隐藏已售出标记
        UpdateSoldOutDisplay();

        Debug.Log($"已初始化祝福商品：{item.cardData.blessingName}，价格：{item.price}");
    }

    /// <summary>
    /// 更新价格显示
    /// </summary>
    private void UpdatePriceDisplay()
    {
        if (shopItem == null)
            return;

        if (priceText != null)
        {
            priceText.text = shopItem.price.ToString();

            priceText.color = Color.black;
            
        }
    }

    /// <summary>
    /// 更新按钮状态
    /// </summary>
    private void UpdateButtonState()
    {
        if (purchaseButton == null || shopItem == null)
            return;

        bool canPurchase = !isSoldOut && GameManager.Instance.currentPoints >= shopItem.price;

        purchaseButton.interactable = canPurchase;

        if (purchaseButtonImage != null)
        {
            purchaseButtonImage.color = normalButtonColor;         
        }
    }

    /// <summary>
    /// 更新已售出显示
    /// </summary>
    private void UpdateSoldOutDisplay()
    {
        if (canvasGroup != null && isSoldOut)
        {
            canvasGroup.alpha = soldOutAlpha;
        }
        else if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }
    }

    /// <summary>
    /// 购买按钮点击事件
    /// </summary>
    private void OnPurchaseButtonClicked()
    {
        if (shopItem == null)
        {
            Debug.LogError("商品数据为空");
            return;
        }

        // 调用 ShopManager 的购买方法
        bool purchaseSuccess = ShopManager.Instance.TryBuyBlessing(shopItem);

        if (purchaseSuccess)
        {
            isSoldOut = true;
            UpdateButtonState();
            UpdateSoldOutDisplay();
            UpdatePriceDisplay();
        }
    }

    /// <summary>
    /// 刷新显示（外部调用，用于点数变化等情况）
    /// </summary>
    public void Refresh()
    {
        if (shopItem == null)
            return;

        // 更新购买次数
        currentPurchaseCount = BlessingManager.Instance.GetBlessingCount(shopItem.cardData.blessingId);

        // 检查商品是否已售出
        isSoldOut = shopItem.sold;

        UpdatePriceDisplay();
        UpdateButtonState();
        UpdateSoldOutDisplay();
    }

    /// <summary>
    /// 获取商品信息（用于UI其他部分）
    /// </summary>
    public ShopItem<BlessingData> GetShopItem()
    {
        return shopItem;
    }

    /// <summary>
    /// 获取祝福数据
    /// </summary>
    public BlessingData GetBlessingData()
    {
        return shopItem != null ? shopItem.cardData : null;
    }

    /// <summary>
    /// 获取商品价格
    /// </summary>
    public int GetPrice()
    {
        return shopItem != null ? shopItem.price : 0;
    }

    /// <summary>
    /// 获取祝福名称
    /// </summary>
    public string GetBlessingName()
    {
        if (blessingUI != null)
            return blessingUI.GetBlessingName();
        return shopItem != null ? shopItem.cardData.blessingName : "";
    }

    /// <summary>
    /// 获取该祝福的购买次数
    /// </summary>
    public int GetPurchaseCount()
    {
        return currentPurchaseCount;
    }

    /// <summary>
    /// 是否已售出
    /// </summary>
    public bool IsSoldOut()
    {
        return isSoldOut;
    }

    /// <summary>
    /// 是否可以购买
    /// </summary>
    public bool CanPurchase()
    {
        if (shopItem == null || isSoldOut)
            return false;

        return GameManager.Instance.currentPoints >= shopItem.price;
    }

    /// <summary>
    /// 手动设置已售出状态（用于测试或特殊情况）
    /// </summary>
    public void SetSoldOut(bool soldOut)
    {
        isSoldOut = soldOut;
        if (shopItem != null)
        {
            shopItem.sold = soldOut;
        }
        UpdateButtonState();
        UpdateSoldOutDisplay();
    }

    /// <summary>
    /// 重置购买状态（用于新一轮商店）
    /// </summary>
    public void ResetPurchaseState()
    {
        isSoldOut = false;
        if (shopItem != null)
        {
            shopItem.sold = false;
        }
        Refresh();
    }
}
