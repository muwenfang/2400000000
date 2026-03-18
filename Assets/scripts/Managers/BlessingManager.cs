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

    // 用于快速查询特定祝福的购买次数
    private Dictionary<BlessingData.BlessingType, int> blessingTypeCount =
        new Dictionary<BlessingData.BlessingType, int>();

    [Header("祝福效果累积")]
    private float totalMultiplierBonus = 0f; // 倍率加成
    private int totalDialecticalCount = 0;   // 购买次数
    private bool hasAllGodsInPlace = false;  //是否拥有祝福众神归位

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

        // 记录祝福购买
        if (ownedBlessings.ContainsKey(blessingData.blessingId))
        {
            ownedBlessings[blessingData.blessingId]++;
        }
        else
        {
            ownedBlessings[blessingData.blessingId] = 1;
        }

        // 更新类型计数
        if (!blessingTypeCount.ContainsKey(blessingData.blessingType))
        {
            blessingTypeCount[blessingData.blessingType] = 0;
        }
        blessingTypeCount[blessingData.blessingType]++;

        // 应用祝福效果
        ApplyBlessingEffect(blessingData);

        return true;
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
            case BlessingData.BlessingType.AllGodsInPlace:
                // 众神归位 - 效果每回合可能要重新检测祝福数量
                hasAllGodsInPlace = true;
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

            case BlessingData.BlessingType.DialecticalViewpoint:
                // 辩证主义 - 立即应用倍率和点数
                totalDialecticalCount++;
                totalMultiplierBonus += blessingData.effectValue;

                // 给予奖励点数
                GameManager.Instance.AddPoints(blessingData.bonusPoints);

                Debug.Log($"辩证主义效果已激活：倍率+{blessingData.effectValue}，" +
                    $"额外获得{blessingData.bonusPoints}点，价格上升1%");
                break;
        }
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
    /// 计算“众神归位”的倍率
    /// </summary>
    private float CalculateAllGodsInPlaceBonus()
    {
        if (!hasAllGodsInPlace) return 0f;
        int totalBlessingCount = GetTotalBlessingCount();
        int AllGodsCount = GetBlessingTypeCount(BlessingData.BlessingType.AllGodsInPlace);
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
    /// 获取当前总倍率加成（来自所有祝福）
    /// </summary>
    public float GetTotalMultiplierBonus()
    {
        float totalMultiplierBonus = 0f;
        
        ///众神归位的额外倍率
        float AllGodsInPlaceBonus = CalculateAllGodsInPlaceBonus();
        totalMultiplierBonus += AllGodsInPlaceBonus; 
        
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
        return multiplier;
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
        Debug.Log("所有祝福已清空");
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
        Debug.Log($"价格乘数：{GetCurrentPriceMultiplier()}");
    }
}
