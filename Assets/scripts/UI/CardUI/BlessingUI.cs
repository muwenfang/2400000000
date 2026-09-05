using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 祝福UI组件 - 展示单个祝福的信息
/// 该脚本应该挂在展示祝福信息的面板上
/// 包含：祝福名称、描述
/// </summary>
public class BlessingUI : MonoBehaviour
{
    [Header("UI 组件")]
    public Text blessingNameText;           // 祝福名称
    public Text blessingDescriptionText;    // 祝福描述
    public Image blessingImage;             // 祝福图标显示组件
    public Text stackCountText;             // 叠加数量显示
    public BlessingData BoundBlessing;

    [Header("祝福图标（按刷新行为区分，拖入同一张 blessingImage 的不同 Sprite）")]
    [Tooltip("AlwaysRefresh：购买后仍会继续刷新（可多次购买）")]
    public Sprite alwaysRefreshSprite;      // 可重复购买祝福的图标
    [Tooltip("NeverRefresh：不会继续刷新（只能购买一次）")]
    public Sprite neverRefreshSprite;       // 一次性祝福的图标
    [Tooltip("CurrentRoundOnly：本回合不再刷新（本次商店最多一个）")]
    public Sprite currentRoundOnlySprite;   // 本回合限购祝福的图标
    
    [Header("颜色配置")]
    [SerializeField] private Color stackableNameColor = Color.green;      // 可叠加祝福：绿色
    [SerializeField] private Color unStackableNameColor = Color.black;    // 不可叠加祝福：白色
    [SerializeField] private Color stackCountColor = new Color(1, 0.84f, 0); // 叠加数量：金色

    private BlessingData currentBlessingData;
    private int currentStackCount = 1;
    private bool hasWarnedMissingSprite;    // 未配置图标时只警告一次

    private void OnEnable()
    {
        // 可选：在启用时刷新显示
    }
    
    /// <summary>
    /// 设置要显示的祝福数据
    /// </summary>
    public void SetBlessingData(BlessingData blessingData, int stackCount = 1)
    {
        if (blessingData == null)
        {
            Debug.LogError("祝福数据为空！");
            return;
        }

        currentBlessingData = blessingData;
        currentStackCount = stackCount;
        RefreshUI();
    }

    /// <summary>
    /// 根据祝福ID设置要显示的祝福
    /// </summary>
    public void SetBlessingById(int blessingId, BlessingLibrary library)
    {
        if (library == null)
        {
            Debug.LogError("祝福库为空！");
            return;
        }

        BlessingData blessing = library.GetBlessingById(blessingId);
        if (blessing != null)
        {
            SetBlessingData(blessing);
        }
        else
        {
            Debug.LogError($"找不到ID为 {blessingId} 的祝福！");
        }
    }

    /// <summary>
    /// 根据祝福类型设置要显示的祝福
    /// </summary>
    public void SetBlessingByType(BlessingData.BlessingType type, BlessingLibrary library)
    {
        if (library == null)
        {
            Debug.LogError("祝福库为空！");
            return;
        }

        BlessingData blessing = library.GetBlessingByType(type);
        if (blessing != null)
        {
            SetBlessingData(blessing);
        }
        else
        {
            Debug.LogError($"找不到类型为 {type} 的祝福！");
        }
    }

    /// <summary>
    /// 刷新UI显示
    /// </summary>
    public void RefreshUI()
    {
        if (currentBlessingData == null)
        {
            ClearUI();
            return;
        }

        // 设置名称
        if (blessingNameText != null)
        {
            blessingNameText.text = currentBlessingData.blessingName;

            // 根据是否可叠加改变颜色
            if (currentBlessingData.isStackable)
            {
                blessingNameText.color = stackableNameColor;  // 绿色 - 可叠加
            }
            else
            {
                blessingNameText.color = unStackableNameColor; //不可叠加
            }
        }

        // 设置描述
        if (blessingDescriptionText != null)
        {
            blessingDescriptionText.text = currentBlessingData.description;
        }

        // 2. 根据刷新行为切换图标
        UpdateBlessingImage();

        // 3. 设置叠加数量显示
        UpdateStackCountDisplay();

    }

    /// <summary>
    /// 根据祝福的刷新行为切换对应的图标
    /// </summary>
    private void UpdateBlessingImage()
    {
        if (blessingImage == null || currentBlessingData == null)
            return;

        Sprite targetSprite = null;
        switch (currentBlessingData.refreshBehavior)
        {
            case BlessingData.RefreshBehavior.AlwaysRefresh:
                targetSprite = alwaysRefreshSprite;
                break;
            case BlessingData.RefreshBehavior.NeverRefresh:
                targetSprite = neverRefreshSprite;
                break;
            case BlessingData.RefreshBehavior.CurrentRoundOnly:
                targetSprite = currentRoundOnlySprite;
                break;
        }

        if (targetSprite != null)
        {
            blessingImage.sprite = targetSprite;
            blessingImage.gameObject.SetActive(true);
        }
        else
        {
            // 未配置对应图标时隐藏，避免显示错误的图片
            blessingImage.gameObject.SetActive(false);
            if (!hasWarnedMissingSprite)
            {
                hasWarnedMissingSprite = true;
                Debug.LogWarning($"[BlessingUI] 刷新行为 {currentBlessingData.refreshBehavior} 未配置对应图标，请在 Inspector 中拖入对应图片");
            }
        }
    }

    /// <summary>
    /// 更新叠加数量显示
    /// </summary>
    private void UpdateStackCountDisplay()
    {
        if (stackCountText == null)
            return;

        // 只有在数量大于1时才显示
        if (currentStackCount > 1)
        {
            stackCountText.text = $"×{currentStackCount}";
            stackCountText.color = stackCountColor;
            stackCountText.gameObject.SetActive(true);
        }
        else
        {
            stackCountText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 清空UI显示
    /// </summary>
    public void ClearUI()
    {
        if (blessingNameText != null)
            blessingNameText.text = "";

        if (blessingDescriptionText != null)
            blessingDescriptionText.text = "";

        if (blessingImage != null)
            blessingImage.gameObject.SetActive(false);

        currentBlessingData = null;
    }

    /// <summary>
    /// 获取当前祝福的名称
    /// </summary>
    public string GetBlessingName()
    {
        return currentBlessingData != null ? currentBlessingData.blessingName : "error";
    }

    /// <summary>
    /// 获取当前祝福的描述
    /// </summary>
    public string GetBlessingDescription()
    {
        return currentBlessingData != null ? currentBlessingData.description : "error";
    }

    /// <summary>
    /// 获取当前显示的祝福数据
    /// </summary>
    public BlessingData GetCurrentBlessingData()
    {
        return currentBlessingData;
    }

    /// <summary>
    /// 获取当前显示的祝福类型
    /// </summary>
    public BlessingData.BlessingType GetBlessingType()
    {
        return currentBlessingData != null ? currentBlessingData.blessingType : BlessingData.BlessingType.FinancialMaster;
    }

    /// <summary>
    /// 获取叠加数量
    /// </summary>
    public int GetStackCount()
    {
        return currentStackCount;
    }

    /// <summary>
    /// 是否可叠加
    /// </summary>
    public bool IsStackable()
    {
        return currentBlessingData != null && currentBlessingData.isStackable;
    }

}
