using System;
using BigInteger = System.Numerics.BigInteger;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 商店中的祝福卡槽位UI组件（完整版）
/// 类似 ShopNumberCardSlot，支持绑定祝福数据到 cardContentRoot 并显示
/// 包含：祝福内容区域、价格文本、购买按钮、锁定状态
/// </summary>
public class BlessingInShop : MonoBehaviour
{
    [Header("祝福内容区域")]
    public Transform cardContentRoot;  // 祝福卡显示的父容器（类似 ShopNumberCardSlot 的设计）

    [Header("价格和购买")]
    public Text priceText;              // 价格文本
    public Button purchaseButton;       // 购买按钮
    public Text purchaseButtonText;     // 购买按钮文本

    [Header("锁定状态")]
    public GameObject lockedPanel;      // 锁定状态的遮罩面板
    public Text lockedText;             // 锁定提示文本
    public Button unlockButton;         // 解锁按钮
    public Text unlockCostText;         // 解锁成本显示

    [Header("UI组件 - 祝福信息")]
    public BlessingUI blessingUI;       // 运行时创建的BlessingUI实例
    public GameObject blessingUIPrefab;  // BlessingUI的Prefab

    [Header("视觉反馈")]
    [SerializeField] private Color normalButtonColor = Color.white;      // 正常按钮颜色
    [SerializeField] private Color disabledButtonColor = Color.gray;     // 禁用按钮颜色
    //[SerializeField] private float soldOutAlpha = 0.5f;                  // 已售出时的透明度

    // 私有数据
    private ShopItem<BlessingData> currentItem;
    private int slotIndex;
    private int currentPurchaseCount = 0;
    private bool isSoldOut = false;

    private void OnEnable()
    {
        if (purchaseButton != null)
        {
            purchaseButton.onClick.AddListener(OnPurchaseButtonClicked);
        }

        if (unlockButton != null)
        {
            unlockButton.onClick.AddListener(OnUnlockClick);
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
            unlockButton.onClick.RemoveListener(OnUnlockClick);
        }
    }

    /// <summary>
    /// 绑定祝福卡到槽位
    /// </summary>
    public void BindBlessing(ShopItem<BlessingData> item, int index)
    {
        slotIndex = index;
        currentItem = item;

        // 如果是锁定槽位
        if (index >= ShopManager.Instance.blessingCardCount)
        {
            ShowLockedState();
            SetupUnlockButton();
            return;
        }

        // 显示正常槽位
        ShowUnlockedState();

        // 清理旧内容
        ClearCardContent();

        // 防御性检查：cardContentRoot 必须存在
        if (cardContentRoot == null)
        {
            Debug.LogError($"【BlessingInShop】 槽位{index}：cardContentRoot 未绑定！无法显示祝福内容（检查 prefab 的 Inspector）");
            ShowLockedState();
            return;
        }

        // 防御性检查：item 数据完整性
        if (item == null || item.cardData == null)
        {
            Debug.LogError($"【BlessingInShop】 槽位{index}：祝福数据为null");
            ShowLockedState();
            return;
        }

        // 获取当前购买次数
        currentPurchaseCount = BlessingManager.Instance != null
            ? BlessingManager.Instance.GetBlessingCount(item.cardData.blessingId)
            : 0;
        isSoldOut = item.sold;

        // 在 cardContentRoot 中动态创建BlessingUI
        DisplayBlessingContent(item.cardData, currentPurchaseCount);

        // 设置价格显示
        UpdatePriceDisplay(item.price, item.sold);

        // 设置购买按钮
        SetupBuyButton(item.sold);
    }

    /// <summary>
    /// 在 cardContentRoot 中动态创建和显示祝福UI
    /// </summary>
    void DisplayBlessingContent(BlessingData blessingData, int stackCount)
    {
        if (blessingData == null)
        {
            Debug.LogError($"【BlessingInShop】 槽位{slotIndex}：祝福数据为null");
            return;
        }

        if (cardContentRoot == null)
        {
            Debug.LogError($"【BlessingInShop】 槽位{slotIndex}：cardContentRoot为null");
            return;
        }

        // 检查 blessingUIPrefab
        if (blessingUIPrefab != null)
        {
            try
            {
                GameObject uiGo = Instantiate(blessingUIPrefab, cardContentRoot);
                uiGo.name = $"BlessingUI_{blessingData.blessingId}";
                uiGo.transform.localPosition = Vector3.zero;
                uiGo.transform.localRotation = Quaternion.identity;
                uiGo.transform.localScale = Vector3.one;
                uiGo.SetActive(true);

                BlessingUI newBlessingUI = uiGo.GetComponent<BlessingUI>();
                if (newBlessingUI != null)
                {
                    blessingUI = newBlessingUI;
                    blessingUI.SetBlessingData(blessingData, stackCount);
                }
                else
                {
                    Debug.LogError($"【BlessingInShop】 槽位{slotIndex}：Prefab缺少BlessingUI脚本！");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"【BlessingInShop】 槽位{slotIndex}：创建BlessingUI失败：{e.Message}");
            }
        }
        // 如果没有Prefab，使用现有的组件
        else if (blessingUI != null)
        {
            blessingUI.SetBlessingData(blessingData, stackCount);
            Debug.Log($"【BlessingInShop】 槽位{slotIndex}：使用已配置的BlessingUI组件: {blessingData.blessingName}");
        }
        else
        {
            Debug.LogWarning($"【BlessingInShop】 槽位{slotIndex}：既没有设置blessingUIPrefab，也没有配置blessingUI组件！无法显示祝福");
        }
    }


    /// <summary>
    /// 清理卡牌内容
    /// </summary>
    void ClearCardContent()
    {
        if (cardContentRoot == null)
            return;

        foreach (Transform child in cardContentRoot)
        {
            Destroy(child.gameObject);
        }
        blessingUI = null;
    }

    /// <summary>
    /// 显示锁定状态
    /// </summary>
    void ShowLockedState()
    {
        if (lockedPanel != null)
            lockedPanel.SetActive(true);

        if (cardContentRoot != null)
            cardContentRoot.gameObject.SetActive(true);

        if (purchaseButton != null)
            purchaseButton.gameObject.SetActive(false);

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

        if (purchaseButton != null)
            purchaseButton.gameObject.SetActive(true);

        if (priceText != null)
            priceText.gameObject.SetActive(true);

        if (blessingUI != null)
            blessingUI.gameObject.SetActive(true);
    }

    /// <summary>
    /// 更新价格显示
    /// </summary>
    void UpdatePriceDisplay(BigInteger price, bool sold)
    {
        if (priceText == null)
        {
            Debug.LogWarning($"【BlessingInShop】 槽位{slotIndex}：priceText 未绑定");
            return;
        }

        if (sold)
        {
            priceText.text = "已售出";
            priceText.color = Color.gray;
        }
        else
        {
            // 统一用商店的科学计数法格式化
            priceText.text = $"${ShopManager.Instance.FormatBigNumber(price)}";
            priceText.color = Color.black;
        }
    }

    /// <summary>
    /// 设置购买按钮
    /// </summary>
    void SetupBuyButton(bool sold)
    {
        if (purchaseButton == null)
        {
            Debug.LogWarning($"【BlessingInShop】 槽位{slotIndex}：purchaseButton 未绑定");
            return;
        }

        purchaseButton.onClick.RemoveAllListeners();
        purchaseButton.onClick.AddListener(OnPurchaseButtonClicked);
        purchaseButton.interactable = !sold;

        if (purchaseButtonText != null)
        {
            if (sold)
            {
                purchaseButtonText.text = "已购买";
            }
        }
    }

    /// <summary>
    /// 设置解锁按钮
    /// </summary>
    void SetupUnlockButton()
    {
        if (unlockButton == null)
            return;

        unlockButton.onClick.RemoveAllListeners();
        unlockButton.onClick.AddListener(OnUnlockClick);

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

        BigInteger unlockCost = ShopManager.Instance.CalculateBlessingSlotUnlockCost();
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
    /// 购买按钮点击事件
    /// </summary>
    void OnPurchaseButtonClicked()
    {
        if (currentItem == null || currentItem.cardData == null)
        {
            Debug.LogWarning("【BlessingInShop】 没有有效的祝福卡可购买");
            return;
        }

        bool success = ShopManager.Instance.TryBuyBlessing(currentItem);

        if (success)
        {
            // 购买成功，更新UI
            isSoldOut = true;
            currentItem.sold = true;

            UpdatePriceDisplay(currentItem.price, true);
            SetupBuyButton(true);

            // 刷新点数显示
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdatePointsDisplay(GameManager.Instance.currentPoints);
            }

            Debug.Log($"【BlessingInShop】 成功购买祝福卡：{currentItem.cardData.blessingName}");
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
            Debug.Log("【BlessingInShop】 祝福卡槽位解锁成功");
            // UI 会在 RefreshShopUI 中自动更新
        }
        else
        {
            Debug.LogWarning("【BlessingInShop】 祝福卡槽位解锁失败");
        }
    }

    /// <summary>
    /// 刷新显示（外部调用，用于点数变化等情况）
    /// </summary>
    public void Refresh()
    {
        if (currentItem == null || currentItem.cardData == null)
        {
            // 未解锁状态，更新解锁成本显示
            UpdateUnlockCostDisplay();
            return;
        }

        // 已解锁状态，更新购买相关信息
        currentPurchaseCount = BlessingManager.Instance != null
            ? BlessingManager.Instance.GetBlessingCount(currentItem.cardData.blessingId)
            : 0;

        isSoldOut = currentItem.sold;

        UpdatePriceDisplay(currentItem.price, isSoldOut);
        SetupBuyButton(isSoldOut);

        // 刷新BlessingUI显示
        if (blessingUI != null && currentItem.cardData != null)
        {
            blessingUI.SetBlessingData(currentItem.cardData, currentPurchaseCount);
        }
    }

    // ==================== Getter 方法 ====================

    /// <summary>
    /// 获取商品信息
    /// </summary>
    public ShopItem<BlessingData> GetShopItem()
    {
        return currentItem;
    }

    /// <summary>
    /// 获取祝福数据
    /// </summary>
    public BlessingData GetBlessingData()
    {
        return currentItem != null ? currentItem.cardData : null;
    }

    /// <summary>
    /// 获取价格
    /// </summary>
    public BigInteger GetPrice()
    {
        return currentItem != null ? currentItem.price : 0;
    }

    /// <summary>
    /// 获取祝福名称
    /// </summary>
    public string GetBlessingName()
    {
        if (blessingUI != null)
        {
            string name = blessingUI.GetBlessingName();
            if (!string.IsNullOrEmpty(name))
                return name;
        }
        return currentItem != null ? currentItem.cardData.blessingName : "";
    }

    /// <summary>
    /// 获取购买次数
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
        if (currentItem == null || isSoldOut)
            return false;

        return GameManager.Instance.currentPoints >= currentItem.price;
    }

    /// <summary>
    /// 是否已解锁
    /// </summary>
    public bool IsUnlocked()
    {
        return currentItem != null && currentItem.cardData != null;
    }

    /// <summary>
    /// 获取槽位索引
    /// </summary>
    public int GetSlotIndex()
    {
        return slotIndex;
    }

    /// <summary>
    /// 手动设置已售出状态（用于测试）
    /// </summary>
    public void SetSoldOut(bool soldOut)
    {
        isSoldOut = soldOut;
        if (currentItem != null)
        {
            currentItem.sold = soldOut;
        }
        SetupBuyButton(soldOut);
    }

    /// <summary>
    /// 重置购买状态（用于新一轮商店）
    /// </summary>
    public void ResetPurchaseState()
    {
        isSoldOut = false;
        if (currentItem != null)
        {
            currentItem.sold = false;
        }
        Refresh();
    }
}