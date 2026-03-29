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

    [Header("锁定状态")]
    public GameObject lockedPanel;      // 锁定状态的遮罩面板
    public Text lockedText;             // 锁定提示文本
    public Button unlockButton;         // 新增：解锁按钮
    public Text unlockCostText;         // 新增：解锁成本显示

    [Header("引用")]
    public NumberCardUIFactory numberCardLibrary; // 用于获取数字卡 Prefab

    private ShopItem<NumberCardInstance> currentItem;
    private int slotIndex;
    private NumberCardInstance boundCard;
    /// <summary>
    /// 绑定数字卡到槽位
    /// </summary>
    public void BindNumberCard(ShopItem<NumberCardInstance> item, int index)
    {
        slotIndex = index;
        currentItem = item;

        // 如果是锁定槽位
        if (index >= ShopManager.Instance.numberCardCount)
        {
            ShowLockedState();
            SetupUnlockButton();
            return;
        }

        boundCard = item.cardData;
        // 显示正常卡牌
        ShowUnlockedState();

        // 清理旧内容
        ClearCardContent();

        // 防御性检查：cardContentRoot 必须存在
        if (cardContentRoot == null)
        {
            Debug.LogError("ShopNumberCardSlot: cardContentRoot 未绑定！无法显示卡牌内容（检查 prefab 的 Inspector）");
            ShowLockedState();
            return;
        }
        // 选择工厂：优先使用本组件配置，其次回退到 UIManager 的全局库（便于 prefab 未赋值时仍能工作）
        NumberCardUIFactory factory = numberCardLibrary;
        if (factory == null && UIManager.Instance != null)
            factory = UIManager.Instance.numberCardLibrary;

        if (factory == null)
        {
            Debug.LogError("ShopNumberCardSlot: 未配置 NumberCardUIFactory（本槽位与 UIManager 均未设置）。无法获取卡牌 Prefab。");
            ShowLockedState();
            return;
        }

        // 防御性检查：实例对象及其数据
        if (item.cardData == null || item.cardData.cardData == null)
        {
            Debug.LogError($"ShopNumberCardSlot: slot {index} 的 ShopItem 数据不完整（NumberCardInstance 或其 cardData 为 null）。");
            ShowLockedState();
            return;
        }
        // 获取对应布局的 Prefab
        GameObject prefab = factory.GetPrefab(item.cardData.cardData.layoutType);
        if (prefab != null)
        {
            // 生成卡牌主体
            GameObject cardGo = Instantiate(prefab, cardContentRoot);
            cardGo.transform.localScale = Vector3.one;
            cardGo.transform.localPosition = Vector3.zero;
            cardGo.transform.localRotation = Quaternion.identity;
            cardGo.SetActive(true);

            // 绑定数据
            var view = cardGo.GetComponent<NumberCardLayoutView>();
            if (view != null)
            {
                // 使用 BindInstance 而不是 Bind，这样可以显示当前值和颜色
                view.BindInstance(item.cardData);
            }
            else
            {
                // 调试信息：列出此物体上的所有组件
                var allComponents = cardGo.GetComponentsInChildren<MonoBehaviour>();
                string componentList = "";
                foreach (var comp in allComponents)
                    componentList += comp.GetType().Name + ", ";

                Debug.LogError($"[ShopNumberCardSlot] 槽位 {index}: 卡牌 Prefab 缺少 NumberCardLayoutView！现有组件: {componentList}");
            }
        }
        else
        {
            Debug.LogError($"找不到布局类型 {item.cardData.cardData.layoutType} 的 Prefab！（检查 NumberCardUIFactory 是否包含此布局）");
            ShowLockedState();
            return;
        }

        // 设置价格
        UpdatePriceDisplay(item.price, item.sold);

        // 设置购买按钮
        SetupBuyButton(item.sold);
    }

    /// <summary>
    /// 更新价格显示
    /// </summary>
    void UpdatePriceDisplay(long price, bool sold)
    {
        if (priceText == null)
        {
            Debug.LogWarning($"[ShopNumberCardSlot] 槽位 {slotIndex}: priceText 未绑定");
            return;
        }
        if (sold)
        {
            priceText.text = "已售出";
            priceText.color = Color.gray;
        }
        else
        {
            priceText.text = $"${price}";
            priceText.color = Color.black;
        }
    }

    /// <summary>
    /// 设置购买按钮
    /// </summary>
    void SetupBuyButton(bool sold)
    {
        if (buyButton == null)
        {
            Debug.LogWarning($"[ShopNumberCardSlot] 槽位 {slotIndex}: buyButton 未绑定");
            return;
        }
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
    /// 设置解锁按钮
    /// </summary>
    void SetupUnlockButton()
    {
        if (unlockButton == null) return;

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
        if (unlockCostText == null) return;

        long unlockCost = ShopManager.Instance.CalculateNumberSlotUnlockCost();
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
        bool success = ShopManager.Instance.TryUnlockNumberSlot();

        if (success)
        {
            // 解锁成功，刷新商店UI
            //ShowUnlockedState();
            Debug.Log("数字卡槽位解锁成功");
        }
        else
        {
            Debug.LogWarning("数字卡槽位解锁失败");
        }
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
