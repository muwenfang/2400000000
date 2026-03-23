using System;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;

/// <summary>
/// 祝福管理器 - 管理玩家拥有的祝福及其效果
/// </summary>
public class BlessingManager : MonoBehaviour
{
    public static BlessingManager Instance;

    [Header("祝福库引用")]
    [Tooltip("祝福卡库 - 拖入 BlessingLibrary 资源")]
    public BlessingLibrary blessingLibrary;

    [Header("玩家已拥有的祝福")]
    // 用字典存储：祝福ID -> 购买次数
    private Dictionary<int, int> ownedBlessings = new Dictionary<int, int>();

    // 跟踪已永久购买过的祝福（用于NeverRefresh类型）
    private HashSet<int> blessingsEverPurchased = new HashSet<int>();

    // 用于快速查询特定祝福的购买次数
    private Dictionary<BlessingData.BlessingType, int> blessingTypeCount =
        new Dictionary<BlessingData.BlessingType, int>();

    public List<BlessingInstance> ownedBlessingInstance =new List<BlessingInstance>();

    [Header("祝福效果累积")]
    private float totalMultiplierBonus = 0f; // 倍率加成
    private int totalDialecticalCount = 0;   // 购买次数
    private float AllGodsCount = 0;          // 众神归位数量 
    private int LuckTurnsCount = 0;           //是否激活转运，1是激活
    private bool hasJackpot7 = false;        //是否激活逢7过
    private int CardMasterCount = 0;       //是否激活卡牌大师 

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }

        InitializeBlessingSystem();
    }

    /// <summary>
    /// 初始化祝福系统
    /// </summary>
    private void InitializeBlessingSystem()
    {
        ownedBlessings.Clear();
        blessingTypeCount.Clear();
        totalMultiplierBonus = 0f;
        totalDialecticalCount = 0;
        AllGodsCount = 0;
        LuckTurnsCount = 0;
        CardMasterCount = 0;
        hasJackpot7 = false;
    }

    /// <summary>
    /// 购买祝福
    /// </summary>
    public bool TryBuyBlessing(BlessingData blessingData)
    {
        if (blessingData == null)
        {
            Debug.LogError("祝福数据为空！");
            return false;
        }

        // 计算当前祝福的价格（考虑已购买次数）
        int currentCount = GetBlessingCount(blessingData.blessingId);
        float priceMultiplier = GetCurrentPriceMultiplier();
        int finalPrice = blessingData.CalculatePrice(currentCount, priceMultiplier);

        // 检查点数是否足够
        if (GameManager.Instance.currentPoints < finalPrice)
        {
            Debug.LogWarning($"点数不足！需要{finalPrice}，当前{GameManager.Instance.currentPoints}");
            return false;
        }

        // 扣除点数
        GameManager.Instance.AddPoints(-finalPrice);

        // 记录祝福购买，ownedBlessings字典中增加购买次数
        if (ownedBlessings.ContainsKey(blessingData.blessingId))
        {
            ownedBlessings[blessingData.blessingId]++;
        }
        else
        {
            ownedBlessings[blessingData.blessingId] = 1;
        }

        // 记录为已购买过（用于NeverRefresh判定）
        blessingsEverPurchased.Add(blessingData.blessingId);

        // 更新类型计数
        if (!blessingTypeCount.ContainsKey(blessingData.blessingType))
        {
            blessingTypeCount[blessingData.blessingType] = 0;
        }
        blessingTypeCount[blessingData.blessingType]++;

        // 创建祝福实例并添加到列表
        ownedBlessingInstance.Add(new BlessingInstance(blessingData, ownedBlessings[blessingData.blessingId]));

        // 应用祝福效果
        ApplyBlessingEffect(blessingData);

        return true;
    }
    /// <summary>
    /// 检查祝福是否已被购买过（用于NeverRefresh判定）
    /// </summary>
    public bool HasBlessingEverBeenPurchased(int blessingId)
    {
        return blessingsEverPurchased.Contains(blessingId);
    }

    /// <summary>
    /// 获取已有祝福数量
    /// </summary>
    private int GetTotalBlessingCount()
    {
        int totalCount = 0;
        foreach (var kvp in ownedBlessings)
        {
            if(kvp.Value > 0)
            {
                totalCount += kvp.Value;
            }
        }
        return totalCount;
    }
    
    /// <summary>
    /// 应用祝福效果
    /// </summary>
    private void ApplyBlessingEffect(BlessingData blessingData)
    {
        switch (blessingData.blessingType)
        {
            case BlessingData.BlessingType.Jackpot7:
                // 逢七过 - 
                hasJackpot7 = true;
                Debug.Log("逢七过效果已激活");
                break;
            
            case BlessingData.BlessingType.AllGodsInPlace:
                // 众神归位 - 效果每回合可能要重新检测祝福数量
                AllGodsCount++;
                Debug.Log("众神归位效果已激活");
                break;
            
            case BlessingData.BlessingType.FinancialMaster:
                // 理财大师 - 效果在回合结束时应用（在 GameManager 中调用）
                Debug.Log("理财大师效果已激活");
                break;

            case BlessingData.BlessingType.MoreMoreBetter:
                // 多多益善 - 复制一张填空卡（需要玩家选择）
                //[todo]
                Debug.Log("多多益善效果已激活，等待玩家选择填空卡");
                break;
            
            case BlessingData.BlessingType.CardMaster:
                // 卡牌大师 - 每张数字卡额外提供1倍率
                CardMasterCount++;
                Debug.Log("卡牌大师效果已激活，提供额外倍率");
                break;

            
            case BlessingData.BlessingType.DialecticalViewpoint:
                // 辩证主义 - 立即应用倍率和点数
                totalDialecticalCount++;
                totalMultiplierBonus += blessingData.effectValue;

                // 给予奖励点数
                GameManager.Instance.AddPoints(blessingData.bonusPoints);

                Debug.Log($"辩证主义效果已激活：倍率+{blessingData.effectValue}，" +
                    $"额外获得{blessingData.bonusPoints}点，价格上升1%");
                break;

            case BlessingData.BlessingType.LuckTurns:
                // 转运 - 无法叠加：你的骰子投到1时，重投一次并将这次的结果作为该骰子的最终判定结果
                LuckTurnsCount = 1;
                Debug.Log("转运效果已激活（无法叠加）：骰子投出1时将重投一次！");
                break;

            case BlessingData.BlessingType.DoubleDown:
                // 倍投 - 倍率+1,价格翻倍
                totalDialecticalCount++;
                totalMultiplierBonus += blessingData.effectValue;
                Debug.Log("倍投效果已激活");
                break;

            case BlessingData.BlessingType.Raise:
                // 加注 - 倍率+1,价格+300
                totalDialecticalCount++;
                totalMultiplierBonus += blessingData.effectValue;
                Debug.Log("加注效果已激活");
                break;

            case BlessingData.BlessingType.SmallCardPack:
                // 小卡牌包 - 立即获得三张随机数字卡
                PlayerCardInventory.Instance.AddRandomNumberCards(3);
                Debug.Log("小卡牌包效果已激活：立即获得三张随机数字卡");
                break;

            case BlessingData.BlessingType.BigCardPack:
                // 大卡牌包 - 立即获得五张随机数字卡
                PlayerCardInventory.Instance.AddRandomNumberCards(5);
                Debug.Log("大卡牌包效果已激活：立即获得五张随机数字卡");
                break;

            case BlessingData.BlessingType.FriendDiscount:
                // 友情折扣 - 不可叠加：所有数字卡、填空卡与祝福的价格-10%(在 ShopManager 中调用)
                totalDialecticalCount++;
                totalMultiplierBonus += blessingData.effectValue;
                Debug.Log("友情折扣效果已激活");
                break;

            case BlessingData.BlessingType.Bless:
                // 眷顾 - 不可叠加：你每拥有一个祝福，所有数字卡、填空卡与祝福的价格-1%
                Debug.Log("眷顾效果已激活");
                break;

            case BlessingData.BlessingType.RichTreasury:
                // 丰盈宝库 - 不可叠加：商店刷新永久免费（在 ShopManager 里调用）
                Debug.Log("丰盈宝库效果已激活");
                break;

            case BlessingData.BlessingType.Empiricism:
                // 经验主义 - 不可叠加：每回合抽取数字卡时先抽取上一回合判定结果最大的数字卡
                Debug.Log("经验主义效果已激活");
                break;

            case BlessingData.BlessingType.CardCheat:
                // 老千祝福 - 激活卡牌选择
                Debug.Log("[BlessingManager] 老千祝福已激活，等待玩家选择数字卡");
                ActivateCardCheatSelection();
                break;

            case BlessingData.BlessingType.QuitGambling:
                // 戒赌 - 可叠加：将数字卡中的一个骰子变为{0}
                Debug.Log("戒赌效果已激活");
                break;

            case BlessingData.BlessingType.RapidActivation:
                // 高效催化 - 可叠加：选择一个数字卡中的绿色数字使其立即+1，祝福“高效催化”的价格翻倍
                Debug.Log("高效催化效果已激活");
                break;

            case BlessingData.BlessingType.GamblingGearUpgraded:
                // 赌具升级 - 可叠加：选择一个骰子使其立即升一级，祝福“赌具升级”的价格翻倍
                Debug.Log("赌具升级效果已激活");
                break;

            case BlessingData.BlessingType.Dyed:
                // 染色 - 可叠加：立即将数字卡的一个普通数字染为绿色，祝福“染色”的价格变为10倍
                Debug.Log("染色效果已激活");
                break;

            case BlessingData.BlessingType.CompulsiveGambler:
                // 狂赌之渊 - 不可叠加，本回合商店不刷新：立即将所有绿色数字变为~20~
                Debug.Log("狂赌之渊效果已激活");
                break;

            case BlessingData.BlessingType.EnergySpread:
                // 能量扩散 - 不可叠加：不参与计算的绿色数字每回合也会+1
                Debug.Log("能量扩散效果已激活");
                break;

            case BlessingData.BlessingType.Utopianism:
                // 空想主义 - 可叠加：立即获得一张未拥有的填空卡；如果获得此祝福时拥有了全部种类的填空卡，你立即获得2400000000点并失去所有“空想主义”
                Debug.Log("空想主义效果已激活");
                break;

            case BlessingData.BlessingType.Pragmatism:
                // 实用主义 - 不可叠加：任意时刻你仅保留价值最高的填空卡并自动删除其它填空卡
                Debug.Log("实用主义效果已激活");
                break;

        }
    }
    /// <summary>
    /// ✨ 激活老千祝福的卡牌选择
    /// </summary>
    private void ActivateCardCheatSelection()
    {
        if (CardSelectionManager.Instance == null)
        {
            Debug.LogError("[BlessingManager] CardSelectionManager 未初始化");
            return;
        }

        // 开启卡牌选择模式
        CardSelectionManager.Instance.StartCardSelection(
            CardSelectionManager.SelectionMode.CardCheat,
            OnCardCheatCardSelected
        );
    }

    /// <summary>
    /// ✨ 老千祝福的卡牌选择回调
    /// </summary>
    private void OnCardCheatCardSelected(object selectedObject)
    {
        // 检查是否是数字卡（应该是）
        if (!(selectedObject is NumberCardInstance selectedCard))
        {
            Debug.LogError("[BlessingManager] 老千祝福：选择的不是数字卡！");
            return;
        }

        if (selectedCard == null)
        {
            Debug.LogError("[BlessingManager] 选择的卡牌为空");
            return;
        }

        Debug.Log($"[BlessingManager] 老千祝福选中数字卡：{selectedCard.cardData.cardName}");

        // 执行老千逻辑
        ApplyCardCheatEffect(selectedCard);
    }

    /// <summary>
    /// 执行老千祝福效果
    /// </summary>
    private void ApplyCardCheatEffect(NumberCardInstance cardToReplace)
    {
        var playerInventory = PlayerCardInventory.Instance;
        if (playerInventory == null)
        {
            Debug.LogError("[BlessingManager] PlayerCardInventory 为空");
            return;
        }

        // 检查卡牌是否在库存中
        if (!playerInventory.numberCards.Contains(cardToReplace))
        {
            Debug.LogWarning("[BlessingManager] 选择的卡牌不在库存中");
            return;
        }

        // 获取卡牌库
        if (playerInventory.numberCardLibrary == null ||
            playerInventory.numberCardLibrary.allCards == null ||
            playerInventory.numberCardLibrary.allCards.Count == 0)
        {
            Debug.LogError("[BlessingManager] 卡牌库为空");
            return;
        }

        // 随机选择新卡牌
        int randomIndex = UnityEngine.Random.Range(0, playerInventory.numberCardLibrary.allCards.Count);
        NumberCardData randomCard = playerInventory.numberCardLibrary.allCards[randomIndex];

        // 替换卡牌
        int oldCardIndex = playerInventory.numberCards.IndexOf(cardToReplace);
        if (oldCardIndex >= 0)
        {
            NumberCardInstance newCardInstance = new NumberCardInstance(randomCard);
            playerInventory.numberCards[oldCardIndex] = newCardInstance;

            Debug.Log($"[BlessingManager] 老千祝福：'{cardToReplace.cardData.cardName}' → '{randomCard.cardName}'");

            // 刷新UI
            if (UIManager.Instance != null)
            {
                UIManager.Instance.RefreshGameUI();
            }
            else
            {
                Debug.LogWarning("[BlessingManager] UIManager 为空，无法刷新UI");
            }
        }
    }
    /// <summary>
    /// 提供转运祝福的激活状态（是否拥有转运祝福）
    /// </summary>
    public bool IsLuckTurnsActive()
    {
        return LuckTurnsCount > 0;
    }

    /// <summary>
    /// 获取特定祝福的购买次数
    /// </summary>
    public int GetBlessingCount(int blessingId)
    {
        return ownedBlessings.ContainsKey(blessingId) ? ownedBlessings[blessingId] : 0;
    }

    /// <summary>
    /// 获取特定类型祝福的购买次数
    /// </summary>
    public int GetBlessingTypeCount(BlessingData.BlessingType type)
    {
        return blessingTypeCount.ContainsKey(type) ? blessingTypeCount[type] : 0;
    }
    
    /// <summary>
    /// 计算“逢七过”的倍率
    /// </summary>
    private float CalculateJackpot7Bonus()
    {
        if (!hasJackpot7) return 0f;
        return 7f; 
    }
    
    /// <summary>
    /// 计算“众神归位”的倍率
    /// </summary>
    private float CalculateAllGodsInPlaceBonus()
    {
        if (AllGodsCount <= 0) return 0f;
        int totalBlessingCount = GetTotalBlessingCount();
        float Godsbonus = totalBlessingCount * AllGodsCount;
        return Godsbonus; 
    }
    
    /// <summary>
    /// 计算"理财大师"效果的额外点数
    /// </summary>
    public BigInteger CalculateFinancialMasterBonus(BigInteger currentPoints)
    {
        int financialMasterCount = GetBlessingTypeCount(BlessingData.BlessingType.FinancialMaster);
        if (financialMasterCount == 0)
            return BigInteger.Zero;

        // 每次购买都叠加效果：额外获得已拥有点数的1%
        BigInteger bonusPerCount = currentPoints / 100; // 1% = 1/100
        BigInteger totalBonus = bonusPerCount * financialMasterCount;

        Debug.Log($"理财大师加成：{currentPoints} × 1% × {financialMasterCount} = {totalBonus}");
        return totalBonus;
    }

    /// <summary>
    /// 获取卡牌大师的额外倍率
    /// </summary>
    private float CalculateCardMasterBonus()
    {
        if (CardMasterCount <= 0) return 0f;
        int numberCardCount = PlayerCardInventory.Instance.GetAllNumberCards().Count;
        float cardMasterBonus = CardMasterCount * numberCardCount;
        return cardMasterBonus;
    }

    /// <summary>
    /// 获取当前总倍率加成（来自所有祝福）
    /// </summary>
    public float GetTotalMultiplierBonus()
    {
        float totalMultiplierBonus = 0f;
        
        ///逢七过的额外倍率
        float Jackpot7Bonus = CalculateJackpot7Bonus();
        totalMultiplierBonus += Jackpot7Bonus;
        
        ///众神归位的额外倍率
        float AllGodsInPlaceBonus = CalculateAllGodsInPlaceBonus();
        totalMultiplierBonus += AllGodsInPlaceBonus; 
        Debug.Log("众神归位+" + AllGodsInPlaceBonus);

        ///卡牌大师的额外倍率
        float cardMasterBonus = CalculateCardMasterBonus();
        totalMultiplierBonus += cardMasterBonus;
        Debug.Log($"卡牌大师倍率加成：{cardMasterBonus}");
        
        return totalMultiplierBonus;
    }

    /// <summary>
    /// 获取当前价格乘数（用于计算商品价格）
    /// 辩证主义：所有卡牌与祝福的价格+1%
    /// </summary>
    public float GetCurrentPriceMultiplier()
    {
        // 每购买一次"辩证主义"，价格上升1%
        float multiplier = Mathf.Pow(1.01f, totalDialecticalCount);
        if (blessingTypeCount[BlessingData.BlessingType.FriendDiscount] == 1)
        { 
            multiplier *= 0.9f; // 友情折扣 - 所有数字卡、填空卡与祝福的价格-10%
        }
        return multiplier;
    }

    /// <summary>
    /// 执行"逢七过"效果 - 判定结果是否符合触发条件
    /// </summary>
    public bool CheckJackpot7Effect(BigInteger score)
    {
        if (!hasJackpot7) return false;
        bool isMultipleOf7 = score % 7 == 0;
        bool ContainsDigit7 = score.ToString().Contains("7");
        bool triggerJackpot7 = isMultipleOf7 || ContainsDigit7;

        if (triggerJackpot7)
        {
            Debug.Log("逢七过触发，本回合得分归0");
        } 
        return triggerJackpot7;
    }
    
    /// <summary>
    /// 执行"多多益善"效果 - 复制指定的填空卡
    /// </summary>
    public void ApplyMoreMoreBetterEffect(FormulaCardData formulaCardToCopy)
    {
        if (formulaCardToCopy == null)
        {
            Debug.LogError("要复制的填空卡为空");
            return;
        }

        int moreMoreCount = GetBlessingTypeCount(BlessingData.BlessingType.MoreMoreBetter);

        // 每次购买都复制一次指定的卡
        for (int i = 0; i < moreMoreCount; i++)
        {
            PlayerCardInventory.Instance.AddFormulaCard(formulaCardToCopy);
            Debug.Log($"已复制填空卡：{formulaCardToCopy.Name}（第{i + 1}次）");
        }

        // 同步牌堆
        CardManager.Instance.SyncDeckFromInventory();
    }

    /// <summary>
    /// 清空所有祝福
    /// </summary>
    public void ClearAllBlessings()
    {
        ownedBlessings.Clear();
        blessingTypeCount.Clear();
        totalMultiplierBonus = 0f;
        totalDialecticalCount = 0;
        AllGodsCount = 0;
        LuckTurnsCount = 0;
        CardMasterCount = 0;
        hasJackpot7 = false;
    }

    /// <summary>
    /// 获取玩家已拥有的所有祝福列表
    /// </summary>
    public List<BlessingInstance> GetOwnedBlessings()
    {
        List<BlessingInstance> result = new List<BlessingInstance>();
        foreach (var kvp in ownedBlessings)
        {
            BlessingData blessingData = blessingLibrary.GetBlessingById(kvp.Key);
            if (blessingData != null)
            {
                result.Add(new BlessingInstance(blessingData, kvp.Value));
            }
        }
        return result;
    }


    /// <summary>
    /// 调试：打印当前祝福状态
    /// </summary>
    [ContextMenu("Print Blessing Status")]
    public void PrintBlessingStatus()
    {
        Debug.Log("=== 祝福状态 ===");
        foreach (var kvp in ownedBlessings)
        {
            BlessingData blessing = blessingLibrary.GetBlessingById(kvp.Key);
            if (blessing != null)
            {
                Debug.Log($"{blessing.blessingName}：{kvp.Value}次");
            }
        }
        Debug.Log($"总倍率加成：{totalMultiplierBonus}");
    }
}
