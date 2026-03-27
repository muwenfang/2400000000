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
    public Image blessingImage;         // 祝福图标
    public Text stackCountText;             // 叠加数量显示
    public BlessingData BoundBlessing;
    
    [Header("颜色配置")]
    [SerializeField] private Color stackableNameColor = Color.green;      // 可叠加祝福：绿色
    [SerializeField] private Color unStackableNameColor = Color.black;    // 不可叠加祝福：白色
    [SerializeField] private Color stackCountColor = new Color(1, 0.84f, 0); // 叠加数量：金色

    private BlessingData currentBlessingData;
    private int currentStackCount = 1;

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

        // 3. 设置叠加数量显示
        UpdateStackCountDisplay();

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
