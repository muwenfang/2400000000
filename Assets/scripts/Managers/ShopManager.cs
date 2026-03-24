using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random; // 明确指定使用 UnityEngine.Random
using System.Numerics;
/// <summary>
/// 商店系统。读取database中的商品信息，读取玩家信息，处理购买逻辑
/// </summary>


//商店购买系统
[System.Serializable]
public class ShopItem<T>
{
    public T cardData;
    public long price;
    public bool sold;

    public ShopItem(T data, long price)
    {
        this.cardData = data;
        this.price = price;
        this.sold = false;
    }
}
//商店抽卡与展示
public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance;

    void Awake()
    {
        Instance = this;
    }

    [Header("配置")]
    // 最大能购买的数量
    public int MaxnumberCardCount = 6;
    public int MaxformulaCardCount = 2;
    public int MaxBlessingCardCount = 4;
    // 当前能购买的数量
    public int numberCardCount = 2;
    public int formulaCardCount = 1;
    public int blessingCardCount = 2;

    //刷新次数
    public int refreshCount = 0;

    //删除卡牌相关配置
    [Header("删除功能配置")]
    public Button deleteCardButton;          // 删除卡牌按钮
    public Text deleteCardCostText;          // 显示删除消耗的文本
    public int totalRemovedNumberCards = 0;
    public int baseNumberCardRemoveCost = 5;

    [Header("槽位解锁配置")]
    public int baseNumberSlotUnlockCost = 10; // 数字卡槽位基础解锁消耗
    public int baseFormulaSlotUnlockCost = 20; // 公式卡槽位基础解锁消耗
    public int baseBlessingSlotUnlockCost = 2000;
    public int numberSlotUnlockTimes = 0; // 数字卡已解锁次数
    public int formulaSlotUnlockTimes = 0; // 公式卡已解锁次数
    public int blessingSlotUnlockTimes = 0;

    [Tooltip("公式卡库 - 拖入 FormulaCardLibrary 资源")]
    public FormulaCardLibrary formulaCardLibrary;

    [Header("卡牌库引用")]
    public NumberCardLibrary numberCardLibrary; // 数字卡库的引用

    [Header("祝福系统")]
    [Tooltip("祝福卡库 - 拖入 BlessingLibrary 资源")]
    public BlessingLibrary blessingLibrary;

    [Header("本次商店商品")]
    public List<ShopItem<NumberCardInstance>> shopNumberCards = new();
    public List<ShopItem<FormulaCardData>> shopFormulaCards = new();
    public List<ShopItem<BlessingData>> shopBlessings = new();

    /// <summary>
    /// 本回合已在当前商店刷新中显示过的祝福ID（用于CurrentRoundOnly类型）
    /// </summary>
    private HashSet<int> currentRoundDisplayedBlessings = new HashSet<int>();


    public void OpenShop()
    {
        GenerateNumberCards();
        GenerateFormulaCards();
        GenerateBlessings();
        // ---通知 UI 刷新 ---
        UIManager.Instance.RefreshShopUI();
    }
    public void InitializeShop()
    {
        numberSlotUnlockTimes = 0; // 数字卡已解锁次数
        formulaSlotUnlockTimes = 0; // 公式卡已解锁次数
        blessingSlotUnlockTimes = 0;
        numberCardCount = 2;
        formulaCardCount = 1;
        blessingCardCount = 2;
        OpenShop();
    }
    /// <summary>
    /// 生成数字卡商品
    /// </summary>
    void GenerateNumberCards()
    {
        shopNumberCards.Clear();

        // 验证库是否存在
        if (numberCardLibrary == null || numberCardLibrary.allCards == null || numberCardLibrary.allCards.Count == 0)
        {
            Debug.LogError("NumberCardLibrary 未设置或为空！");
            return;
        }

        // 获取价格乘数（如果有祝福影响价格的话）
        float priceMultiplier = BlessingManager.Instance != null
         ? BlessingManager.Instance.GetCurrentPriceMultiplier()
         : 1.0f;

        // 生成所有槽位（包括锁定的）
        for (int i = 0; i < MaxnumberCardCount; i++)
        {
            // 未锁定槽位：随机抽取一张卡
             int randomIndex = Random.Range(0, numberCardLibrary.allCards.Count);
             NumberCardData randomCard = numberCardLibrary.allCards[randomIndex];

            // 推断布局类型
            randomCard.layoutType = InferLayoutType(randomCard);

            // 创建实例并计算价格
            NumberCardInstance instance = new NumberCardInstance(randomCard);
            long price = instance.GetNumberCardPrice(randomCard);

            shopNumberCards.Add(new ShopItem<NumberCardInstance>(instance, price));
            Debug.Log($"槽位{i}：{randomCard.cardName}，价格 {price}");
            

        }
    }
    /// <summary>
    /// 根据卡牌的逻辑类型推断布局类型
    /// </summary>
    private NumberCardLayoutType InferLayoutType(NumberCardData card)
    {
        switch (card.logicalType)
        {
            case NumberCardData.LogicalType.Normal:
                return NumberCardLayoutType.Single;

            case NumberCardData.LogicalType.Addition:
                return NumberCardLayoutType.Add_AB;

            case NumberCardData.LogicalType.Multiplication:
                return NumberCardLayoutType.Multiply_AB;

            case NumberCardData.LogicalType.Power:
                return NumberCardLayoutType.Composite_AB;

            default:
                return NumberCardLayoutType.Single;
        }
    }

    /// <summary>
    /// 生成公式卡商品
    /// </summary>
    void GenerateFormulaCards()
    {
        shopFormulaCards.Clear();

        // 验证库是否存在
        if (formulaCardLibrary == null || formulaCardLibrary.allCards == null || formulaCardLibrary.allCards.Count == 0)
        {
            Debug.LogError("FormulaCardLibrary 未设置或为空！");
            return;
        }

        // 获取价格乘数（如果有祝福影响价格的话）
        float priceMultiplier = BlessingManager.Instance != null
            ? BlessingManager.Instance.GetCurrentPriceMultiplier()
            : 1.0f;

        // 创建临时池，避免重复抽取
        List<FormulaCardData> tempPool = new List<FormulaCardData>(formulaCardLibrary.allCards);

        // 生成所有槽位
        for (int i = 0; i < MaxformulaCardCount; i++)
        {
                // 未锁定槽位：随机抽取
                int randomIndex = Random.Range(0, tempPool.Count);
                FormulaCardData randomCard = tempPool[randomIndex];
                tempPool.RemoveAt(randomIndex); // 避免重复

                shopFormulaCards.Add(new ShopItem<FormulaCardData>(randomCard, randomCard.CardPrice));
                Debug.Log($"槽位{i}：{randomCard.Name}，价格 {randomCard.CardPrice}");
            
        }
    }

    /// <summary>
    /// 生成祝福商品
    /// </summary>
    void GenerateBlessings()
    {
        shopBlessings.Clear();

        if (blessingLibrary == null || blessingLibrary.GetAllBlessings().Count == 0)
        {
            Debug.LogError("BlessingLibrary 未设置或为空！");
            return;
        }

        // 获取价格乘数（如果有祝福影响价格的话）
        float priceMultiplier = BlessingManager.Instance != null
        ? BlessingManager.Instance.GetCurrentPriceMultiplier()
        : 1.0f;

        // 构建可用祝福池（根据刷新行为过滤）
        List<BlessingData> availableBlessings = BuildAvailableBlessingPool();
        // 生成4个槽位（包括锁定的）
        for (int i = 0; i < MaxBlessingCardCount; i++)
        {
            // 如果是解锁的槽位
            if (i < blessingCardCount)
            {
                BlessingData selectedBlessing = null;

                // 从可用池中选择祝福
                if (availableBlessings.Count > 0)
                {
                    int randomIndex = Random.Range(0, availableBlessings.Count);
                    selectedBlessing = availableBlessings[randomIndex];

                    // 移除已选中的祝福（除非是 AlwaysRefresh 类型）
                    // CurrentRoundOnly 和 NeverRefresh 类型在本次刷新中只能出现一次
                    if (selectedBlessing.refreshBehavior != BlessingData.RefreshBehavior.AlwaysRefresh)
                    {
                        availableBlessings.RemoveAt(randomIndex);
                        currentRoundDisplayedBlessings.Add(selectedBlessing.blessingId);
                        Debug.Log($"[ShopManager] 祝福 '{selectedBlessing.blessingName}' 已在本次刷新中显示，标记为已显示");
                    }

                    // 计算价格
                    int currentCount = BlessingManager.Instance.GetBlessingCount(selectedBlessing.blessingId);
                    int price = selectedBlessing.CalculatePrice(currentCount, priceMultiplier);

                    shopBlessings.Add(new ShopItem<BlessingData>(selectedBlessing, price));
                    Debug.Log($"祝福槽位{i}：{selectedBlessing.blessingName}（{selectedBlessing.refreshBehavior}），价格 {price}");
                }
                else
                {
                    // 可用祝福不足，添加空槽位
                    shopBlessings.Add(new ShopItem<BlessingData>(null, 0));
                    Debug.LogWarning($"[ShopManager] 可用祝福不足！槽位{i}无法填充");
                }
            }
            else
            {
                // 锁定槽位：添加占位符（null）
                shopBlessings.Add(new ShopItem<BlessingData>(null, 0));
            }
        }
    }

    /// <summary>
    /// 构建可用祝福池 - 根据刷新行为和购买历史过滤
    /// </summary>
    private List<BlessingData> BuildAvailableBlessingPool()
    {
        List<BlessingData> availableBlessings = new List<BlessingData>();
        List<BlessingData> allBlessings = blessingLibrary.GetAllBlessings();

        foreach (var blessing in allBlessings)
        {
            if (blessing == null) continue;

            bool isAvailable = false;

            switch (blessing.refreshBehavior)
            {
                case BlessingData.RefreshBehavior.AlwaysRefresh:
                    // 总是可用
                    isAvailable = true;
                    break;

                case BlessingData.RefreshBehavior.NeverRefresh:
                    // 仅当未曾购买时可用
                    isAvailable = !BlessingManager.Instance.HasBlessingEverBeenPurchased(blessing.blessingId);
                    break;

                case BlessingData.RefreshBehavior.CurrentRoundOnly:
                    // 仅当本次刷新中未显示过时可用
                    isAvailable = !currentRoundDisplayedBlessings.Contains(blessing.blessingId);
                    break;
            }

            if (isAvailable)
            {
                availableBlessings.Add(blessing);
                Debug.Log($"[ShopManager] 祝福 '{blessing.blessingName}' 可用（{blessing.refreshBehavior}）");
            }
            else
            {
                Debug.Log($"[ShopManager] 祝福 '{blessing.blessingName}' 不可用（{blessing.refreshBehavior}）");
            }
        }

        return availableBlessings;
    }

    /// <summary>
    /// 重置本回合显示过的祝福
    /// </summary>
    private void ResetCurrentRoundBlessings()
    {
        currentRoundDisplayedBlessings.Clear();
        Debug.Log("[ShopManager] 本回合已显示的祝福记录已清空");
    }

    public bool TryBuyNumberCard(ShopItem<NumberCardInstance> item)
    {
        if (item == null || item.cardData == null)
        {
            Debug.Log("槽位已锁定，无法购买");
            return false;
        }
        if (item.sold) 
        {
            Debug.Log("商品已售出");
            return false; 
        }
        if (GameManager.Instance.currentPoints < item.price)
        {
            Debug.Log("n点数不足，无法购买");
            return false;
        }
        
        GameManager.Instance.AddPoints(-item.price);//扣除点数
        
        // item.cardData 现在是 Instance，所以要访问 .cardData.cardData.cardName
        Debug.Log($"购买成功: {item.cardData.cardData.cardName}");

        // 添加到背包
        // 注意：item.cardData 是 NumberCardInstance 类型
        // item.cardData.cardData 是 NumberCardData (ScriptableObject) 类型
        PlayerCardInventory.Instance.AddNumberCard(item.cardData.cardData);

        item.sold = true;
        return true;
    }
    public bool TryBuyFormulaCard(ShopItem<FormulaCardData> item)
    {
        if (item == null || item.cardData == null)
        {
            Debug.Log("槽位已锁定，无法购买");
            return false;
        }

        if (item.sold)
        {
            Debug.Log("商品已售出");
            return false;
        }
        
        if (GameManager.Instance.currentPoints < item.price)
        {
            Debug.Log("f点数不足，无法购买");
            return false;
        }
        PlayerCardInventory.Instance.AddFormulaCard(item.cardData);
        GameManager.Instance.AddPoints(-item.price);
        Debug.Log("购买成功");
        CardManager.Instance.SyncDeckFromInventory();

        item.sold = true;
        return true;
    }
    /// <summary>
    /// 尝试购买祝福
    /// </summary>
    public bool TryBuyBlessing(ShopItem<BlessingData> item)
    {
        if (item == null || item.cardData == null)
        {
            Debug.LogWarning("祝福商品为空");
            return false;
        }

        if (item.sold)
        {
            Debug.LogWarning("祝福已被购买（本回合）");
            return false;
        }

        // 使用 BlessingManager 的购买逻辑
        bool purchaseSuccess = BlessingManager.Instance.TryBuyBlessing(item.cardData);

        if (purchaseSuccess)
        {
            item.sold = true;
            // 刷新商店显示
            UIManager.Instance.RefreshShopUI();
        }

        return purchaseSuccess;
    }
    //商店刷新
    public void RefreshShop()
    {
        int currentRound = GameManager.Instance.currentRound;
        long roundSquare = (long)Mathf.Pow(currentRound, 2);
        long powerOfTwo = (long)Mathf.Pow(2, refreshCount);
        long refreshCost = roundSquare * powerOfTwo;///计算刷新需要的点数

        // 如果拥有丰盈宝库祝福，刷新费用为0
        if (BlessingManager.Instance.ownedBlessings[26] == 1)
        {
            refreshCost = 0;
        }

        if (GameManager.Instance.currentPoints < refreshCost)
        {
            Debug.Log("点数不足，无法刷新");
            return;
        }
        GameManager.Instance.AddPoints(-refreshCost);
        refreshCount++;//刷新次数应该每回合重置
        
        OpenShop();
    }

    public void CloseShop() 
    {
        // 隐藏商店面板
        if (UIManager.Instance.shopPanel != null)
        {
            UIManager.Instance.shopPanel.SetActive(false);
        }
        //清空祝福已购买标记（下次商店刷新时重新生成）
        foreach (var item in shopBlessings)
        {
            item.sold = false;
        }

        //重置刷新次数
        refreshCount = 0;
    }

    #region 数字卡删除逻辑
    /// <summary>
    /// 计算删除数字卡卡牌的消耗点数
    /// </summary>
    public BigInteger CalculateNumberRemoveCost()
    {
        BigInteger powerOfTwo = BigInteger.Pow(2, totalRemovedNumberCards);
        return baseNumberCardRemoveCost * powerOfTwo;
    }

    /// <summary>
    /// 初始化删除按钮
    /// </summary>
    private void InitializeDeleteButton()
    {
        if (deleteCardButton != null)
        {
            deleteCardButton.onClick.AddListener(OnDeleteCardButtonClicked);
            Debug.Log("[ShopManager] 删除按钮已初始化");
        }
        else
        {
            Debug.LogWarning("[ShopManager] deleteCardButton 未设置");
        }
    }

    /// <summary>
    /// 删除按钮点击事件
    /// </summary>
    private void OnDeleteCardButtonClicked()
    {
        Debug.Log("[ShopManager] 用户点击删除卡牌按钮");

        // 更新删除消耗显示
        UpdateDeleteCardCostDisplay();

        // 打开卡牌选择界面
        OpenCardDeletionMode();
    }

    /// <summary>
    /// 打开卡牌删除模式（显示三个界面的所有卡牌）
    /// </summary>
    private void OpenCardDeletionMode()
    {
        if (CardSelectionManager.Instance == null)
        {
            Debug.LogError("[ShopManager] CardSelectionManager 未初始化");
            return;
        }

        Debug.Log("[ShopManager] 进入卡牌删除模式");

        // 开启卡牌选择模式，支持删除任意卡牌
        CardSelectionManager.Instance.StartCardSelection(
            CardSelectionManager.SelectionMode.RemoveCard,
            OnCardSelectedForDeletion
        );
    }

    /// <summary>
    /// 卡牌删除选择的回调
    /// </summary>
    private void OnCardSelectedForDeletion(object selectedObject)
    {
        if (selectedObject == null)
        {
            Debug.LogError("[ShopManager] 选择的卡牌为空");
            return;
        }

        // 判断卡牌类型并执行对应删除逻辑
        if (selectedObject is NumberCardInstance numberCard)
        {
            HandleNumberCardDeletion(numberCard);
        }
        else if (selectedObject is FormulaCardData formulaCard)
        {
            HandleFormulaCardDeletion(formulaCard);
        }
        else
        {
            Debug.LogError($"[ShopManager] 未知的卡牌类型：{selectedObject.GetType().Name}");
        }
    }

    /// <summary>
    /// 处理数字卡删除
    /// </summary>
    private void HandleNumberCardDeletion(NumberCardInstance selectedCard)
    {
        Debug.Log($"[ShopManager] 用户选择删除数字卡：{selectedCard.cardData.cardName}");

        // 执行删除逻辑
        if (TryRemoveNumberCard(selectedCard))
        {
            Debug.Log("[ShopManager] 数字卡删除成功");
            RefreshUI();
        }
        else
        {
            Debug.LogWarning("[ShopManager] 数字卡删除失败");
        }
    }

    /// <summary>
    /// 处理公式卡删除
    /// </summary>
    private void HandleFormulaCardDeletion(FormulaCardData selectedCard)
    {
        Debug.Log($"[ShopManager] 用户选择删除公式卡：{selectedCard.Name}");

        if (TryRemoveFormulaCard(selectedCard))
        {
            Debug.Log("[ShopManager] 公式卡删除成功");
            RefreshUI();
        }
        else
        {
            Debug.LogWarning("[ShopManager] 公式卡删除失败");
        }
    }

    ///// <summary>
    ///// 处理祝福卡删除
    ///// </summary>
    //private void HandleBlessingCardDeletion(BlessingData selectedCard)
    //{
    //    Debug.Log($"[ShopManager] 用户选择删除祝福卡：{selectedCard.blessingName}");

    //    if (TryRemoveBlessingCard(selectedCard))
    //    {
    //        Debug.Log("[ShopManager] 祝福卡删除成功");
    //        RefreshUI();
    //    }
    //    else
    //    {
    //        Debug.LogWarning("[ShopManager] 祝福卡删除失败");
    //    }
    //}
    ///删除对应的卡牌实例
    public bool TryRemoveNumberCard(NumberCardInstance NumbercardToRemove)///这里的NumbercardToRemove应该是要跟UI关联起来（？），这个我不太会做
    {
        if (NumbercardToRemove == null)///检测是否接收到实例
        {
            Debug.LogWarning("没有接受到要删除的数字卡");
            return false;
        }

        if (!PlayerCardInventory.Instance.numberCards.Contains(NumbercardToRemove))///检测是否接收到卡组中没有的数字卡
        {
            Debug.LogWarning("接收到了玩家未拥有的数字卡");
            return false;
        }

        int minRequireCards = 6;///设定最少需要保留几张数字卡，目前设定为6张
        if (PlayerCardInventory.Instance.numberCards.Count <= minRequireCards)
        {
            Debug.LogWarning("卡组中数字卡过少");
            return false;
        }
        BigInteger NumberCardremoveCost = CalculateNumberRemoveCost();///计算所需点数
        if (GameManager.Instance.currentPoints < NumberCardremoveCost)///检查点数是否足够
        {
            Debug.LogWarning("点数不足");
            return false;
        }
        ///删除卡组中的对应数字卡
        PlayerCardInventory.Instance.numberCards.Remove(NumbercardToRemove);
        GameManager.Instance.AddPoints(-NumberCardremoveCost);
        totalRemovedNumberCards++;
        CardManager.Instance.SyncDeckFromInventory();///应该要调用这个方法同步牌堆（？）
        return true;
    }
    /// <summary>
    /// 删除公式卡
    /// </summary>
    private bool TryRemoveFormulaCard(FormulaCardData cardToRemove)
    {
        if (cardToRemove == null)
        {
            Debug.LogWarning("[ShopManager] 要删除的公式卡为空");
            return false;
        }

        if (!PlayerCardInventory.Instance.formulaCards.Contains(cardToRemove))
        {
            Debug.LogWarning("[ShopManager] 公式卡不在库存中");
            return false;
        }

        // 计算删除消耗（公式卡消耗 = 其价格的50%）
        BigInteger removeCost = (BigInteger)(cardToRemove.CardPrice * 0.5f);

        // 检查点数
        if (GameManager.Instance.currentPoints < removeCost)
        {
            Debug.LogWarning($"[ShopManager] 点数不足！需要{removeCost}，当前{GameManager.Instance.currentPoints}");
            return false;
        }

        // 检查卡牌数量（至少保留1张）
        if (PlayerCardInventory.Instance.formulaCards.Count <= 1)
        {
            Debug.LogWarning("[ShopManager] 公式卡数量不足，不能删除");
            return false;
        }

        // 扣除点数
        GameManager.Instance.AddPoints(-removeCost);

        // 删除卡牌
        PlayerCardInventory.Instance.formulaCards.Remove(cardToRemove);

        Debug.Log($"[ShopManager] 删除公式卡：{cardToRemove.Name}，消耗{removeCost}点");
        return true;
    }

    /// <summary>
    /// 更新删除卡牌消耗的显示
    /// </summary>
    private void UpdateDeleteCardCostDisplay()
    {
        if (deleteCardCostText == null) return;

        BigInteger numberCardCost = CalculateNumberRemoveCost();

        // 显示数字卡的删除消耗（公式卡和祝福卡的消耗动态计算）
        deleteCardCostText.text = $"删除消耗（数字卡）: {numberCardCost} 点";

        Debug.Log($"[ShopManager] 删除消耗已更新：{numberCardCost}");
    }
    /// <summary>
    /// 获取删除功能是否可用
    /// </summary>
    public bool IsDeleteCardAvailable()
    {
        // 检查点数（以数字卡最小消耗为判断）
        BigInteger minCost = CalculateNumberRemoveCost();
        if (GameManager.Instance.currentPoints < minCost)
        {
            return false;
        }

        // 检查是否有可删除的卡牌
        int totalCards = PlayerCardInventory.Instance.numberCards.Count +
                        PlayerCardInventory.Instance.formulaCards.Count;

        if (totalCards <= 7)  // 保留6张数字卡 + 1张公式卡的最少需求
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 更新删除按钮的可用性和显示
    /// </summary>
    public void UpdateDeleteButtonState()
    {
        if (deleteCardButton == null) return;

        bool isAvailable = IsDeleteCardAvailable();
        deleteCardButton.interactable = isAvailable;

        // 更新按钮文本颜色
        Image buttonImage = deleteCardButton.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = isAvailable ? Color.white : Color.gray;
        }

        if (!isAvailable)
        {
            Debug.Log("[ShopManager] 删除功能暂不可用");
        }
    }

    /// <summary>
    /// 刷新UI显示
    /// </summary>
    private void RefreshUI()
    {
        // 更新删除消耗显示
        UpdateDeleteCardCostDisplay();

        // 更新删除按钮状态
        UpdateDeleteButtonState();

        // 通过 UIManager 刷新显示
        if (UIManager.Instance != null)
        {
            UIManager.Instance.RefreshGameUI();
            UIManager.Instance.RefreshShopUI();
            Debug.Log("[ShopManager] UI 已刷新");
        }
    }

    /// <summary>
    /// 显示删除功能提示
    /// </summary>
    public void ShowDeleteCardTip()
    {
        BigInteger cost = CalculateNumberRemoveCost();
        string tip = $"删除数字卡，消耗 {cost} 点。\n已删除卡牌数：{totalRemovedNumberCards}";
        Debug.Log($"[ShopManager] {tip}");

        // 可以在这里显示 Tooltip
        if (UIManager.Instance != null)
        {
            // UIManager.Instance.ShowTip(tip);  // 如果有 ShowTip 方法
        }
    }
    /// <summary>
    /// 点击删除时调用此方法
    /// </summary>
    public void OnEnterRemovalMode()
    {
        InitializeDeleteButton();
        UpdateDeleteCardCostDisplay();
        UpdateDeleteButtonState();
        Debug.Log("[ShopManager] 进入商店，删除功能已初始化");
    }

    /// <summary>
    /// 当离开商店时调用此方法
    /// </summary>
    public void OnExitRemovalMode()
    {
        if (deleteCardButton != null)
        {
            deleteCardButton.onClick.RemoveListener(OnDeleteCardButtonClicked);
        }
        Debug.Log("[ShopManager] 离开商店，删除功能已清理");
    }
    #endregion



    #region 槽位解锁逻辑
    /// <summary>
    /// 计算数字卡槽位解锁消耗
    /// </summary>
    public long CalculateNumberSlotUnlockCost()
    {
        long powerOfTwo = (long)Mathf.Pow(2, numberSlotUnlockTimes);
        return baseNumberSlotUnlockCost * powerOfTwo;
    }

    /// <summary>
    /// 计算公式卡槽位解锁消耗
    /// </summary>
    public long CalculateFormulaSlotUnlockCost()
    {
        long powerOfTwo = (long)Mathf.Pow(2, formulaSlotUnlockTimes);
        return baseFormulaSlotUnlockCost * powerOfTwo;
    }

    /// <summary>
    /// 获取下一个可解锁的数字卡槽位编号
    /// </summary>
    public int GetNextUnlockedNumberSlot()
    {
        return numberCardCount;
    }

    /// <summary>
    /// 获取下一个可解锁的公式卡槽位编号
    /// </summary>
    public int GetNextUnlockedFormulaSlot()
    {
        return formulaCardCount;
    }

    /// <summary>
    /// 尝试解锁数字卡槽位
    /// </summary>
    /// <returns>解锁成功返回true，失败返回false</returns>
    public bool TryUnlockNumberSlot()
    {
        // 判定1：是否已达到最大槽位
        if (numberCardCount >= MaxnumberCardCount)
        {
            Debug.LogWarning("数字卡槽位已解锁至最大值，无法继续解锁");
            return false;
        }

        // 判定2：计算消耗并校验点数
        long unlockCost = CalculateNumberSlotUnlockCost();
        if (GameManager.Instance.currentPoints < unlockCost)
        {
            Debug.LogWarning($"数字卡槽位解锁失败：点数不足，需要{unlockCost}，当前{GameManager.Instance.currentPoints}");
            return false;
        }

        // 执行解锁：扣除点数、更新解锁次数、增加可购买槽位数量
        GameManager.Instance.AddPoints(-unlockCost);
        numberSlotUnlockTimes++;

        int newSlotIndex = numberCardCount;  // 记录新槽位的索引
        numberCardCount++;

        Debug.Log($"数字卡槽位解锁成功！当前可购买数量：{numberCardCount}，累计解锁次数：{numberSlotUnlockTimes}");

        // 只生成新槽位的卡牌，不改变现有卡牌
        GenerateNewNumberCardSlot(newSlotIndex);

        // 只刷新UI，不改变卡牌数值
        UIManager.Instance.RefreshShopUI();

        return true;
    }
    /// <summary>
    /// 计算祝福卡槽位解锁消耗
    /// </summary>
    public long CalculateBlessingSlotUnlockCost()
    {
        long powerOfTwo = (long)Mathf.Pow(25, blessingSlotUnlockTimes);
        return baseBlessingSlotUnlockCost * powerOfTwo;
    }
    /// <summary>
    /// 尝试解锁公式卡槽位
    /// </summary>
    /// <returns>解锁成功返回true，失败返回false</returns>
    public bool TryUnlockFormulaSlot()
    {
        // 判定1：是否已达到最大槽位
        if (formulaCardCount >= MaxformulaCardCount)
        {
            Debug.LogWarning("公式卡槽位已解锁至最大值，无法继续解锁");
            return false;
        }

        // 判定2：计算消耗并校验点数
        long unlockCost = CalculateFormulaSlotUnlockCost();
        if (GameManager.Instance.currentPoints < unlockCost)
        {
            Debug.LogWarning($"公式卡槽位解锁失败：点数不足，需要{unlockCost}，当前{GameManager.Instance.currentPoints}");
            return false;
        }

        // 执行解锁：扣除点数、更新解锁次数、增加可购买槽位数量
        GameManager.Instance.AddPoints(-unlockCost);
        formulaSlotUnlockTimes++;
        int newSlotIndex = formulaCardCount;  // 记录新槽位的索引
        formulaCardCount++;

        Debug.Log($"公式卡槽位解锁成功！当前可购买数量：{formulaCardCount}，累计解锁次数：{formulaSlotUnlockTimes}");

        //只生成新槽位的卡牌，不改变现有卡牌
        GenerateNewFormulaCardSlot(newSlotIndex);

        // 只刷新UI，不改变卡牌数值
        UIManager.Instance.RefreshShopUI();

        return true;
    }
    /// <summary>
    /// 尝试解锁祝福卡槽位
    /// </summary>
    public bool TryUnlockBlessingSlot()
    {
        // 判定1：是否已达到最大槽位
        if (blessingCardCount >= MaxBlessingCardCount)
        {
            Debug.LogWarning("祝福卡槽位已解锁至最大值，无法继续解锁");
            return false;
        }

        // 判定2：计算消耗并校验点数
        long unlockCost = CalculateBlessingSlotUnlockCost();
        if (GameManager.Instance.currentPoints < unlockCost)
        {
            Debug.LogWarning($"祝福卡槽位解锁失败：点数不足，需要{unlockCost}，当前{GameManager.Instance.currentPoints}");
            return false;
        }

        // 执行解锁：扣除点数、更新解锁次数、增加可购买槽位数量
        GameManager.Instance.AddPoints(-unlockCost);
        blessingSlotUnlockTimes++;

        int newSlotIndex = blessingCardCount;  // 记录新槽位的索引
        blessingCardCount++;

        Debug.Log($"祝福卡槽位解锁成功！当前可购买数量：{blessingCardCount}，累计解锁次数：{blessingSlotUnlockTimes}");

        // 只生成新槽位的祝福，不改变现有祝福
        GenerateNewBlessingSlot(newSlotIndex);

        // 刷新 UI
        UIManager.Instance.RefreshShopUI();

        return true;
    }
    #endregion
    /// <summary>
    ///只生成一个新的公式卡槽位（用于解锁时调用）
    /// 不改变现有的卡牌数值
    /// </summary>
    void GenerateNewFormulaCardSlot(int slotIndex)
    {
        // 验证库是否存在
        if (formulaCardLibrary == null || formulaCardLibrary.allCards == null || formulaCardLibrary.allCards.Count == 0)
        {
            Debug.LogError("FormulaCardLibrary 未设置或为空！");
            return;
        }

        // 只生成一个新槽位，避免与现有卡牌重复
        FormulaCardData randomCard = formulaCardLibrary.GetRandomCard();

        // 将新卡牌添加到列表
        if (slotIndex < shopFormulaCards.Count)
        {
            shopFormulaCards[slotIndex] = new ShopItem<FormulaCardData>(randomCard, randomCard.CardPrice);
        }
        else
        {
            shopFormulaCards.Add(new ShopItem<FormulaCardData>(randomCard, randomCard.CardPrice));
        }

        Debug.Log($"槽位{slotIndex}：{randomCard.Name}，价格 {randomCard.CardPrice}");
    }
    /// <summary>
    ///只生成一个新的数字卡槽位（用于解锁时调用）
    /// 不改变现有的卡牌数值
    /// </summary>
    void GenerateNewNumberCardSlot(int slotIndex)
    {
        // 验证库是否存在
        if (numberCardLibrary == null || numberCardLibrary.allCards == null || numberCardLibrary.allCards.Count == 0)
        {
            Debug.LogError("NumberCardLibrary 未设置或为空！");
            return;
        }

        // 只生成一个新槽位
        int randomIndex = Random.Range(0, numberCardLibrary.allCards.Count);
        NumberCardData randomCard = numberCardLibrary.allCards[randomIndex];

        // 推断布局类型
        randomCard.layoutType = InferLayoutType(randomCard);

        // 创建实例并计算价格
        NumberCardInstance instance = new NumberCardInstance(randomCard);
        long price = instance.GetNumberCardPrice(randomCard);

        // 将新卡牌添加到列表
        if (slotIndex < shopNumberCards.Count)
        {
            shopNumberCards[slotIndex] = new ShopItem<NumberCardInstance>(instance, price);
        }
        else
        {
            shopNumberCards.Add(new ShopItem<NumberCardInstance>(instance, price));
        }

        Debug.Log($"槽位{slotIndex}：{randomCard.cardName}，价格 {price}");
    }
    /// <summary>
    /// 只生成一个新的祝福槽位（用于解锁时调用）
    /// 不改变现有的祝福数值
    /// </summary>
    void GenerateNewBlessingSlot(int slotIndex)
    {
        // 验证库是否存在
        if (blessingLibrary == null || blessingLibrary.GetAllBlessings().Count == 0)
        {
            Debug.LogError("BlessingLibrary 未设置或为空！");
            return;
        }

        float priceMultiplier = BlessingManager.Instance != null
            ? BlessingManager.Instance.GetCurrentPriceMultiplier()
            : 1.0f;

        // 随机获取一个祝福
        BlessingData randomBlessing = blessingLibrary.GetRandomBlessing();

        // 计算价格
        int currentCount = BlessingManager.Instance.GetBlessingCount(randomBlessing.blessingId);
        int price = randomBlessing.CalculatePrice(currentCount, priceMultiplier);

        // 将新祝福添加到列表
        if (slotIndex < shopBlessings.Count)
        {
            shopBlessings[slotIndex] = new ShopItem<BlessingData>(randomBlessing, price);
        }
        else
        {
            shopBlessings.Add(new ShopItem<BlessingData>(randomBlessing, price));
        }

        Debug.Log($"祝福槽位{slotIndex}：{randomBlessing.blessingName}，价格 {price}");
    }
}
