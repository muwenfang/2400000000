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
    private int slotIndex;                      // 该祝福在商店中的槽位索引

    [Header("UI 组件")]
    public BlessingUI blessingUI;           // 祝福UI面板
    public Text priceText;                  // 价格文本
    public Button purchaseButton;           // 购买按钮
    public Text purchaseButtonText;         // 购买按钮文本
    public Image purchaseButtonImage;       // 购买按钮背景
    public CanvasGroup canvasGroup;         // 用于控制整体透明度

    public GameObject lockedPanel;          // 锁定面板（显示未解锁状态）
    public Text lockedText;                 // 锁定文本（显示解锁条件）
    public GameObject unlockButton;         // 锁定状态下的按钮（点击后显示解锁信息）
    public Text unlockCostText;             // 解锁成本显示


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

        if (unlockButton != null)
        {
            var unlockBtnComp = unlockButton.GetComponent<Button>();
            if (unlockBtnComp != null)
            {
                unlockBtnComp.onClick.AddListener(OnUnlockClick);
            }
        }
    }

    private void OnDisable()
    {
        if (purchaseButton != null)
        {
            purchaseButton.onClick.RemoveListener(OnPurchaseButtonClicked);
        }
        if (unlockButton != null)
        {
            var unlockBtnComp = unlockButton.GetComponent<Button>();
            if (unlockBtnComp != null)
            {
                unlockBtnComp.onClick.RemoveListener(OnUnlockClick);
            }
        }
    }
    /// <summary>
    /// 初始化商店中的祝福卡
    /// </summary>
    public void Initialize(ShopItem<BlessingData> item,int index)
    {
        slotIndex = index;
        // 如果是未解锁的槽位（item为空或cardData为空）
        if (item == null || item.cardData == null)
        {
            ShowLockedState();
            SetupUnlockButton();
            return;
        }

        shopItem = item;
        isSoldOut = item.sold;
        currentPurchaseCount = BlessingManager.Instance != null
                         ? BlessingManager.Instance.GetBlessingCount(item.cardData.blessingId)
                         : 0;
        // 显示解锁状态
        ShowUnlockedState();

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
    /// 显示未解锁状态
    /// </summary>
    void ShowLockedState()
    {
        // 显示未解锁面板
        if (lockedPanel != null)
            lockedPanel.SetActive(true);

        // 隐藏祝福信息和购买按钮
        if (blessingUI != null)
            blessingUI.gameObject.SetActive(false);

        if (purchaseButton != null)
            purchaseButton.gameObject.SetActive(false);

        if (priceText != null)
            priceText.gameObject.SetActive(false);

        // 设置未解锁提示文本
        if (lockedText != null)
            lockedText.text = "已锁定";
    }

    /// <summary>
    /// 显示解锁状态
    /// </summary>
    void ShowUnlockedState()
    {
        // 隐藏未解锁面板
        if (lockedPanel != null)
            lockedPanel.SetActive(false);

        // 显示祝福信息和购买按钮
        if (blessingUI != null)
            blessingUI.gameObject.SetActive(true);

        if (purchaseButton != null)
            purchaseButton.gameObject.SetActive(true);

        if (priceText != null)
            priceText.gameObject.SetActive(true);
    }

    /// <summary>
    /// 设置解锁按钮
    /// </summary>
    void SetupUnlockButton()
    {
        if (unlockButton == null)
            return;

        var unlockBtnComp = unlockButton.GetComponent<Button>();
        if (unlockBtnComp != null)
        {
            unlockBtnComp.onClick.RemoveAllListeners();
            unlockBtnComp.onClick.AddListener(OnUnlockClick);
        }

        // 更新解锁成本显示
        UpdateUnlockCostDisplay();
    }

    /// <summary>
    /// 更新解锁成本显示
    /// </summary>
    void UpdateUnlockCostDisplay()
    {
        if (unlockCostText == null)
            return;

        long unlockCost = ShopManager.Instance.CalculateBlessingSlotUnlockCost();
        unlockCostText.text = $"{unlockCost}";

        // 根据点数情况改变颜色
        if (GameManager.Instance.currentPoints >= unlockCost)
        {
            unlockCostText.color = Color.green;
        }
        else
        {
            unlockCostText.color = Color.red;
        }
    }

    /// <summary>
    /// 解锁按钮点击事件
    /// </summary>
    void OnUnlockClick()
    {
        bool success = ShopManager.Instance.TryUnlockBlessingSlot();

        if (success)
        {
            Debug.Log("祝福卡槽位解锁成功");
            // UI 会在 RefreshShopUI 中自动更新
        }
        else
        {
            Debug.LogWarning("祝福卡槽位解锁失败");
        }
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
        if (isSoldOut)
        {
            purchaseButtonText.text = "已购买";
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

            Debug.Log($"购买成功：{shopItem.cardData.blessingName}");
        }
    }

    /// <summary>
    /// 刷新显示（外部调用，用于点数变化等情况）
    /// </summary>
    public void Refresh()
    {
        if (shopItem == null || shopItem.cardData == null)
        {
            // 未解锁状态，更新解锁成本显示
            UpdateUnlockCostDisplay();
            return;
        }

        // 已解锁状态，更新购买相关信息
        currentPurchaseCount = BlessingManager.Instance != null
            ? BlessingManager.Instance.GetBlessingCount(shopItem.cardData.blessingId)
            : 0;
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
    public long GetPrice()
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

    /// <summary>
    /// 获取槽位索引
    /// </summary>
    public int GetSlotIndex()
    {
        return slotIndex;
    }
}
