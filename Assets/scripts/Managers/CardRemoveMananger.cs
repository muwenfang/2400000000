using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 卡牌删除管理器 - 新增
/// 职责：
/// 1. 统一管理删除卡牌的逻辑
/// 2. 计算删除成本
/// 3. 处理UI确认对话框
/// 4. 同步数据和UI
/// </summary>
public class CardRemovalManager : MonoBehaviour
{
    public static CardRemovalManager Instance;

    [Header("配置")]
    [Tooltip("基础删除成本")]
    public int baseRemoveCost = 5;

    [Tooltip("最少保留的数字卡数量")]
    public int minNumberCardsToKeep = 6;

    // 删除成本计算：基础成本 × 2^删除次数
    private int totalRemovedCards = 0;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 计算删除一张卡牌的成本
    /// </summary>
    public BigInteger CalculateRemoveCost()
    {
        BigInteger powerOfTwo = BigInteger.Pow(2, totalRemovedCards);
        return baseRemoveCost * powerOfTwo;
    }

    /// <summary>
    /// 打开删除模式（商店中的删除按钮调用）
    /// </summary>
    public void OpenDeleteMode()
    {
        // 检查是否在商店阶段
        if (GameManager.Instance.currentState != GameManager.GameState.Shop)
        {
            Debug.LogWarning("[CardDeletionManager] 只能在商店阶段删除卡牌");
            return;
        }

        Debug.Log("[CardDeletionManager] 打开删除模式");

        // 打开卡牌库，进入删除模式
        UIManager.Instance.OpenNumberCardDeck();

        // 通知 ShowMyCard 进入删除模式
        var showMyCard = UIManager.Instance.myNumberCardPanel.GetComponent<ShowMyCard>();
        if (showMyCard != null)
        {
            showMyCard.EnterDeleteMode();
        }
    }

    /// <summary>
    /// 关闭删除模式（关闭卡牌库时调用）
    /// </summary>
    public void CloseDeleteMode()
    {
        var showMyCard = UIManager.Instance.myNumberCardPanel.GetComponent<ShowMyCard>();
        if (showMyCard != null)
        {
            showMyCard.ExitDeleteMode();
        }

        UIManager.Instance.CloseCardDeck();
    }

    /// <summary>
    /// 获取当前删除次数
    /// </summary>
    public int GetTotalRemovedCards()
    {
        return totalRemovedCards;
    }

    /// <summary>
    /// 增加删除计数
    /// </summary>
    public void IncrementRemovalCount()
    {
        totalRemovedCards++;
    }

    /// <summary>
    /// 重置删除计数（每回合或阶段）
    /// </summary>
    public void ResetRemovalCount()
    {
        totalRemovedCards = 0;
        Debug.Log("[CardDeletionManager] 删除计数已重置");
    }
}
