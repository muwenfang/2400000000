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
    public Text blessingNameText;      // 祝福名称
    public Text blessingDescriptionText; // 祝福描述
    public Image blessingIconImage;    // 祝福图标
    public LayoutGroup layoutGroup;    // 用于自动排列
    public Text count;                 // 祝福层数显示

    [Header("配置")]
    [SerializeField] private bool autoResizeContainer = true; // 是否自动调整容器大小
    [SerializeField] private float containerPadding = 10f;    // 容器内边距

    private BlessingData currentBlessingData;

    private void OnEnable()
    {
        // 可选：在启用时刷新显示
    }

    /// <summary>
    /// 设置要显示的祝福数据
    /// </summary>
    public void SetBlessingData(BlessingData blessingData)
    {
        if (blessingData == null)
        {
            Debug.LogError("祝福数据为空！");
            return;
        }

        currentBlessingData = blessingData;
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
        }

        // 设置描述
        if (blessingDescriptionText != null)
        {
            blessingDescriptionText.text = currentBlessingData.description;
        }

        Debug.Log($"已加载祝福UI：{currentBlessingData.blessingName}");
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
        return currentBlessingData != null ? currentBlessingData.blessingName : "";
    }

    /// <summary>
    /// 获取当前祝福的描述
    /// </summary>
    public string GetBlessingDescription()
    {
        return currentBlessingData != null ? currentBlessingData.description : "";
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
    /// 设置UI是否可交互
    /// </summary>
    public void SetInteractable(bool interactable)
    {
        // 可以添加更多交互控制逻辑
        gameObject.SetActive(interactable);
    }

}
